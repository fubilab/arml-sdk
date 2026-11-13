using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Phone-side control UI, built entirely at runtime (uGUI). It contains no knowledge of any
/// specific parameter: it renders one control per parameter the server advertises (see
/// OscParamInfo), grouped under the server's section names, and sends edits back over OSC.
/// What can be controlled is decided in the Linux build, so this app never needs rebuilding for that.
/// </summary>
public class OSCTestUI : MonoBehaviour
{
    [SerializeField] private OSCRemoteClient client;

    private const string LastIpKey = "osc_last_ip";

    private static readonly Color Bg = new Color(0.08f, 0.09f, 0.11f);
    private static readonly Color Panel = new Color(0.16f, 0.18f, 0.22f);
    private static readonly Color Accent = new Color(0.25f, 0.55f, 0.95f);
    private static readonly Color Good = new Color(0.25f, 0.7f, 0.4f);
    private static readonly Color Muted = new Color(0.3f, 0.33f, 0.38f);

    private sealed class SliderRow
    {
        public Slider Slider;
        public Text Label;
        public string Name;
        public string Format;

        public void Set(float value)
        {
            Slider.SetValueWithoutNotify(value);
            UpdateLabel(value);
        }

        public void UpdateLabel(float value) => Label.text = $"{Name}: {value.ToString(Format)}";
    }

    private Font _font;
    private Transform _content;
    private Transform _dynamic;   // rebuilt whenever the server sends a new schema
    private Text _status;
    private InputField _ipField;
    private Transform _serverList;  // one button per discovered server, rebuilt when the list changes

    // One entry per control; called when values arrive from the server.
    private readonly List<Action> _refreshers = new List<Action>();
    private readonly List<Texture2D> _textures = new List<Texture2D>();

    // ---------------------------------------------------------------- build

    private void Start()
    {
        if (client == null) client = FindFirstObjectByType<OSCRemoteClient>();
        if (client == null)
        {
            Debug.LogError("[OSC UI] No OSCRemoteClient in the scene");
            return;
        }

        // Android otherwise tends to run at 30 fps, which makes scrolling look choppy.
        Application.targetFrameRate = 60;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUi();

        client.SchemaChanged += RebuildControls;
        client.StateChanged += RefreshValues;
        client.ConnectionChanged += OnConnectionChanged;
        client.ServersChanged += RefreshServers;
        RebuildControls();
        RefreshStatus();
        RefreshServers();
    }

    private void OnConnectionChanged()
    {
        RefreshStatus();
        RefreshServers(); // highlights the selected server
        // The "server exposes no parameters" note depends on the connection state.
        if (client.Params.Count == 0) RebuildControls();
    }

    private float _nextStatusRefresh;

    private void Update()
    {
        // Keep the live probe counters fresh while searching.
        if (client == null || _status == null || client.State != OSCRemoteClient.ConnectionState.Searching) return;
        if (Time.unscaledTime < _nextStatusRefresh) return;

        _nextStatusRefresh = Time.unscaledTime + 0.5f;
        RefreshStatus();
    }

    private void OnDestroy()
    {
        foreach (Texture2D t in _textures) if (t != null) Destroy(t);
        if (client == null) return;
        client.SchemaChanged -= RebuildControls;
        client.StateChanged -= RefreshValues;
        client.ConnectionChanged -= OnConnectionChanged;
        client.ServersChanged -= RefreshServers;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("TestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        var background = NewRect("Background", canvasObject.transform);
        Stretch(background);
        background.gameObject.AddComponent<Image>().color = Bg;

        // Scroll area, kept inside the device's safe area (notches, nav bars).
        var scroll = NewRect("Scroll", canvasObject.transform);
        Rect safe = Screen.safeArea;
        scroll.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        scroll.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        scroll.offsetMin = scroll.offsetMax = Vector2.zero;

        var viewport = NewRect("Viewport", scroll);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 30, 120);
        layout.spacing = 16;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _content = content;

        var scrollRect = scroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 40f;

        BuildConnectionSection();
    }

    private void BuildConnectionSection()
    {
        AddHeader(_content, "Connection");
        _status = AddText(_content, "", 34, TextAnchor.MiddleLeft, 120);

        // Every server answering discovery; tap one to switch to it.
        var list = NewRect("Servers", _content);
        var listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 12;
        listLayout.childControlWidth = listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _serverList = list;

        var row = AddRow(_content, 100);
        _ipField = AddInputField(row, "server ip (optional)");
        _ipField.text = PlayerPrefs.GetString(LastIpKey, "");
        AddButton(row, "Connect", Accent, () =>
        {
            if (client.ConnectManual(_ipField.text)) PlayerPrefs.SetString(LastIpKey, _ipField.text);
            else _status.text = "Not a valid IP address";
        }, 220);
        AddButton(row, "Rescan", Muted, () => client.Search(), 220);
    }

    private void RefreshServers()
    {
        if (_serverList == null) return;

        // Destroy() is deferred to the end of the frame, so detach first to keep the layout clean.
        for (int i = _serverList.childCount - 1; i >= 0; i--)
        {
            Transform child = _serverList.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        if (client.Servers.Count == 0)
        {
            AddText(_serverList, "No servers found yet", 30, TextAnchor.MiddleLeft, 50).color = new Color(1, 1, 1, 0.6f);
            return;
        }

        AddText(_serverList, "Servers (tap to switch)", 30, TextAnchor.MiddleLeft, 50).color = new Color(1, 1, 1, 0.6f);

        foreach (OscServerInfo server in client.Servers)
        {
            OscServerInfo target = server;
            bool current = client.Server != null && client.Server.Equals(target.Endpoint);
            string suffix = !current ? "" :
                client.State == OSCRemoteClient.ConnectionState.Connected ? "   [connected]" : "   [connecting...]";

            AddButton(_serverList, $"{target.Name}{suffix}\n{target.Endpoint}", current ? Good : Muted,
                () => client.Select(target), 0, 120, 30);
        }
    }

    // ---------------------------------------------------------------- dynamic controls

    private void RebuildControls()
    {
        if (_dynamic != null) Destroy(_dynamic.gameObject);
        foreach (Texture2D t in _textures) if (t != null) Destroy(t);
        _textures.Clear();
        _refreshers.Clear();

        var holder = NewRect("Controls", _content);
        var layout = holder.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        holder.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _dynamic = holder;

        string group = null;
        foreach (OscParamInfo p in client.Params)
        {
            string g = p.Group ?? "";
            if (g.Length > 0 && g != group) AddHeader(holder, g);
            if (g.Length > 0) group = g;

            switch (p.Type)
            {
                case OscParamType.Float:
                case OscParamType.Int: BuildNumber(holder, p); break;
                case OscParamType.Bool: BuildBool(holder, p); break;
                case OscParamType.Enum: BuildChoice(holder, p); break;
                case OscParamType.Trigger: BuildTrigger(holder, p); break;
                case OscParamType.Range: BuildRange(holder, p); break;
                case OscParamType.Color: BuildColor(holder, p); break;
                case OscParamType.Gradient: new GradientEditor(this, holder, p); break;
            }
        }

        if (client.Params.Count == 0 && client.State == OSCRemoteClient.ConnectionState.Connected)
            AddText(holder, "The server exposes no parameters.", 32, TextAnchor.MiddleLeft, 60);

        RefreshValues();
    }

    private void RefreshValues()
    {
        foreach (Action refresh in _refreshers) refresh();
    }

    private void Edited(OscParamInfo p) => client.SendParam(p);

    private void BuildNumber(Transform parent, OscParamInfo p)
    {
        bool whole = p.Type == OscParamType.Int;
        SliderRow row = AddSlider(parent, p.Label, new Vector2(p.Min, p.Max),
            whole ? "0" : OscParamInfo.FormatFor(p.Min, p.Max), v =>
            {
                p.Values[0] = whole ? Mathf.Round(v) : v;
                Edited(p);
            }, whole);
        _refreshers.Add(() => row.Set(p.Values[0]));
    }

    private void BuildRange(Transform parent, OscParamInfo p)
    {
        string format = OscParamInfo.FormatFor(p.Min, p.Max);
        var bounds = new Vector2(p.Min, p.Max);
        SliderRow lo = AddSlider(parent, p.Label + " min", bounds, format, v => { p.Values[0] = v; Edited(p); });
        SliderRow hi = AddSlider(parent, p.Label + " max", bounds, format, v => { p.Values[1] = v; Edited(p); });
        _refreshers.Add(() => { lo.Set(p.Values[0]); hi.Set(p.Values[1]); });
    }

    private void BuildBool(Transform parent, OscParamInfo p)
    {
        Button button = null;
        button = AddButton(parent, "", Muted, () =>
        {
            p.Values[0] = p.Values[0] >= 0.5f ? 0f : 1f;
            Edited(p);
            SetToggle(button, p.Label, p.Values[0] >= 0.5f);
        }, 0, 90);
        _refreshers.Add(() => SetToggle(button, p.Label, p.Values[0] >= 0.5f));
    }

    private void BuildChoice(Transform parent, OscParamInfo p)
    {
        AddText(parent, p.Label, 32, TextAnchor.MiddleLeft, 50);

        RectTransform grid = NewRect("Options", parent);
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(320, 100);
        layout.spacing = new Vector2(14, 14);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var buttons = new List<Button>();

        void Highlight()
        {
            for (int i = 0; i < buttons.Count; i++)
                buttons[i].image.color = i == Mathf.RoundToInt(p.Values[0]) ? Accent : Muted;
        }

        for (int i = 0; i < p.Options.Length; i++)
        {
            int index = i;
            buttons.Add(AddButton(grid, p.Options[i], Muted, () =>
            {
                p.Values[0] = index;
                Edited(p);
                Highlight();
            }, 0, 0, 30));
        }

        _refreshers.Add(Highlight);
    }

    private void BuildTrigger(Transform parent, OscParamInfo p) =>
        AddButton(parent, p.Label, Accent, () => Edited(p), 0, 90);

    private void BuildColor(Transform parent, OscParamInfo p)
    {
        var swatchRect = NewRect("Swatch", parent);
        AddLayout(swatchRect, 60);
        var swatch = swatchRect.gameObject.AddComponent<Image>();

        void UpdateSwatch() => swatch.color = new Color(p.Values[0], p.Values[1], p.Values[2]);

        var unit = new Vector2(0, 1);
        SliderRow r = AddSlider(parent, p.Label + " red", unit, "0.00", v => { p.Values[0] = v; Edited(p); UpdateSwatch(); });
        SliderRow g = AddSlider(parent, p.Label + " green", unit, "0.00", v => { p.Values[1] = v; Edited(p); UpdateSwatch(); });
        SliderRow b = AddSlider(parent, p.Label + " blue", unit, "0.00", v => { p.Values[2] = v; Edited(p); UpdateSwatch(); });

        _refreshers.Add(() =>
        {
            r.Set(p.Values[0]);
            g.Set(p.Values[1]);
            b.Set(p.Values[2]);
            UpdateSwatch();
        });
    }

    // Colour keys of a gradient parameter: preview strip, mode toggle, key selector and RGB/position sliders.
    private sealed class GradientEditor
    {
        private static readonly string[] ModeNames = { "Blend", "Fixed", "Perceptual" };

        private readonly OSCTestUI _ui;
        private readonly OscParamInfo _p;
        private readonly Button _modeButton;
        private readonly RectTransform _keyRow;
        private readonly List<Button> _keyButtons = new List<Button>();
        private readonly SliderRow _keyR, _keyG, _keyB, _keyT;
        private readonly Texture2D _preview;
        private int _selected;

        public GradientEditor(OSCTestUI ui, Transform parent, OscParamInfo p)
        {
            _ui = ui;
            _p = p;

            ui.AddText(parent, p.Label, 32, TextAnchor.MiddleLeft, 50);

            var previewRect = NewRect("Preview", parent);
            AddLayout(previewRect, 90);
            var image = previewRect.gameObject.AddComponent<RawImage>();
            _preview = new Texture2D(128, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            ui._textures.Add(_preview);
            image.texture = _preview;

            _modeButton = ui.AddButton(parent, "", Muted, () =>
            {
                p.GradientMode = (p.GradientMode + 1) % ModeNames.Length;
                Send();
                Rebuild();
            }, 0, 90);

            ui.AddText(parent, "Colour keys (tap to select)", 28, TextAnchor.MiddleLeft, 50);
            _keyRow = ui.AddRow(parent, 100);

            var unit = new Vector2(0, 1);
            _keyR = ui.AddSlider(parent, "Red", unit, "0.00", v => EditKey(0, v));
            _keyG = ui.AddSlider(parent, "Green", unit, "0.00", v => EditKey(1, v));
            _keyB = ui.AddSlider(parent, "Blue", unit, "0.00", v => EditKey(2, v));
            _keyT = ui.AddSlider(parent, "Position", unit, "0.00", v => EditKey(3, v));

            ui._refreshers.Add(Rebuild);
        }

        private void Send() => _ui.Edited(_p);

        private void EditKey(int component, float value)
        {
            if (_selected < 0 || _selected >= _p.GradientKeys.Count) return;

            Vector4 key = _p.GradientKeys[_selected];
            key[component] = value;
            _p.GradientKeys[_selected] = key;

            Send();
            Bake();
            UpdateSwatch(_selected);
        }

        private void AddKey()
        {
            if (_p.GradientKeys.Count >= OscParamInfo.MaxGradientKeys) return;

            Color at = _p.ToGradient().Evaluate(0.5f);
            _p.GradientKeys.Add(new Vector4(at.r, at.g, at.b, 0.5f));
            _selected = _p.GradientKeys.Count - 1;
            Send();
            Rebuild();
        }

        private void RemoveKey()
        {
            if (_p.GradientKeys.Count <= 1) return;

            _p.GradientKeys.RemoveAt(_selected);
            _selected = Mathf.Clamp(_selected, 0, _p.GradientKeys.Count - 1);
            Send();
            Rebuild();
        }

        private void Rebuild()
        {
            _modeButton.GetComponentInChildren<Text>().text =
                $"Mode: {ModeNames[Mathf.Clamp(_p.GradientMode, 0, ModeNames.Length - 1)]} (tap to change)";

            _selected = Mathf.Clamp(_selected, 0, Mathf.Max(0, _p.GradientKeys.Count - 1));

            foreach (Button b in _keyButtons) Destroy(b.gameObject);
            _keyButtons.Clear();
            for (int i = 0; i < _p.GradientKeys.Count; i++)
            {
                int index = i;
                _keyButtons.Add(_ui.AddButton(_keyRow, (i + 1).ToString(), Muted, () =>
                {
                    _selected = index;
                    Rebuild();
                }, 100));
                UpdateSwatch(i);
            }
            _keyButtons.Add(_ui.AddButton(_keyRow, "+", Accent, AddKey, 100));
            _keyButtons.Add(_ui.AddButton(_keyRow, "-", Accent, RemoveKey, 100));

            if (_selected < _p.GradientKeys.Count)
            {
                Vector4 key = _p.GradientKeys[_selected];
                _keyR.Set(key.x);
                _keyG.Set(key.y);
                _keyB.Set(key.z);
                _keyT.Set(key.w);
            }

            Bake();
        }

        private void UpdateSwatch(int index)
        {
            if (index >= _keyButtons.Count) return;

            Vector4 k = _p.GradientKeys[index];
            var colour = new Color(k.x, k.y, k.z, 1f);
            Button button = _keyButtons[index];
            button.image.color = colour;
            Text label = button.GetComponentInChildren<Text>();
            label.color = colour.grayscale > 0.5f ? Color.black : Color.white;
            label.fontStyle = index == _selected ? FontStyle.BoldAndItalic : FontStyle.Normal;
            label.text = index == _selected ? $"[{index + 1}]" : (index + 1).ToString();
        }

        private void Bake()
        {
            Gradient g = _p.ToGradient();
            int width = _preview.width;
            for (int x = 0; x < width; x++)
                _preview.SetPixel(x, 0, g.Evaluate((float)x / (width - 1)));
            _preview.Apply();
        }
    }

    // ---------------------------------------------------------------- status

    private void RefreshStatus()
    {
        if (_status == null) return;

        if (!string.IsNullOrEmpty(client.Error))
        {
            _status.text = $"Network error: {client.Error}\n(Android: set Internet Access to Require)";
            _status.color = Color.red;
            return;
        }

        switch (client.State)
        {
            case OSCRemoteClient.ConnectionState.Searching:
                _status.text = $"Searching for server...\n{client.DebugInfo}";
                _status.color = Color.yellow;
                break;
            case OSCRemoteClient.ConnectionState.Connecting:
                _status.text = $"Connecting to {client.ServerName} ({client.Server})...";
                _status.color = Color.yellow;
                break;
            default:
                _status.text = $"Connected: {client.ServerName} ({client.Server})";
                _status.color = Good;
                break;
        }
    }

    private static void SetToggle(Button button, string label, bool on)
    {
        button.GetComponentInChildren<Text>().text = $"{label}: {(on ? "ON" : "OFF")}";
        button.image.color = on ? Good : Muted;
    }

    // ---------------------------------------------------------------- uGUI helpers

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static LayoutElement AddLayout(RectTransform rect, float height, float width = 0)
    {
        var le = rect.gameObject.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
        if (height > 0) le.preferredHeight = height;
        if (width > 0) le.preferredWidth = width;
        return le;
    }

    private void AddHeader(Transform parent, string text)
    {
        Text t = AddText(parent, text, 40, TextAnchor.LowerLeft, 80);
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(0.6f, 0.75f, 1f);
    }

    private Text AddText(Transform parent, string text, int size, TextAnchor anchor, float height)
    {
        RectTransform rect = NewRect("Text", parent);
        AddLayout(rect, height);
        var t = rect.gameObject.AddComponent<Text>();
        t.font = _font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = Color.white;
        t.text = text;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        return t;
    }

    private RectTransform AddRow(Transform parent, float height)
    {
        RectTransform row = NewRect("Row", parent);
        AddLayout(row, height);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 12;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        return row;
    }

    // width > 0 gives a fixed width; otherwise it flexes to fill. height is used outside rows.
    private Button AddButton(Transform parent, string label, Color colour, Action onClick,
        float width = 0, float height = 0, int fontSize = 36)
    {
        RectTransform rect = NewRect("Button", parent);
        LayoutElement le = AddLayout(rect, height, width);
        if (width <= 0) le.flexibleWidth = 1;

        var image = rect.gameObject.AddComponent<Image>();
        image.color = colour;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        RectTransform textRect = NewRect("Label", rect);
        Stretch(textRect);
        var text = textRect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;
        return button;
    }

    private InputField AddInputField(Transform parent, string placeholder)
    {
        RectTransform rect = NewRect("InputField", parent);
        AddLayout(rect, 0).flexibleWidth = 1;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Panel;

        RectTransform textRect = NewRect("Text", rect);
        Stretch(textRect);
        textRect.offsetMin = new Vector2(16, 6);
        textRect.offsetMax = new Vector2(-16, -6);
        var text = textRect.gameObject.AddComponent<Text>();
        text.font = _font;
        text.fontSize = 36;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.white;
        text.supportRichText = false;

        RectTransform phRect = NewRect("Placeholder", rect);
        Stretch(phRect);
        phRect.offsetMin = new Vector2(16, 6);
        phRect.offsetMax = new Vector2(-16, -6);
        var ph = phRect.gameObject.AddComponent<Text>();
        ph.font = _font;
        ph.fontSize = 32;
        ph.fontStyle = FontStyle.Italic;
        ph.alignment = TextAnchor.MiddleLeft;
        ph.color = new Color(1, 1, 1, 0.4f);
        ph.text = placeholder;

        var field = rect.gameObject.AddComponent<InputField>();
        field.targetGraphic = image;
        field.textComponent = text;
        field.placeholder = ph;
        // Not the numeric keyboard: on Android it's a decimal-number input that allows only one '.'.
        field.keyboardType = TouchScreenKeyboardType.Default;
        field.lineType = InputField.LineType.SingleLine;
        return field;
    }

    private SliderRow AddSlider(Transform parent, string name, Vector2 bounds, string format,
        Action<float> onChange, bool wholeNumbers = false)
    {
        var row = new SliderRow { Name = name, Format = format };

        RectTransform holder = NewRect(name, parent);
        AddLayout(holder, 130);
        var v = holder.gameObject.AddComponent<VerticalLayoutGroup>();
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        row.Label = AddText(holder, "", 32, TextAnchor.MiddleLeft, 44);

        RectTransform root = NewRect("Slider", holder);
        AddLayout(root, 80);

        RectTransform track = NewRect("Track", root);
        track.anchorMin = new Vector2(0, 0.5f);
        track.anchorMax = new Vector2(1, 0.5f);
        track.sizeDelta = new Vector2(0, 18);
        track.gameObject.AddComponent<Image>().color = Panel;

        RectTransform fillArea = NewRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0, 0.5f);
        fillArea.anchorMax = new Vector2(1, 0.5f);
        fillArea.offsetMin = new Vector2(0, -9);
        fillArea.offsetMax = new Vector2(-25, 9);
        RectTransform fill = NewRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0, 1);
        fill.sizeDelta = new Vector2(10, 0);
        fill.gameObject.AddComponent<Image>().color = Accent;

        RectTransform handleArea = NewRect("Handle Slide Area", root);
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(25, 0);
        handleArea.offsetMax = new Vector2(-25, 0);
        RectTransform handle = NewRect("Handle", handleArea);
        handle.anchorMin = Vector2.zero;
        handle.anchorMax = new Vector2(0, 1);
        handle.sizeDelta = new Vector2(50, 0);
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = bounds.x;
        slider.maxValue = bounds.y;
        slider.wholeNumbers = wholeNumbers;
        slider.onValueChanged.AddListener(value =>
        {
            row.UpdateLabel(value);
            onChange(value);
        });

        row.Slider = slider;
        row.Set(slider.minValue);
        return row;
    }
}

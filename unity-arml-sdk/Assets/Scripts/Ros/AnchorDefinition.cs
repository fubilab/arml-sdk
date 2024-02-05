using UnityEngine;

public class AnchorDefinition : MonoBehaviour
{
    void Start()
    {
        UpdateVisualizers();
        _camera = Camera.main;
        _lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (_dirty)
        {
            UpdateVisualizers();
            _dirty = false;
        }
        // if anchor has moved, add it to the dirty queue
        if (!_lastPosition.Equals(transform.position))
        {
            Debouncer.Instance.Debounce(1, () =>
            {
                AnchorPublisher.Instance.DirtyAnchorDefinitions.Enqueue(this);
            });
            _lastPosition = transform.position;
        }
        // update boundary viz
        float distanceToCamera = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(
                _camera.transform.position.x,
                _camera.transform.position.z
            )
        );
        BoundaryCylinder.sharedMaterial.color =
            distanceToCamera > _filterProximity
                ? _inactiveBoundaryColor
                : _activeBoundaryColor;
    }

    void UpdateVisualizers()
    {
        BoundaryCylinder.transform.localScale = new Vector3(
            _filterProximity * 2,
            BoundaryCylinder.transform.localScale.y,
            _filterProximity * 2
        );
    }

    void SetDirty()
    {
        _dirty = true;
    }

    [SerializeField, SerializeProperty("FilterProximity")]
    private float _filterProximity;

    [SerializeField, SerializeProperty("InactiveBoundaryColor")]
    private Color _inactiveBoundaryColor;

    [SerializeField, SerializeProperty("ActiveBoundaryColor")]
    private Color _activeBoundaryColor;

    private bool _dirty = true;
    private Camera _camera;
    private Vector3 _lastPosition;

    public string AnchorId;

    public Color InactiveBoundaryColor
    {
        get => _inactiveBoundaryColor;
        set
        {
            _inactiveBoundaryColor = value;
            SetDirty();
        }
    }

    public Color ActiveBoundaryColor
    {
        get => _activeBoundaryColor;
        set
        {
            _activeBoundaryColor = value;
            SetDirty();
        }
    }

    public float FilterProximity
    {
        get => _filterProximity;
        set
        {
            _filterProximity = value;
            SetDirty();
        }
    }

    public MeshRenderer BoundaryCylinder;
}
using UnityEngine.EventSystems;

/// <summary>
/// Exposes what the ARML's hardware remote can do to the OSC remote: open/close the menu, reset the
/// pose, press the arrow keys (navigate the menu, or move the camera while the menu is closed), and
/// activate the selected item.
/// The actions live in <see cref="RemoteControl"/>, so they behave exactly like the physical keys.
///
/// They are registered as ordinary triggers under fixed "/remote/..." addresses, so any client shows
/// them as buttons; the phone app recognises the addresses and draws a pad instead.
///
/// <see cref="OSCServerController"/> adds this component automatically (see its "Expose Remote Control" option).
/// </summary>
public class OSCRemoteControlSource : OscParameterSource
{
    // First on the phone.
    public override int Order => -100;

    public override void Register(OscParameterRegistry r)
    {
        const string group = OscAddresses.RemoteGroup;

        r.Trigger(OscAddresses.RemoteMenu, "Menu", () => RemoteControl.PressMenu(), group);
        r.Trigger(OscAddresses.RemoteReset, "Reset pose", () => RemoteControl.PressReset(), group);

        // Arrow keys: navigate the menu when it is open, move the camera when it is closed.
        r.Trigger(OscAddresses.RemoteUp, "Up", () => RemoteControl.PressArrow(MoveDirection.Up), group);
        r.Trigger(OscAddresses.RemoteDown, "Down", () => RemoteControl.PressArrow(MoveDirection.Down), group);
        r.Trigger(OscAddresses.RemoteLeft, "Left", () => RemoteControl.PressArrow(MoveDirection.Left), group);
        r.Trigger(OscAddresses.RemoteRight, "Right", () => RemoteControl.PressArrow(MoveDirection.Right), group);
        r.Trigger(OscAddresses.RemoteSelect, "Select", () => RemoteControl.Submit(), group);
    }
}

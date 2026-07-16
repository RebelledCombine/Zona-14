using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Radiation.UI;

public sealed class GeigerItemControl : Control
{
    private readonly GeigerComponent _component;
    private readonly RichTextLabel _label;

    public GeigerItemControl(GeigerComponent component)
    {
        _component = component;
        _label = new RichTextLabel { StyleClasses = { StyleClass.ItemStatus } };
        AddChild(_label);

        Update();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_component.UiUpdateNeeded)
            return;
        Update();
    }

    private void Update()
    {
        string msg;
        if (_component.IsEnabled)
        {
            var color = SharedGeigerSystem.LevelToColor(_component.DangerLevel);
            var currentRads = _component.CurrentRadiation;
            var rads = currentRads.ToString("N1");
            // Zona14: use the geiger's configured prefix (Fluent key) or fall back to the default "rads" locale
            var unit = string.IsNullOrEmpty(_component.Prefix)
                ? Loc.GetString("geiger-unit-rads")
                : Loc.TryGetString(_component.Prefix, out var prefix) ? prefix : _component.Prefix;

            // Zona14: ShowAll dosimeters display a compact per-damage-type readout
            if (_component.ShowAll && _component.DamageTypes.Count > 0)
            {
                var details = new List<string>();
                foreach (var damageType in _component.DamageTypes)
                {
                    var typeId = damageType.Id.ToString();
                    var value = SharedGeigerSystem.GetCurrentDamageValue(_component, typeId);
                    var name = string.IsNullOrEmpty(damageType.Name)
                        ? typeId
                        : Loc.TryGetString(damageType.Name, out var nameStr) ? nameStr : damageType.Name;
                    details.Add($"{name}: {value:N1}");
                }

                msg = Loc.GetString("geiger-item-control-status-all",
                    ("rads", rads), ("color", color), ("unit", unit),
                    ("details", string.Join(", ", details)));
            }
            else
            {
                msg = Loc.GetString("geiger-item-control-status",
                    ("rads", rads), ("color", color), ("unit", unit));
            }
            // End Zona14
        }
        else
        {
            msg = Loc.GetString("geiger-item-control-disabled");
        }

        _label.SetMarkup(msg);
        _component.UiUpdateNeeded = false;
    }
}

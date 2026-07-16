using Content.Shared.Examine;
using Content.Shared.Radiation.Components;

namespace Content.Shared.Radiation.Systems;

public abstract class SharedGeigerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, GeigerComponent component, ExaminedEvent args)
    {
        if (!component.ShowExamine || !component.IsEnabled || !args.IsInDetailsRange)
            return;

        var currentRads = component.CurrentRadiation;
        var rads = currentRads.ToString("N1");
        var color = LevelToColor(component.DangerLevel);
        // Zona14: use the geiger's configured prefix (Fluent key) or fall back to the default "rads" locale
        var unit = string.IsNullOrEmpty(component.Prefix)
            ? Loc.GetString("geiger-unit-rads")
            : Loc.TryGetString(component.Prefix, out var prefix) ? prefix : component.Prefix;
        var msg = Loc.GetString("geiger-component-examine",
            ("rads", rads), ("color", color), ("unit", unit));
        args.PushMarkup(msg);

        // Zona14: per-damage-type readout for ShowAll dosimeters
        if (component.ShowAll)
        {
            var types = component.DamageTypes.Count > 0
                ? component.DamageTypes
                : new List<GeigerDamageType> { new() { Id = "Radiation" } };

            foreach (var damageType in types)
            {
                var value = component.CurrentDamage.TryGetValue(damageType.Id.ToString(), out var v) ? v : 0f;
                var name = string.IsNullOrEmpty(damageType.Name)
                    ? damageType.Id.ToString()
                    : Loc.TryGetString(damageType.Name, out var nameStr) ? nameStr : damageType.Name;
                var typeColor = LevelToColor(RadsToLevel(value));
                var detail = Loc.GetString("geiger-component-examine-detail",
                    ("name", (object) name), ("value", (object) value.ToString("N1")), ("color", (object) typeColor));
                args.PushMarkup(detail);
            }
        }
        // End Zona14
    }

    // Zona14: helper used by GeigerItemControl to avoid execute-accessing CurrentDamage directly
    public static float GetCurrentDamageValue(GeigerComponent component, string typeId)
    {
        return component.CurrentDamage.TryGetValue(typeId, out var value) ? value : 0f;
    }

    // Zona14: moved from server so client UI and examine can compute danger levels per type
    public static GeigerDangerLevel RadsToLevel(float rads)
    {
        return rads switch
        {
            < 0.2f => GeigerDangerLevel.None,
            < 1f => GeigerDangerLevel.Low,
            < 3f => GeigerDangerLevel.Med,
            < 6f => GeigerDangerLevel.High,
            _ => GeigerDangerLevel.Extreme
        };
    }

    public static Color LevelToColor(GeigerDangerLevel level)
    {
        switch (level)
        {
            case GeigerDangerLevel.None:
                return Color.Green;
            case GeigerDangerLevel.Low:
                return Color.Yellow;
            case GeigerDangerLevel.Med:
                return Color.DarkOrange;
            case GeigerDangerLevel.High:
            case GeigerDangerLevel.Extreme:
                return Color.Red;
            default:
                return Color.White;
        }
    }
}

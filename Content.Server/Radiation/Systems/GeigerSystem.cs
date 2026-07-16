using System.Linq; // Zona14
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Events;
using Content.Server._Zona14.MapRadiation; // Zona14
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly MapRadiationSystem _mapRadiation = default!; // Zona14
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private static readonly float ApproxEqual = 0.01f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, ActivateInWorldEvent>(OnActivate);

        SubscribeLocalEvent<GeigerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<GeigerComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<GeigerComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<GeigerComponent, GotUnequippedHandEvent>(OnUnequippedHand);

        SubscribeLocalEvent<RadiationSystemUpdatedEvent>(OnUpdate);
    }

    private void OnActivate(Entity<GeigerComponent> geiger, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || geiger.Comp.AttachedToSuit)
            return;
        args.Handled = true;

        SetEnabled(geiger, !geiger.Comp.IsEnabled);
    }

    private void OnEquipped(Entity<GeigerComponent> geiger, ref GotEquippedEvent args)
    {
        if (geiger.Comp.AttachedToSuit)
            SetEnabled(geiger, true);
        SetUser(geiger, args.Equipee);
    }

    private void OnEquippedHand(Entity<GeigerComponent> geiger, ref GotEquippedHandEvent args)
    {
        if (geiger.Comp.AttachedToSuit)
            return;

        SetUser(geiger, args.User);
    }

    private void OnUnequipped(Entity<GeigerComponent> geiger, ref GotUnequippedEvent args)
    {
        if (geiger.Comp.AttachedToSuit)
            SetEnabled(geiger, false);
        SetUser(geiger, null);
    }

    private void OnUnequippedHand(Entity<GeigerComponent> geiger, ref GotUnequippedHandEvent args)
    {
        if (geiger.Comp.AttachedToSuit)
            return;

        SetUser(geiger, null);
    }

    private void OnUpdate(RadiationSystemUpdatedEvent ev)
    {
        // update only active geiger counters
        // deactivated shouldn't have rad receiver component
        var query = EntityQueryEnumerator<GeigerComponent, RadiationReceiverComponent>();
        while (query.MoveNext(out var uid, out var geiger, out var receiver))
        {
            // Zona14: support configurable damage types and ambient map radiation
            var types = geiger.DamageTypes.Count > 0
                ? geiger.DamageTypes
                : DefaultRadiationTypes;

            var currentDamage = new Dictionary<string, float>();
            var rads = 0f;
            foreach (var damageType in types)
            {
                var typeId = damageType.Id.ToString();
                var value = 0f;
                if (receiver?.CurrentDamage != null &&
                    receiver.CurrentDamage.TryGetValue(typeId, out var receiverValue))
                {
                    value += receiverValue;
                }

                if (geiger.User is {} user)
                {
                    value += _mapRadiation.GetAmbientRadiation(user, typeId);
                }

                currentDamage[typeId] = value;
                rads += value;
            }

            geiger.CurrentDamage = currentDamage;
            SetCurrentRadiation(uid, geiger, rads);
            Dirty(uid, geiger);
            // End Zona14
        }
    }

    // Zona14: fallback list used when a geiger has no configured damage types
    private static readonly List<GeigerDamageType> DefaultRadiationTypes = new()
    {
        new GeigerDamageType { Id = "Radiation" }
    };

    private void SetCurrentRadiation(EntityUid uid, GeigerComponent component, float rads)
    {
        // check that it's approx equal
        if (MathHelper.CloseTo(component.CurrentRadiation, rads, ApproxEqual))
            return;

        var curLevel = component.DangerLevel;
        var newLevel = RadsToLevel(rads); // Zona14: uses shared helper now

        component.CurrentRadiation = rads;
        component.DangerLevel = newLevel;

        if (curLevel != newLevel)
        {
            UpdateAppearance(uid, component);
            UpdateSound(uid, component);
        }

        Dirty(uid, component);
    }

    private void SetUser(Entity<GeigerComponent> component, EntityUid? user)
    {
        if (component.Comp.User == user)
            return;

        component.Comp.User = user;
        Dirty(component);
        UpdateSound(component, component);
    }

    private void SetEnabled(Entity<GeigerComponent> geiger, bool isEnabled)
    {
        var component = geiger.Comp;
        if (component.IsEnabled == isEnabled)
            return;

        component.IsEnabled = isEnabled;
        if (!isEnabled)
        {
            component.CurrentRadiation = 0f;
            component.DangerLevel = GeigerDangerLevel.None;
        }

        _radiation.SetCanReceive(geiger, isEnabled);

        UpdateAppearance(geiger, component);
        UpdateSound(geiger, component);
        Dirty(geiger, component);
    }

    private void UpdateAppearance(EntityUid uid, GeigerComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, GeigerVisuals.IsEnabled, component.IsEnabled, appearance);
        _appearance.SetData(uid, GeigerVisuals.DangerLevel, component.DangerLevel, appearance);
    }

    private void UpdateSound(EntityUid uid, GeigerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Stream = _audio.Stop(component.Stream);

        if (!component.Sounds.TryGetValue(component.DangerLevel, out var sounds))
            return;

        var sound = _audio.ResolveSound(sounds);
        var param = sounds.Params.WithLoop(true).WithVolume(component.Volume);

        if (component.BroadcastAudio)
        {
            // For some reason PlayPvs sounds quieter even at distance 0, so we need to boost the volume a bit for consistency
            param = sounds.Params.WithLoop(true).WithVolume(component.Volume + 1.5f).WithMaxDistance(component.BroadcastRange);
            component.Stream = _audio.PlayPvs(sound, uid, param)?.Entity;
        }
        else if (component.User is not null && _player.TryGetSessionByEntity(component.User.Value, out var session))
            component.Stream = _audio.PlayGlobal(sound, session, param)?.Entity;
    }

}

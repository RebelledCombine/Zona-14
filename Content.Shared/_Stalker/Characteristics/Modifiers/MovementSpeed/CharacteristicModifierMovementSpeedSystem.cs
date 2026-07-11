using Content.Shared.Movement.Systems;

namespace Content.Shared._Stalker.Characteristics.Modifiers.MovementSpeed;

/// <summary>
/// Zona14: applies the (server-computed, networked) Dexterity movement speed modifier.
/// Lives in Content.Shared so it runs on the client too, keeping movement prediction in sync
/// with the server. The actual scalar is calculated server-side in
/// <c>CharacteristicModifierMovementSpeedComputeSystem</c>.
/// </summary>
public sealed class CharacteristicModifierMovementSpeedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacteristicModifierMovementSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, CharacteristicModifierMovementSpeedComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.CurrentModifier, component.CurrentModifier);
    }
}

using Content.Shared._Stalker.Characteristics;
using Content.Shared._Stalker.Characteristics.Modifiers.MovementSpeed;
using Content.Shared.Movement.Systems;

namespace Content.Server._Stalker.Characteristics.Modifiers.MovementSpeed;

/// <summary>
/// Zona14: server-side half of the Dexterity movement modifier. Reads the (server-only)
/// characteristic container, computes the speed scalar and stores it in the networked
/// <see cref="CharacteristicModifierMovementSpeedComponent.CurrentModifier"/> so the client can
/// predict the same movement speed. The scalar is applied in the shared
/// <c>CharacteristicModifierMovementSpeedSystem</c>.
/// </summary>
public sealed class CharacteristicModifierMovementSpeedComputeSystem : EntitySystem
{
    [Dependency] private readonly CharacteristicContainerSystem _characteristicContainer = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacteristicModifierMovementSpeedComponent, CharacteristicUpdatedEvent>(OnUpdate);
    }

    private void OnUpdate(Entity<CharacteristicModifierMovementSpeedComponent> modifier, ref CharacteristicUpdatedEvent args)
    {
        var newModifier = 1f;

        if (_characteristicContainer.TryGetValue(modifier.Owner, CharacteristicType.Dexterity, out var level) && level != 0)
        {
            var value = Math.Abs((float)level);
            var mod = level > 0
                ? modifier.Comp.PositiveModifier
                : modifier.Comp.NegativeModifier;

            newModifier = Math.Clamp(1f + value * mod, modifier.Comp.MinBonus, modifier.Comp.MaxBonus);
        }

        if (MathHelper.CloseTo(newModifier, modifier.Comp.CurrentModifier))
            return;

        modifier.Comp.CurrentModifier = newModifier;
        Dirty(modifier);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(modifier);
    }
}

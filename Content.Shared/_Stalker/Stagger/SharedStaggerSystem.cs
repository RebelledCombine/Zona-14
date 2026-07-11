using Content.Shared.Movement.Systems;

namespace Content.Shared._Stalker.Stagger;

/// <summary>
/// Zona14: applies the (server-computed, networked) stagger movement modifier. Lives in
/// Content.Shared so it runs on the client too, keeping movement prediction in sync with the
/// server. The scalar itself is computed server-side in <c>StaggerSystem</c>.
/// </summary>
public sealed class SharedStaggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaggerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, StaggerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.MovementSpeedModifier, component.MovementSpeedModifier);
    }
}

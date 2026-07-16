// SPDX-License-Identifier: MIT

using Content.Client._Zona14.Administration.Events;
using Content.Client.Administration.Managers;
using Content.Shared._Zona14.Administration;
using Content.Shared.DragDrop;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;

namespace Content.Client._Zona14.Administration.Systems;

/// <summary>
/// Allows admins in aghost mode to initiate a drag on any visible entity and
/// teleport it to wherever they release the cursor when no valid entity drop target is found.
/// </summary>
public sealed class AdminDragTeleportSystem : EntitySystem
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<DragNoTargetEvent>(OnDragNoTarget);
    }

    private bool IsAdminGhost()
    {
        if (!_adminManager.IsAdmin())
            return false;

        var localEntity = _playerManager.LocalEntity;
        if (localEntity == null)
            return false;

        return TryComp<GhostComponent>(localEntity.Value, out var ghost) && ghost.CanGhostInteract;
    }

    private void OnCanDrag(EntityUid uid, SpriteComponent component, ref CanDragEvent args)
    {
        if (!IsAdminGhost())
            return;

        args.Handled = true;
    }

    private void OnDragNoTarget(DragNoTargetEvent ev)
    {
        if (!IsAdminGhost())
            return;

        RaiseNetworkEvent(new AdminSelfDragTeleportEvent(
            GetNetEntity(ev.DraggedEntity),
            GetNetCoordinates(ev.TargetCoordinates)));

        ev.Handled = true;
    }
}

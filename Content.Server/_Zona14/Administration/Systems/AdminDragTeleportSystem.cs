// SPDX-License-Identifier: MIT

using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared._Zona14.Administration;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server._Zona14.Administration.Systems;

/// <summary>
/// Handles the <see cref="AdminSelfDragTeleportEvent"/> sent by an admin client
/// who drags their own controlled entity (in aghost mode) to empty space.
/// </summary>
public sealed class AdminDragTeleportSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<AdminSelfDragTeleportEvent>(OnAdminDragTeleport);
    }

    private void OnAdminDragTeleport(AdminSelfDragTeleportEvent ev, EntitySessionEventArgs args)
    {
        if (!_adminManager.IsAdmin(args.SenderSession))
            return;

        var senderEntity = args.SenderSession.AttachedEntity;
        if (senderEntity == null || !TryComp<GhostComponent>(senderEntity.Value, out var ghostComp) || !ghostComp.CanGhostInteract)
            return;

        var entity = GetEntity(ev.DraggedEntity);
        if (!Exists(entity))
            return;

        var targetCoords = GetCoordinates(ev.TargetCoordinates);
        if (!targetCoords.IsValid(EntityManager))
            return;

        _transform.SetCoordinates(entity, targetCoords);
        _transform.AttachToGridOrMap(entity);

        _adminLogger.Add(
            LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.SenderSession.AttachedEntity ?? entity):actor} drag-teleported {ToPrettyString(entity):subject} to {targetCoords}");
    }
}

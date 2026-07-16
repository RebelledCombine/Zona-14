// SPDX-License-Identifier: MIT

using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration;

/// <summary>
/// Sent from the client to the server requesting that <see cref="DraggedEntity"/>
/// be teleported to <see cref="TargetCoordinates"/>.
/// Only fulfilled server-side when the sender has admin privileges.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminSelfDragTeleportEvent : EntityEventArgs
{
    /// <summary>The entity that was dragged.</summary>
    public readonly NetEntity DraggedEntity;

    /// <summary>The map-relative coordinates the admin wishes to teleport to.</summary>
    public readonly NetCoordinates TargetCoordinates;

    public AdminSelfDragTeleportEvent(NetEntity draggedEntity, NetCoordinates targetCoordinates)
    {
        DraggedEntity = draggedEntity;
        TargetCoordinates = targetCoordinates;
    }
}

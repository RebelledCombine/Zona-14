// SPDX-License-Identifier: MIT

using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.Bwoink;

/// <summary>
/// Sent by the server to all admins when an AHelp channel assignment changes.
/// </summary>
[Serializable, NetSerializable]
public sealed class BwoinkAssignUpdated : EntityEventArgs
{
    public NetUserId Channel { get; }

    /// <summary>
    /// The assigned admin's display name, or null if the ticket is unassigned.
    /// </summary>
    public string? AdminName { get; }

    public BwoinkAssignUpdated(NetUserId channel, string? adminName)
    {
        Channel = channel;
        AdminName = adminName;
    }
}

// SPDX-License-Identifier: MIT

using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.Bwoink;

/// <summary>
/// Sent by a client to request an assignment change for an AHelp channel.
/// </summary>
[Serializable, NetSerializable]
public sealed class BwoinkAssignMessage : EntityEventArgs
{
    public NetUserId Channel { get; }

    /// <summary>
    /// If true, removes any assignment for this channel instead of assigning the sender.
    /// </summary>
    public bool Unassign { get; }

    public BwoinkAssignMessage(NetUserId channel, bool unassign = false)
    {
        Channel = channel;
        Unassign = unassign;
    }
}

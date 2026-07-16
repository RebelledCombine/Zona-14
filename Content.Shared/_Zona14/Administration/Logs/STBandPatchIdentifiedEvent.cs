// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.Logs;

/// <summary>
/// Sent from a client to the server when a player's band-patch recognition
/// reaches the Known state.
/// </summary>
[Serializable, NetSerializable]
public sealed class STBandPatchIdentifiedEvent : EntityEventArgs
{
    public readonly NetEntity Identifier;
    public readonly NetEntity Target;

    public STBandPatchIdentifiedEvent(NetEntity identifier, NetEntity target)
    {
        Identifier = identifier;
        Target = target;
    }
}

// SPDX-License-Identifier: MIT

using Content.Shared.Administration.Logs;
using Robust.Shared.Network;

namespace Content.Shared._Zona14.Administration.Logs;

/// <summary>
/// Holds an offline or online player reference for the admin log JSON/player-link pipeline.
/// </summary>
public readonly record struct AdminLogPlayerValue(NetUserId UserId, string? Name = null) : IAdminLogsPlayerValue
{
    public IEnumerable<NetUserId> Players => [UserId];

    public override string ToString() => Name ?? UserId.ToString();
}

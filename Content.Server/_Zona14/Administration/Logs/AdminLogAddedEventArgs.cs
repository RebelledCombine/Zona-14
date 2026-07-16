// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Content.Server.Database;
using Content.Shared.Database;

namespace Content.Server._Zona14.Administration.Logs;

/// <summary>
/// Event arguments raised by <see cref="IAdminLogManager.OnAdminLogAdded"/> after a new admin log is added.
/// </summary>
public sealed class AdminLogAddedEventArgs : EventArgs
{
    public AdminLog Log { get; }
    public IReadOnlyDictionary<string, object?> Values { get; }
    public IReadOnlyList<AdminLogPlayer> Players => Log.Players;
    public LogImpact Impact => Log.Impact;
    public LogType Type => Log.Type;

    public AdminLogAddedEventArgs(AdminLog log, IReadOnlyDictionary<string, object?> values)
    {
        Log = log;
        Values = values;
    }
}

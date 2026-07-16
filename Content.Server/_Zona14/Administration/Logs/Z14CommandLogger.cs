// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Server.Console;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Logs;

/// <summary>
/// Logs every admin console command invocation to IAdminLogManager.
/// This catches engine commands and any registered IConsoleCommand that requires admin flags.
/// </summary>
public sealed class Z14CommandLogger : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public override void Initialize()
    {
        base.Initialize();
        _consoleHost.AnyCommandExecuted += OnCommandExecuted;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _consoleHost.AnyCommandExecuted -= OnCommandExecuted;
    }

    private void OnCommandExecuted(IConsoleShell shell, string commandName, string argStr, string[] args)
    {
        if (!_adminManager.TryGetCommandFlags(commandName, out var flags))
            return;

        // AnyCommand or unassigned commands are not logged here.
        if (flags is null || flags.Length == 0)
            return;

        var impact = LogImpact.Low;
        if (flags.Any(f => f == AdminFlags.Ban || f == AdminFlags.Host))
            impact = LogImpact.Extreme;
        else if (flags.Any(f => f == AdminFlags.Admin || f == AdminFlags.Round || f == AdminFlags.Fun))
            impact = LogImpact.High;

        if (shell.Player is { } player)
        {
            _adminLog.Add(LogType.AdminCommands, impact,
                $"{player:player} executed {commandName:command} {argStr:args}");
        }
        else
        {
            _adminLog.Add(LogType.AdminCommands, impact,
                $"Server console executed {commandName:command} {argStr:args}");
        }
    }
}

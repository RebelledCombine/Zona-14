// SPDX-License-Identifier: MIT

using System.Text;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Adminhelp)]
public sealed class AHelpTranscriptCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public override string Command => "ahelptranscript";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var lookup = await _playerLocator.LookupIdByNameOrIdAsync(args[0]);
        if (lookup == null)
        {
            shell.WriteLine(Loc.GetString("cmd-ahelptranscript-not-found", ("target", args[0])));
            return;
        }

        var filter = new LogFilter
        {
            Types = new HashSet<LogType> { LogType.AdminMessage },
            AnyPlayers = new[] { lookup.UserId.UserId },
            IncludePlayers = true,
            IncludeNonPlayers = false,
            Limit = 1000,
        };

        var logs = await _adminLog.All(filter);

        if (logs.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-ahelptranscript-empty", ("target", lookup.Username)));
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("cmd-ahelptranscript-header", ("target", lookup.Username), ("count", logs.Count)));
        foreach (var log in logs)
        {
            sb.AppendLine($"[{log.Date:yyyy-MM-dd HH:mm:ss}] {log.Message}");
        }

        shell.WriteLine(sb.ToString());
    }
}

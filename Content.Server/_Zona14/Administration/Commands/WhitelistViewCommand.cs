// SPDX-License-Identifier: MIT

using System.Text;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Server.Players.JobWhitelist;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
///     Views all whitelisted CKEYs or the whitelist status of a specific player.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class WhitelistViewCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override string Command => "whitelistview";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            var whitelisted = await _dbManager.GetAllWhitelistedAsync();
            var builder = new StringBuilder();
            builder.AppendLine(Loc.GetString("cmd-whitelistview-header", ("count", whitelisted.Count)));

            foreach (var uid in whitelisted)
            {
                var located = await _locator.LookupIdAsync(uid);
                builder.AppendLine($"- {located?.Username ?? uid.ToString()} ({uid})");
            }

            shell.WriteLine(builder.ToString());
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data == null)
        {
            shell.WriteError(Loc.GetString("cmd-whitelistview-not-found", ("name", name)));
            return;
        }

        var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(data.UserId);
        var jobWhitelists = await _dbManager.GetJobWhitelists(data.UserId.UserId);

        shell.WriteLine(Loc.GetString("cmd-whitelistview-status",
            ("name", data.Username),
            ("userId", data.UserId),
            ("whitelisted", isWhitelisted),
            ("jobs", jobWhitelists.Count)));

        foreach (var job in jobWhitelists)
        {
            shell.WriteLine($"- {job}");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

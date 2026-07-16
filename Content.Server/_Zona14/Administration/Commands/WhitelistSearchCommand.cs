// SPDX-License-Identifier: MIT

using System.Text;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
///     Searches for players by name and shows their whitelist status.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class WhitelistSearchCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    public override string Command => "whitelistsearch";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Help);
            return;
        }

        var query = string.Join(' ', args).Trim();
        var records = await _dbManager.SearchPlayersByName(query);

        if (records.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-whitelistsearch-empty", ("query", query)));
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("cmd-whitelistsearch-header", ("count", records.Count), ("query", query)));

        foreach (var record in records)
        {
            var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(record.UserId);
            var jobCount = (await _dbManager.GetJobWhitelists(record.UserId.UserId)).Count;
            builder.AppendLine($"- {record.LastSeenUserName} ({record.UserId}) | Whitelisted: {isWhitelisted} | Job whitelists: {jobCount}");
        }

        shell.WriteLine(builder.ToString());
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

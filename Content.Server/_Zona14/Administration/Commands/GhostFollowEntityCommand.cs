// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Follower;
using Content.Shared.Ghost;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
/// Makes an admin ghost follow the given entity by NetEntity ID.
/// If the caller is not a ghost, they will be admin-ghosted automatically.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class GhostFollowEntityCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IServerConsoleHost _consoleHost = default!;

    public string Command => "ghostfollow";
    public string Description => "Makes your ghost follow the given entity by NetEntity ID.";
    public string Help => "ghostfollow <net entity id>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
        {
            shell.WriteError("Only players can use this command.");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEnt) || !_entManager.TryGetEntity(netEnt, out var target))
        {
            shell.WriteError($"Could not find entity with ID '{args[0]}'.");
            return;
        }

        if (target is not { } targetUid)
        {
            shell.WriteError($"Could not find entity with ID '{args[0]}'.");
            return;
        }

        var followerUid = player.AttachedEntity;
        if (followerUid is not { Valid: true } || !_entManager.TryGetComponent<GhostComponent>(followerUid.Value, out var ghost))
        {
            if (!_adminManager.IsAdmin(player))
            {
                shell.WriteError("You must be a ghost to use this command.");
                return;
            }

            _consoleHost.ExecuteCommand(player, "aghost");
            followerUid = player.AttachedEntity;
            if (followerUid is not { Valid: true } || !_entManager.TryGetComponent<GhostComponent>(followerUid.Value, out ghost))
            {
                shell.WriteError("Failed to enter admin ghost mode.");
                return;
            }
        }

        if (ghost is { CanGhostInteract: true } && _entManager.TrySystem<FollowerSystem>(out var follower))
        {
            follower.StartFollowingEntity(followerUid.Value, targetUid);
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{shell.Player?.Name ?? "Console"} started ghost-following {_entManager.ToPrettyString(targetUid)}");
        }
        else
            shell.WriteError("Could not start following the target.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("<net entity id>");

        return CompletionResult.Empty;
    }
}

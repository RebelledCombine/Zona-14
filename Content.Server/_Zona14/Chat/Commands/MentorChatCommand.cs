// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Chat.Commands;

[AdminCommand(AdminFlags.Admin)]
[AdminCommand(AdminFlags.Mentor)]
internal sealed class MentorChatCommand : LocalizedCommands
{
    [Dependency] private readonly IChatManager _chatManager = default!;

    public override string Command => "msay";

    public override string Description => Loc.GetString("cmd-msay-desc");

    public override string Help => Loc.GetString("cmd-msay-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;

        if (player == null)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        _chatManager.TrySendOOCMessage(player, message, OOCChatType.Mentor);
    }
}

// SPDX-License-Identifier: MIT

using Content.Client._Zona14.UserInterface.Systems.MentorHelp;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Client._Zona14.Commands;

[AnyCommand]
public sealed class OpenMentorHelpCommand : LocalizedCommands
{
    public override string Command => "openmentorhelp";
    public override string Description => Loc.GetString("cmd-openmentorhelp-desc");
    public override string Help => Loc.GetString("cmd-openmentorhelp-help", ("command", Command));

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var controller = IoCManager.Resolve<IUserInterfaceManager>().GetUIController<MentorHelpUIController>();

        if (args.Length == 0)
        {
            controller.Open();
            return;
        }

        if (Guid.TryParse(args[0], out var userId))
        {
            controller.Open(new Robust.Shared.Network.NetUserId(userId));
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-openmentorhelp-error"));
        }
    }
}

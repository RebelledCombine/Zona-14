using Content.Server.Administration.Managers; // Zona14
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Ban)]
    public sealed class PardonCommand : LocalizedCommands
    {
        [Dependency] private readonly IBanManager _banManager = default!; // Zona14: pardon via IBanManager

        public override string Command => "pardon";

        // Zona14: refactored server ban pardon to use IBanManager
        public override async void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine(Help);
                return;
            }

            if (!int.TryParse(args[0], out var banId))
            {
                shell.WriteLine(Loc.GetString("cmd-pardon-unable-to-parse", ("id", args[0]), ("help", Help)));
                return;
            }

            var result = await _banManager.PardonBan(banId, shell.Player?.UserId, DateTimeOffset.Now);
            shell.WriteLine(result);
        }
        // End Zona14
    }
}

// SPDX-License-Identifier: MIT

using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
///     Prints general admin status information: online players, admins, round, stations.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class Z14AdminInfoCommand : LocalizedCommands
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => "z14admininfo";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var gameTicker = _entityManager.System<GameTicker>();
        var stationSystem = _entityManager.System<StationSystem>();

        var builder = new StringBuilder();
        builder.AppendLine($"=== Z14 Admin Info ===");
        builder.AppendLine($"Round: {gameTicker.RoundId}");
        builder.AppendLine($"Run level: {gameTicker.RunLevel}");
        builder.AppendLine($"Online players: {_playerManager.PlayerCount}");
        builder.AppendLine($"Active admins: {_adminManager.ActiveAdmins.Count()}");
        builder.AppendLine($"All admins (including de-admin): {_adminManager.AllAdmins.Count()}");

        var stations = stationSystem.GetStations();
        builder.AppendLine($"Stations: {stations.Count}");
        foreach (var station in stations)
        {
            var name = _entityManager.GetComponent<MetaDataComponent>(station).EntityName;
            builder.AppendLine($"  - {name} ({station})");
        }

        shell.WriteLine(builder.ToString());
    }
}

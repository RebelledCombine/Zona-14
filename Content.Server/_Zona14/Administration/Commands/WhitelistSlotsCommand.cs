// SPDX-License-Identifier: MIT

using System.Text;
using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
///     Lists per-job slot counts for all stations.
/// </summary>
[AdminCommand(AdminFlags.Admin)]
public sealed class WhitelistSlotsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override string Command => "whitelistslots";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var stationSystem = _entityManager.System<StationSystem>();
        var stationJobs = _entityManager.System<StationJobsSystem>();
        var stations = stationSystem.GetStations();

        if (stations.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-whitelistslots-no-stations"));
            return;
        }

        var builder = new StringBuilder();
        foreach (var station in stations)
        {
            var name = _entityManager.GetComponent<MetaDataComponent>(station).EntityName;
            builder.AppendLine(Loc.GetString("cmd-whitelistslots-station", ("name", name), ("station", station)));

            foreach (var (jobId, slots) in stationJobs.GetJobs(station))
            {
                var displayName = _prototypes.TryIndex<JobPrototype>(jobId, out var jobProto)
                    ? jobProto.LocalizedName
                    : jobId.ToString();

                var slotText = slots == null ? Loc.GetString("cmd-whitelistslots-infinite") : slots.Value.ToString();
                builder.AppendLine($"  - {displayName}: {slotText}");
            }
        }

        shell.WriteLine(builder.ToString());
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}

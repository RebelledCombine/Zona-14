// SPDX-License-Identifier: MIT

using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Shared._Stalker_EN.Emission;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;

namespace Content.Server._Zona14.AnomalyMigration;

public sealed class Z14AnomalyMigrationEmissionSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmissionStateChangedEvent>(OnEmissionStateChanged);
    }

    private void OnEmissionStateChanged(ref EmissionStateChangedEvent ev)
    {
        var query = EntityQueryEnumerator<Z14AnomalyMigrationEmissionComponent, GameRuleComponent>();

        while (query.MoveNext(out var uid, out var emission, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (ev.IsActive && !emission.TriggerAtStart)
                continue;

            if (!ev.IsActive && !emission.TriggerAtEnd)
                continue;

            TriggerMigration(uid, emission);
        }
    }

    private void TriggerMigration(EntityUid uid, Z14AnomalyMigrationEmissionComponent emission)
    {
        var ent = _gameTicker.AddGameRule("Z14AnomalyMigrationRule");

        if (TryComp<Z14AnomalyMigrationRuleComponent>(ent, out var rule))
        {
            rule.MigrateAll = emission.MigrateAll;

            if (emission.RegenerateDelay > TimeSpan.Zero)
                rule.RegenerateDelay = emission.RegenerateDelay;
        }

        if (TryComp<GameRuleComponent>(ent, out var gameRule))
            gameRule.Delay = null;

        _gameTicker.StartGameRule(ent);

        _adminLog.Add(LogType.Z14AnomalyMigration, LogImpact.High,
            $"Z14 anomaly migration triggered by emission on {uid:emission}");
    }
}

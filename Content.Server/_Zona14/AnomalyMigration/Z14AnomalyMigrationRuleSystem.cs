// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using Content.Server._Stalker.Anomaly.Generation.Components;
using Content.Server._Stalker.Anomaly.Generation.Systems;
using Content.Server._Stalker.Map;
using Content.Server._Stalker.StationEvents.Components;
using Content.Server.Chat.Systems;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._Stalker.Anomaly.Data;
using Content.Shared._Stalker.Anomaly.Prototypes;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.AnomalyMigration;

public sealed class Z14AnomalyMigrationRuleSystem : StationEventSystem<Z14AnomalyMigrationRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly STAnomalyGeneratorSystem _anomalyGenerator = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, Z14AnomalyMigrationRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
    }

    protected override void Started(EntityUid uid, Z14AnomalyMigrationRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryPickTargets(component))
        {
            Sawmill.Warning("No valid anomaly migration target found");
            ForceEndSelf(uid, gameRule);
            return;
        }

        if (component.ForcedCount > 0)
            component.MigrationCount = component.ForcedCount;

        component.Phase = Z14AnomalyMigrationPhase.Clear;
        component.NextAction = Timing.CurTime;

        AdminLogManager.Add(LogType.Z14AnomalyMigration, LogImpact.High,
            $"Z14 anomaly migration started for {(component.MigrateAll ? "all maps" : component.TargetMapName ?? "unknown")} (count {component.MigrationCount})");

        if (component.MigrateAll)
        {
            ChatSystem.DispatchFilteredAnnouncement(
                Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame),
                Loc.GetString("z14-anomaly-migration-start-all"),
                playSound: false);
        }
        else if (component.TargetMapName != null)
        {
            ChatSystem.DispatchFilteredAnnouncement(
                Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame),
                Loc.GetString("z14-anomaly-migration-start", ("map", component.TargetMapName)),
                playSound: false);
        }
    }

    protected override void ActiveTick(EntityUid uid, Z14AnomalyMigrationRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        switch (component.Phase)
        {
            case Z14AnomalyMigrationPhase.Clear:
                TickClear(uid, component);
                break;
            case Z14AnomalyMigrationPhase.Regenerate:
                TickRegenerate(uid, component);
                break;
            case Z14AnomalyMigrationPhase.Wait:
                TickWait(uid, component);
                break;
        }
    }

    protected override void Ended(EntityUid uid, Z14AnomalyMigrationRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        if (component.MigrateAll)
        {
            ChatSystem.DispatchFilteredAnnouncement(
                Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame),
                Loc.GetString("z14-anomaly-migration-end-all"),
                playSound: false);
        }
        else if (component.TargetMapName != null)
        {
            ChatSystem.DispatchFilteredAnnouncement(
                Filter.Empty().AddWhere(GameTicker.UserHasJoinedGame),
                Loc.GetString("z14-anomaly-migration-end", ("map", component.TargetMapName)),
                playSound: false);
        }
    }

    /// <summary>
    /// Trigger a random single-map anomaly migration event.
    /// </summary>
    public void Trigger()
    {
        Trigger(false);
    }

    /// <summary>
    /// Trigger an anomaly migration event. If <paramref name="migrateAll"/> is true, all valid
    /// <see cref="STAnomalyGeneratorTargetComponent"/> maps are migrated.
    /// </summary>
    public void Trigger(bool migrateAll)
    {
        var ent = GameTicker.AddGameRule("Z14AnomalyMigrationRule");
        if (!TryComp<Z14AnomalyMigrationRuleComponent>(ent, out var component))
            return;

        component.MigrateAll = migrateAll;

        if (TryComp<GameRuleComponent>(ent, out var gameRule))
            gameRule.Delay = null;

        GameTicker.StartGameRule(ent);
    }

    /// <summary>
    /// Trigger an anomaly migration on a specific map.
    /// </summary>
    public void Trigger(MapId mapId, ProtoId<STAnomalyGenerationOptionsPrototype> optionsId, int count)
    {
        var ent = GameTicker.AddGameRule("Z14AnomalyMigrationRule");
        if (!TryComp<Z14AnomalyMigrationRuleComponent>(ent, out var component))
            return;

        component.ForcedMapId = mapId;
        component.ForcedOptionsId = optionsId;
        component.ForcedCount = count;

        if (TryComp<GameRuleComponent>(ent, out var gameRule))
            gameRule.Delay = null;

        GameTicker.StartGameRule(ent);
    }

    /// <summary>
    /// Resolves a map by its STMapKey or numeric MapId to a usable migration target.
    /// </summary>
    public bool TryResolveMigrationTarget(string? mapKey, MapId? mapId, out MapId targetMapId, out ProtoId<STAnomalyGenerationOptionsPrototype> optionsId, out string mapName)
    {
        targetMapId = MapId.Nullspace;
        optionsId = default;
        mapName = string.Empty;

        if (mapId != null)
        {
            return TryResolveMap(mapId.Value, out targetMapId, out optionsId, out mapName);
        }

        if (string.IsNullOrEmpty(mapKey))
            return false;

        var query = EntityQueryEnumerator<MapComponent, STMapKeyComponent>();
        while (query.MoveNext(out var mapUid, out var mapComp, out var keyComp))
        {
            if (!keyComp.Value.Equals(mapKey, StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryResolveMap(mapComp.MapId, out targetMapId, out optionsId, out mapName))
                return true;
        }

        return false;
    }

    private bool TryPickTargets(Z14AnomalyMigrationRuleComponent component)
    {
        component.Targets.Clear();
        component.MigrationTasks.Clear();

        if (component.ForcedMapId != null)
        {
            if (TryResolveForcedTarget(component, out var target))
            {
                component.Targets.Add(target);
                component.TargetMapName = target.MapName;
                return true;
            }

            return false;
        }

        var candidates = FindAllTargets(component);
        if (candidates.Count == 0)
            return false;

        if (component.MigrateAll)
        {
            component.Targets.AddRange(candidates);
            component.TargetMapName = null;
            return true;
        }

        var candidate = RobustRandom.Pick(candidates);
        component.Targets.Add(candidate);
        component.TargetMapName = candidate.MapName;
        return true;
    }

    private bool TryResolveForcedTarget(Z14AnomalyMigrationRuleComponent component, out Z14AnomalyMigrationTarget target)
    {
        target = default;
        var mapId = component.ForcedMapId!.Value;
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (Deleted(mapUid))
            return false;

        var mapKey = TryComp<STMapKeyComponent>(mapUid, out var keyComp) ? keyComp.Value : null;
        var mapName = GetMapName(mapUid);
        ProtoId<STAnomalyGenerationOptionsPrototype> optionsId;

        if (component.ForcedOptionsId != null)
        {
            optionsId = component.ForcedOptionsId.Value;
        }
        else if (TryComp<STAnomalyGeneratorTargetComponent>(mapUid, out var targetComp)
                 && PrototypeManager.TryIndex(targetComp.OptionsId, out _))
        {
            optionsId = targetComp.OptionsId;
        }
        else if (component.AllowFallback && component.FallbackOptions != null
                 && PrototypeManager.TryIndex(component.FallbackOptions.Value, out _))
        {
            optionsId = component.FallbackOptions.Value;
        }
        else
        {
            return false;
        }

        target = new Z14AnomalyMigrationTarget(mapId, mapKey, mapName, optionsId);
        return true;
    }

    private List<Z14AnomalyMigrationTarget> FindAllTargets(Z14AnomalyMigrationRuleComponent component)
    {
        var candidates = new List<Z14AnomalyMigrationTarget>();
        var query = EntityQueryEnumerator<MapComponent>();

        while (query.MoveNext(out var mapUid, out var mapComp))
        {
            var mapId = mapComp.MapId;
            if (mapId == MapId.Nullspace)
                continue;

            if (component.SkipSafeMaps && HasComp<StalkerSafeZoneComponent>(mapUid))
                continue;

            var mapKey = TryComp<STMapKeyComponent>(mapUid, out var keyComp) ? keyComp.Value : null;
            var mapName = GetMapName(mapUid);

            if (TryComp<STAnomalyGeneratorTargetComponent>(mapUid, out var target)
                && PrototypeManager.TryIndex(target.OptionsId, out _))
            {
                candidates.Add(new Z14AnomalyMigrationTarget(mapId, mapKey, mapName, target.OptionsId));
                continue;
            }

            if (component.AllowFallback && component.FallbackOptions != null
                && PrototypeManager.TryIndex(component.FallbackOptions.Value, out _))
            {
                candidates.Add(new Z14AnomalyMigrationTarget(mapId, mapKey, mapName, component.FallbackOptions.Value));
            }
        }

        return candidates;
    }

    private bool TryResolveMap(MapId mapId, out MapId targetMapId, out ProtoId<STAnomalyGenerationOptionsPrototype> optionsId, out string mapName)
    {
        targetMapId = mapId;
        optionsId = default;
        mapName = string.Empty;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        if (Deleted(mapUid))
            return false;

        if (!TryComp<STAnomalyGeneratorTargetComponent>(mapUid, out var target)
            || !PrototypeManager.TryIndex(target.OptionsId, out _))
        {
            return false;
        }

        mapName = GetMapName(mapUid);
        optionsId = target.OptionsId;
        return true;
    }

    private string GetMapName(EntityUid mapUid)
    {
        if (TryComp<STMapKeyComponent>(mapUid, out var keyComp))
            return keyComp.Value;

        return MetaData(mapUid).EntityName;
    }

    private void TickClear(EntityUid uid, Z14AnomalyMigrationRuleComponent component)
    {
        if (Timing.CurTime < component.NextAction)
            return;

        if (TryComp<StationEventComponent>(uid, out var stationEvent))
            stationEvent.EndTime = null;

        foreach (var target in component.Targets)
        {
            _anomalyGenerator.ClearGeneration(target.MapId);
            AdminLogManager.Add(LogType.Z14AnomalyMigration, LogImpact.Medium,
                $"Cleared anomalies on {target.MapName} before migration");
        }

        component.NextAction = Timing.CurTime + component.RegenerateDelay;
        component.Phase = Z14AnomalyMigrationPhase.Regenerate;
    }

    private void TickRegenerate(EntityUid uid, Z14AnomalyMigrationRuleComponent component)
    {
        if (Timing.CurTime < component.NextAction)
            return;

        foreach (var target in component.Targets)
        {
            if (!PrototypeManager.TryIndex(target.OptionsId, out var proto))
            {
                Sawmill.Error("Anomaly migration target options are invalid for {0}", target.MapName);
                if (TryComp<StationEventComponent>(uid, out var stationEvent))
                    stationEvent.EndTime = Timing.CurTime;
                component.Phase = Z14AnomalyMigrationPhase.Complete;
                return;
            }

            var count = proto.Options.TotalCount;

            if (target.MapKey != null
                && component.MapOverrides.TryGetValue(target.MapKey, out var overrideCount)
                && overrideCount > 0)
            {
                count = overrideCount;
            }
            else if (component.MigrationCount > 0)
            {
                count = component.MigrationCount;
            }

            var options = new STAnomalyGenerationOptions
            {
                MapId = target.MapId,
                TotalCount = count,
                AnomalyEntries = new HashSet<STAnomalyGeneratorAnomalyEntry>(proto.Options.AnomalyEntries)
            };

            var task = _anomalyGenerator.StartGeneration(target.MapId, options);
            component.MigrationTasks.Add(task);

            AdminLogManager.Add(LogType.Z14AnomalyMigration, LogImpact.Medium,
                $"Regenerating {count} anomalies on {target.MapName}");
        }

        component.Phase = Z14AnomalyMigrationPhase.Wait;
    }

    private void TickWait(EntityUid uid, Z14AnomalyMigrationRuleComponent component)
    {
        if (component.MigrationTasks.Count == 0)
        {
            if (TryComp<StationEventComponent>(uid, out var stationEvent))
                stationEvent.EndTime = Timing.CurTime;
            component.Phase = Z14AnomalyMigrationPhase.Complete;
            return;
        }

        var allCompleted = true;
        foreach (var task in component.MigrationTasks)
        {
            if (!task.IsCompleted)
            {
                allCompleted = false;
                break;
            }

            if (task.IsFaulted)
            {
                Sawmill.Error("Anomaly migration job failed: {0}", task.Exception);
            }
        }

        if (!allCompleted)
            return;

        if (TryComp<StationEventComponent>(uid, out var endEvent))
            endEvent.EndTime = Timing.CurTime;

        component.Phase = Z14AnomalyMigrationPhase.Complete;

        AdminLogManager.Add(LogType.Z14AnomalyMigration, LogImpact.High,
            $"Z14 anomaly migration completed for {(component.MigrateAll ? "all maps" : component.TargetMapName ?? "unknown")}");
    }
}

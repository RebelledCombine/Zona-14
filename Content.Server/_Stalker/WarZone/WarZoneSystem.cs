using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server._Stalker.WarZone;
using Content.Shared._Stalker.WarZone.Requirenments;
using Content.Server.Database;
using Content.Shared._Stalker.WarZone;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Events;
using Content.Server.Chat.Managers;
using Content.Server.Popups;
using Content.Shared.Mobs;
using Content.Shared._Stalker.Bands;
using Content.Shared.NPC.Prototypes;

namespace Content.Server._Stalker.WarZone;

public sealed partial class WarZoneSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private readonly Dictionary<EntityUid, CaptureState> _activeCaptures = new();
    private readonly Dictionary<EntityUid, TimeSpan> _lastRewardTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WarZoneComponent, ComponentInit>(OnWarZoneInit);
        SubscribeLocalEvent<WarZoneComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<WarZoneComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<MetaDataComponent, EntityTerminatingEvent>(OnEntityTerminating);
    }

    private void OnWarZoneInit(EntityUid uid, WarZoneComponent component, ComponentInit args)
    {
        _ = InitializeWarZoneAsync(uid, component);
    }

    private async Task InitializeWarZoneAsync(EntityUid uid, WarZoneComponent component)
    {
        try
        {
            // Sync all bands
            foreach (var bandProto in _prototypeManager.EnumeratePrototypes<STBandPrototype>())
            {
                var band = await _dbManager.GetStalkerBandAsync(bandProto.ID);
                if (band == null)
                    await _dbManager.SetStalkerBandAsync(bandProto.ID, 0);
            }

            // Sync all factions
            foreach (var factionProto in _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>())
            {
                var faction = await _dbManager.GetStalkerFactionAsync(factionProto.ID);
                if (faction == null)
                    await _dbManager.SetStalkerFactionAsync(factionProto.ID, 0);
            }

            if (!_activeCaptures.TryGetValue(uid, out var state))
            {
                state = new CaptureState { InitialLoadComplete = false };
                _activeCaptures[uid] = state;

                _ = LoadInitialZoneStateAsync(uid, component.ZoneProto, state);
            }

            var ownership = await _dbManager.GetStalkerWarOwnershipAsync(component.ZoneProto);
            if (ownership != null && (ownership.BandId != null || ownership.FactionId != null))
            {
                var lastRewardTime = ownership.LastCapturedByCurrentOwnerAt.HasValue
                    ? _gameTiming.CurTime - (DateTime.UtcNow - ownership.LastCapturedByCurrentOwnerAt.Value)
                    : _gameTiming.CurTime;

                _lastRewardTimes[uid] = lastRewardTime;

                Logger.InfoS("warzone", $"Initialized reward timing for zone '{component.PortalName}', owned by {(ownership.BandId != null ? $"band:{ownership.BandId}" : $"faction:{ownership.FactionId}")}");
            }
        }
        catch (Exception ex)
        {
            Logger.ErrorS("warzone", $"Error initializing war zone {component.ZoneProto}: {ex}");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _gameTiming.CurTime;

        foreach (var (zone, state) in _activeCaptures)
        {
            _ = UpdateCaptureAsync(zone, state, now);
        }

        foreach (var (zone, lastRewardTime) in _lastRewardTimes)
        {
            DistributeRewards(zone, lastRewardTime, now);
        }
    }

    private async Task UpdateCaptureAsync(EntityUid zone, CaptureState state, TimeSpan now)
    {
        var frameTimeSec = (float)_gameTiming.FrameTime.TotalSeconds;

        if (!_entityManager.TryGetComponent(zone, out WarZoneComponent? wzComp) ||
            !_prototypeManager.TryIndex<STWarZonePrototype>(wzComp.ZoneProto, out var wzProto))
            return;

        if (state.PresentBandIds.Count > 1 || state.PresentFactionIds.Count > 1)
        {
            ResetAllRequirements(zone);
            return;
        }

        if (state.PresentBandIds.Count == 0 && state.PresentFactionIds.Count == 0)
        {
            ResetAllRequirements(zone);

            if (state.CurrentAttackerBandId != null || state.CurrentAttackerFactionId != null)
            {
                if ((state.CurrentAttackerBandId != state.DefendingBandId || state.CurrentAttackerFactionId != state.DefendingFactionId) &&
                    (state.CurrentAttackerBandId != null || state.CurrentAttackerFactionId != null))
                {
                    string attackerName = GetAttackerName(state.CurrentAttackerBandId, state.CurrentAttackerFactionId);
                    _chatManager.DispatchServerAnnouncement($"Capture attempt on '{wzComp.PortalName}' by {attackerName} was abandoned!");
                }
                state.CurrentAttackerBandId = null;
                state.CurrentAttackerFactionId = null;
                state.PresentBandIds.Clear();
                state.PresentFactionIds.Clear();
            }

            return;
        }

        int? attackerBand = null;
        int? attackerFaction = null;

        if (state.PresentBandIds.Count == 1)
            attackerBand = GetFirst(state.PresentBandIds);
        if (state.PresentFactionIds.Count == 1)
            attackerFaction = GetFirst(state.PresentFactionIds);

        if ((attackerBand != null && attackerBand == state.DefendingBandId) ||
            (attackerFaction != null && attackerFaction == state.DefendingFactionId))
        {
            ResetAllRequirements(zone);
            return;
        }

        bool isDefender = (attackerBand != null && attackerBand == state.DefendingBandId) ||
                          (attackerFaction != null && attackerFaction == state.DefendingFactionId);

        bool zoneCooldownActive = state.CooldownEndTime.HasValue && now < state.CooldownEndTime.Value;

        if (!isDefender && !zoneCooldownActive && state.InitialLoadComplete &&
            (attackerBand != state.CurrentAttackerBandId || attackerFaction != state.CurrentAttackerFactionId) &&
            (attackerBand != null || attackerFaction != null))
        {
            state.CurrentAttackerBandId = attackerBand;
            state.CurrentAttackerFactionId = attackerFaction;

            string attackerName = GetAttackerName(attackerBand, attackerFaction);
            _chatManager.DispatchServerAnnouncement($"Capture attempt by {attackerName} started on '{wzComp.PortalName}'!");
        }

        var ownerships = new Dictionary<ProtoId<STWarZonePrototype>, (int? BandId, int? FactionId)>();
        var lastCaptureTimes = new Dictionary<ProtoId<STWarZonePrototype>, DateTime?>();
        var zonePrototypes = new Dictionary<ProtoId<STWarZonePrototype>, STWarZonePrototype>();

        var requiredZoneIds = new HashSet<ProtoId<STWarZonePrototype>>();

        if (wzProto.Requirements != null)
        {
            foreach (var req in wzProto.Requirements)
            {
                if (req is ZoneOwnershipRequirenment zoneReq)
                {
                    foreach (var rid in zoneReq.RequiredZones)
                        requiredZoneIds.Add(rid);
                }
            }
        }

        foreach (var rid in requiredZoneIds)
        {
            var ownership = await _dbManager.GetStalkerWarOwnershipAsync(rid);
            if (ownership != null)
            {
                ownerships[rid] = (ownership.BandId, ownership.FactionId);
                lastCaptureTimes[rid] = ownership.LastCapturedByCurrentOwnerAt;
            }

            if (_prototypeManager.TryIndex<STWarZonePrototype>(rid, out var reqProto))
            {
                zonePrototypes[rid] = reqProto;
            }
        }

        var allMet = true;

        if (wzProto.Requirements != null)
        {
            foreach (var req in wzProto.Requirements)
            {
                var blockReason = req.Check(attackerBand, attackerFaction, ownerships, lastCaptureTimes, zonePrototypes, wzComp.ZoneProto, frameTimeSec);
                if (blockReason != CaptureBlockReason.None)
                {
                    allMet = false;
                    break;
                }
            }
        }

        if (!allMet)
            return;

        state.DefendingBandId = attackerBand;
        state.DefendingFactionId = attackerFaction;

        ProtoId<STBandPrototype>? bandProtoId = null;
        ProtoId<NpcFactionPrototype>? factionProtoId = null;

        if (attackerBand.HasValue)
        {
            foreach (var bandProto in _prototypeManager.EnumeratePrototypes<STBandPrototype>())
            {
                if (bandProto.DatabaseId == attackerBand.Value)
                {
                    bandProtoId = bandProto.ID;
                    break;
                }
            }
        }

        if (attackerFaction.HasValue)
        {
            foreach (var factionProto in _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>())
            {
                if (factionProto.DatabaseId == attackerFaction.Value)
                {
                    factionProtoId = factionProto.ID;
                    break;
                }
            }
        }

        if (bandProtoId != null && factionProtoId != null)
            bandProtoId = null;

        await _dbManager.SetStalkerZoneOwnershipAsync(
            wzComp.ZoneProto,
            bandProtoId,
            factionProtoId);

        if (_activeCaptures.TryGetValue(zone, out var captureState) && wzProto.CaptureCooldownHours > 0)
        {
            captureState.CooldownEndTime = _gameTiming.CurTime + TimeSpan.FromHours(wzProto.CaptureCooldownHours);
        }

        string defenderName = GetAttackerName(state.DefendingBandId, state.DefendingFactionId);
        _chatManager.DispatchServerAnnouncement($"Zone '{wzComp.PortalName}' captured by {defenderName}!");
    }

    private void ResetAllRequirements(EntityUid zone)
    {
        if (!_entityManager.TryGetComponent(zone, out WarZoneComponent? wzComp))
            return;

        if (!_prototypeManager.TryIndex<STWarZonePrototype>(wzComp.ZoneProto, out var wzProto))
            return;

        if (wzProto.Requirements == null)
            return;

        foreach (var req in wzProto.Requirements)
        {
            if (req is CaptureTimeRequirenment captureReq)
                captureReq.Reset();
        }
    }

    private void DistributeRewards(EntityUid zone, TimeSpan lastRewardTime, TimeSpan now)
    {
        if (!_entityManager.TryGetComponent(zone, out WarZoneComponent? wzComp))
            return;

        if (!_prototypeManager.TryIndex<STWarZonePrototype>(wzComp.ZoneProto, out var wzProto))
            return;

        var period = TimeSpan.FromSeconds(wzProto.RewardPeriod);

        if (now - lastRewardTime < period)
            return;

        if (!_activeCaptures.TryGetValue(zone, out var state))
            return;

        if (state.DefendingBandId == null && state.DefendingFactionId == null && wzProto.ShouldAwardWhenDefenderPresent)
            return;

        var points = wzProto.RewardPointsPerPeriod;
        bool rewarded = false;

        if (state.DefendingBandId.HasValue)
        {
            foreach (var bandProto in _prototypeManager.EnumeratePrototypes<STBandPrototype>())
            {
                if (bandProto.DatabaseId == state.DefendingBandId.Value)
                {
                    _dbManager.SetStalkerBandAsync(new ProtoId<STBandPrototype>(bandProto.ID), (int)points);
                    Logger.InfoS("warzone", $"Awarded {points} points to band {bandProto.Name} for controlling {wzComp.PortalName}");
                    rewarded = true;
                    break;
                }
            }
        }
        else if (state.DefendingFactionId.HasValue)
        {
            foreach (var factionProto in _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>())
            {
                if (factionProto.DatabaseId == state.DefendingFactionId.Value)
                {
                    _dbManager.SetStalkerFactionAsync(new ProtoId<NpcFactionPrototype>(factionProto.ID), (int)points);
                    Logger.InfoS("warzone", $"Awarded {points} points to faction {factionProto.ID} for controlling {wzComp.PortalName}");
                    rewarded = true;
                    break;
                }
            }
        }

        if (rewarded)
        {
            _lastRewardTimes[zone] = now;
        }
    }

    private static int? GetFirst(HashSet<int> set)
    {
        foreach (var g in set)
            return g;
        return null;
    }

    private void OnStartCollide(EntityUid uid, WarZoneComponent component, ref readonly StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!_entityManager.TryGetComponent(other, out BandsComponent? bands))
            return;

        var bandProtoId = bands.BandProto;
        int? bandDbId = null;
        int? factionDbId = null;

        if (_prototypeManager.TryIndex<STBandPrototype>(bandProtoId, out var bandProto))
        {
            bandDbId = bandProto.DatabaseId;

            if (_prototypeManager.TryIndex<NpcFactionPrototype>(bandProto.FactionId, out var factionProto))
            {
                factionDbId = factionProto.DatabaseId;
            }
        }

        if (!_activeCaptures.TryGetValue(uid, out var state))
        {
            state = new CaptureState { InitialLoadComplete = false };
            _activeCaptures[uid] = state;
            _ = Task.Run(() => LoadInitialZoneStateAsync(uid, component.ZoneProto, state));
        }

        var now = _gameTiming.CurTime;
        if (state.CooldownEndTime.HasValue && now < state.CooldownEndTime.Value)
        {
            var remainingTime = state.CooldownEndTime.Value - now;
            var portalName = component.PortalName ?? "Unknown Zone";
            var message = $"Zone '{portalName}' is on cooldown. Next capture available in {remainingTime.TotalMinutes:F1} minutes.";
            _popup.PopupEntity(message, other);
        }

        if (bandDbId.HasValue)
            state.PresentBandIds.Add(bandDbId.Value);
        if (factionDbId.HasValue)
            state.PresentFactionIds.Add(factionDbId.Value);
    }

    private void OnEndCollide(EntityUid uid, WarZoneComponent component, ref readonly EndCollideEvent args)
    {
        var other = args.OtherEntity;

        if (!_entityManager.TryGetComponent(other, out BandsComponent? bands))
            return;

        var bandProtoId = bands.BandProto;
        int? bandDbId = null;
        int? factionDbId = null;

        if (_prototypeManager.TryIndex<STBandPrototype>(bandProtoId, out var bandProto))
        {
            bandDbId = bandProto.DatabaseId;

            if (_prototypeManager.TryIndex<NpcFactionPrototype>(bandProto.FactionId, out var factionProto))
            {
                factionDbId = factionProto.DatabaseId;
            }
        }

        if (!_activeCaptures.TryGetValue(uid, out var state))
            return;

        if (bandDbId.HasValue)
            state.PresentBandIds.Remove(bandDbId.Value);
        if (factionDbId.HasValue)
            state.PresentFactionIds.Remove(factionDbId.Value);
    }

    private void OnEntityTerminating(EntityUid uid, MetaDataComponent component, ref EntityTerminatingEvent args)
    {
        RemoveEntityFromAllCaptures(uid);
    }

    private void RemoveEntityFromAllCaptures(EntityUid uid)
    {
        if (!_entityManager.TryGetComponent(uid, out BandsComponent? bands))
            return;

        int? bandDbId = null;
        int? factionDbId = null;

        if (_prototypeManager.TryIndex<STBandPrototype>(bands.BandProto, out var bandProto))
        {
            bandDbId = bandProto.DatabaseId;

            if (_prototypeManager.TryIndex<NpcFactionPrototype>(bandProto.FactionId, out var factionProto))
            {
                factionDbId = factionProto.DatabaseId;
            }
        }

        foreach (var (zone, state) in _activeCaptures)
        {
            bool changed = false;

            if (bandDbId.HasValue && state.PresentBandIds.Remove(bandDbId.Value))
                changed = true;

            if (factionDbId.HasValue && state.PresentFactionIds.Remove(factionDbId.Value))
                changed = true;

            if (changed && state.PresentBandIds.Count == 0 && state.PresentFactionIds.Count == 0)
            {
                ResetAllRequirements(zone);

                if ((state.CurrentAttackerBandId != null || state.CurrentAttackerFactionId != null) &&
                    _entityManager.TryGetComponent(zone, out WarZoneComponent? wzComp))
                {
                    string attackerName = GetAttackerName(state.CurrentAttackerBandId, state.CurrentAttackerFactionId);
                    _chatManager.DispatchServerAnnouncement($"Capture attempt on '{wzComp.PortalName}' by {attackerName} was abandoned!");
                }

                state.CurrentAttackerBandId = null;
                state.CurrentAttackerFactionId = null;
                state.PresentBandIds.Clear();
                state.PresentFactionIds.Clear();
            }
        }
    }

    private string GetAttackerName(int? bandId, int? factionId)
    {
        if (bandId.HasValue)
        {
            foreach (var bandProto in _prototypeManager.EnumeratePrototypes<STBandPrototype>())
            {
                if (bandProto.DatabaseId == bandId.Value)
                    return bandProto.Name;
            }
        }
        else if (factionId.HasValue)
        {
            foreach (var factionProto in _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>())
            {
                if (factionProto.DatabaseId == factionId.Value)
                    return factionProto.ID;
            }
        }
        return "Unknown";
    }

    private sealed class CaptureState
    {
        public int? DefendingBandId;
        public int? DefendingFactionId;

        public int? CurrentAttackerBandId;
        public int? CurrentAttackerFactionId;
        public TimeSpan? CooldownEndTime;

        public bool InitialLoadComplete = true;

        public HashSet<int> PresentBandIds = new();
        public HashSet<int> PresentFactionIds = new();
    }

    private async Task LoadInitialZoneStateAsync(EntityUid zoneUid, ProtoId<STWarZonePrototype> zoneProtoId, CaptureState state)
    {
        try
        {
            if (!_prototypeManager.TryIndex<STWarZonePrototype>(zoneProtoId, out var wzProto))
            {
                Logger.ErrorS("warzone", $"Could not find STWarZonePrototype with ID '{zoneProtoId}' during async state load for zone {zoneUid}.");
                return;
            }

            var ownership = await _dbManager.GetStalkerWarOwnershipAsync(zoneProtoId);

            if (ownership != null && ownership.LastCapturedByCurrentOwnerAt.HasValue && wzProto.CaptureCooldownHours > 0)
            {
                DateTime captureTime = ownership.LastCapturedByCurrentOwnerAt.Value;
                DateTime cooldownEndDateTime = captureTime.AddHours(wzProto.CaptureCooldownHours);
                DateTime currentDateTime = DateTime.UtcNow;

                if (cooldownEndDateTime > currentDateTime)
                {
                    TimeSpan remainingCooldown = cooldownEndDateTime - currentDateTime;
                    state.CooldownEndTime = _gameTiming.CurTime + remainingCooldown;
                }

                int? dbBandId = null;
                if (ownership.BandId.HasValue)
                {
                    string bandProtoIdStr = ownership.BandId.Value.ToString();
                    if (_prototypeManager.TryIndex<STBandPrototype>(bandProtoIdStr, out var dbBandProto))
                        dbBandId = dbBandProto.DatabaseId;
                }

                int? dbFactionId = null;
                if (ownership.FactionId.HasValue)
                {
                    string factionProtoIdStr = ownership.FactionId.Value.ToString();
                    if (_prototypeManager.TryIndex<NpcFactionPrototype>(factionProtoIdStr, out var dbFactionProto))
                        dbFactionId = dbFactionProto.DatabaseId;
                }

                state.DefendingBandId = dbBandId;
                state.DefendingFactionId = dbFactionId;
            }

            state.InitialLoadComplete = true;
        }
        catch (Exception ex)
        {
            state.InitialLoadComplete = true;
            Logger.ErrorS("warzone", $"Exception during async zone state load for {zoneUid} ({zoneProtoId}): {ex}");
        }
    }
}
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Doors.Components;
using Robust.Shared.GameObjects;

namespace Content.Server._Zona14.Administration.DoorLogs;

/// <summary>
///     Logs door and window destruction to the admin log system.
/// </summary>
public sealed class DoorLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private readonly Dictionary<EntityUid, EntityUid> _lastAttacker = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoorComponent, DamageChangedEvent>(OnDoorDamaged);
        SubscribeLocalEvent<DoorComponent, DamageThresholdReached>(OnDoorThresholdReached);
    }

    private void OnDoorDamaged(EntityUid uid, DoorComponent comp, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.Origin == null)
            return;

        _lastAttacker[uid] = args.Origin.Value;
    }

    private void OnDoorThresholdReached(EntityUid uid, DoorComponent comp, DamageThresholdReached args)
    {
        if (!args.Threshold.Behaviors.OfType<DoActsBehavior>().Any(b =>
                b.HasAct(ThresholdActs.Destruction) || b.HasAct(ThresholdActs.Breakage)))
        {
            return;
        }

        var doorProto = MetaData(uid).EntityPrototype?.ID ?? "Unknown";
        var doorName = ToPrettyString(uid);

        var destroyer = _lastAttacker.TryGetValue(uid, out var attacker)
            ? ToPrettyString(attacker)
            : new EntityStringRepresentation(EntityUid.Invalid, NetEntity.Invalid, true, "Unknown");

        _adminLog.Add(LogType.Z14Door, LogImpact.Medium,
            $"{destroyer:actor} destroyed {doorName:door} ({doorProto})");
    }
}

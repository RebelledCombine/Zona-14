// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Zona14.Administration;

/// <summary>
/// Tests that the door destruction log system records the correct actor.
/// </summary>
[TestFixture]
public sealed class DoorLogSystemTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false };

    [Test]
    public async Task DoorDestructionCreatesZ14DoorLog()
    {
        var adminLog = Server.ResolveDependency<IAdminLogManager>();
        var damageable = SEntMan.System<DamageableSystem>();
        var door = await Spawn("Airlock");
        var doorUid = SEntMan.GetEntity(door);

        var structural = ProtoMan.Index<DamageTypePrototype>(new ProtoId<DamageTypePrototype>("Structural"));
        var damage = new DamageSpecifier(structural, FixedPoint2.New(500));

        await Server.WaitPost(() =>
        {
            damageable.TryChangeDamage(doorUid, damage, origin: SPlayer);
        });

        await RunTicks(5);

        var logs = await adminLog.CurrentRoundLogs(new LogFilter
        {
            Types = new HashSet<LogType> { LogType.Z14Door },
            Limit = 10,
        });

        Assert.That(logs, Is.Not.Empty, "Expected a Z14Door log to be created");

        var log = logs.FirstOrDefault(l => l.Message.Contains("destroyed") && l.Players.Contains(ServerSession.UserId.UserId));
        Assert.That(log.Message, Does.Contain("destroyed"), "Expected the Z14Door log to include the destroyed message");
        Assert.That(log.Players, Has.Member(ServerSession.UserId.UserId), "Expected the Z14Door log to include the player as the actor");
    }
}

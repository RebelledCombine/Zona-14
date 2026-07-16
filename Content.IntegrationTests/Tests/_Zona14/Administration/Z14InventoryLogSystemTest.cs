// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Zona14.Administration;

/// <summary>
/// Tests that inventory equip/unequip events are logged as Z14Inventory logs.
/// </summary>
[TestFixture]
public sealed class Z14InventoryLogSystemTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false };

    [Test]
    public async Task EquipAndUnequipCreateZ14InventoryLogs()
    {
        var adminLog = Server.ResolveDependency<IAdminLogManager>();
        var inventory = SEntMan.System<InventorySystem>();

        var coat = await Spawn("ClothingOuterWinterCoat");
        var coatUid = SEntMan.GetEntity(coat);

        await Server.WaitPost(() =>
        {
            inventory.TryEquip(SPlayer, coatUid, "outerClothing", force: true);
        });

        await RunTicks(5);

        await Server.WaitPost(() =>
        {
            inventory.TryUnequip(SPlayer, "outerClothing", force: true);
        });

        await RunTicks(5);

        var logs = await adminLog.CurrentRoundLogs(new LogFilter
        {
            Types = new HashSet<LogType> { LogType.Z14Inventory },
            Limit = 10,
        });

        Assert.That(logs, Has.Count.GreaterThanOrEqualTo(2), "Expected equip and unequip Z14Inventory logs");

        var equip = logs.FirstOrDefault(l => l.Message.Contains("equipped") && l.Players.Contains(ServerSession.UserId.UserId));
        var unequip = logs.FirstOrDefault(l => l.Message.Contains("unequipped") && l.Players.Contains(ServerSession.UserId.UserId));

        Assert.That(equip.Message, Does.Contain("equipped"), "Expected an equip Z14Inventory log for the player");
        Assert.That(unequip.Message, Does.Contain("unequipped"), "Expected an unequip Z14Inventory log for the player");
    }
}

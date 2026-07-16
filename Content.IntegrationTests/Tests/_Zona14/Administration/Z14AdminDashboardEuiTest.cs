// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Server._Zona14.Administration.UI.Dashboard;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Zona14.Administration;

/// <summary>
/// Tests for the Z14 admin dashboard EUI state and event handling.
/// </summary>
[TestFixture]
public sealed class Z14AdminDashboardEuiTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false, Destructive = true };

    [Test]
    public async Task DashboardStateAndEvents()
    {
        var adminManager = Server.ResolveDependency<IAdminManager>();
        var adminLog = Server.ResolveDependency<IAdminLogManager>();
        var euiManager = Server.ResolveDependency<EuiManager>();

        // Promote the test player to host so they have admin access.
        await Server.ExecuteCommand($"promotehost {ServerSession.Name}");
        await RunTicks(5);

        Assert.That(adminManager.IsAdmin(ServerSession), Is.True, "Test player should be admin after promotion");

        var eui = new Z14AdminDashboardEui();
        await Server.WaitPost(() => euiManager.OpenEui(eui, ServerSession));
        await RunTicks(5);

        Z14AdminDashboardState state = default!;
        await Server.WaitPost(() => state = (Z14AdminDashboardState) eui.GetNewState());

        // Verify admin flags are reported.
        Assert.That(((AdminFlags) state.Flags).HasFlag(AdminFlags.Admin), Is.True, "Dashboard should report Admin flag");

        // Verify allowed commands include mentor chat and admin info.
        var commandNames = state.AllowedCommands.Select(c => c.Name).ToHashSet();
        Assert.That(commandNames, Does.Contain("msay"), "Dashboard should allow msay");
        Assert.That(commandNames, Does.Contain("z14admininfo"), "Dashboard should allow z14admininfo");

        // Add interesting logs and ensure they appear in RecentEvents.
        await Server.WaitPost(() =>
        {
            var player = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
            adminLog.Add(LogType.MentorHelp, LogImpact.Low, $"{player:player} sent a mentor help message");
            adminLog.Add(LogType.Z14Inventory, LogImpact.Low, $"{player:player} equipped a winter coat");
        });

        await RunTicks(5);

        await Server.WaitPost(() => state = (Z14AdminDashboardState) eui.GetNewState());

        var types = state.RecentEvents.Select(e => e.Type).ToList();
        Assert.That(types, Does.Contain(LogType.MentorHelp), "Dashboard should include MentorHelp events");
        Assert.That(types, Does.Contain(LogType.Z14Inventory), "Dashboard should include Z14Inventory events");

        Assert.That(state.EventCounts.ContainsKey("MentorHelp"), Is.True, "Dashboard should count MentorHelp events");
        Assert.That(state.EventCounts.ContainsKey("Z14Inventory"), Is.True, "Dashboard should count Z14Inventory events");

        await Server.WaitPost(() => euiManager.CloseEui(eui));
    }
}

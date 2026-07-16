// SPDX-License-Identifier: MIT

#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server._Zona14.Administration.Logs;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Zona14.Administration;

/// <summary>
/// Tests for the Zona-14 anti-cheat / cheat detection system.
/// </summary>
[TestFixture]
public sealed class Z14CheatDetectionSystemTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true };

    private IAdminLogManager AdminLog => Server.ResolveDependency<IAdminLogManager>();

    [Test]
    public async Task Movement()
    {
        var adminManager = Server.ResolveDependency<IAdminManager>();

        // The test player is the host; de-admin so the anti-cheat movement check is not skipped.
        await Server.WaitPost(() =>
        {
            if (adminManager.IsAdmin(ServerSession))
                adminManager.DeAdmin(ServerSession);
        });

        var localPos = SEntMan.GetComponent<TransformComponent>(SPlayer).LocalPosition;

        // Initial small move to seed the movement sample.
        var start = localPos + new Vector2(0.1f, 0f);
        await Server.WaitPost(() => Transform.SetLocalPosition(SPlayer, start));

        // Wait longer than the minimum movement sample time.
        await RunSeconds(0.15f);

        var end = start + new Vector2(4.9f, 0f);

        // Attach the alert handler before the suspicious move so it is caught synchronously.
        var tcs = AttachAlertHandler("impossible movement", ServerSession.UserId);

        await Server.WaitPost(() => Transform.SetLocalPosition(SPlayer, end));

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(5));
        Assert.That(alert, Is.Not.Null, "Expected an impossible movement admin alert");
    }

    [Test]
    public async Task Kill()
    {
        var tcs = AttachAlertHandler("rapid killing", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
                var victim = "Victim";
                AdminLog.Add(LogType.Kill, LogImpact.High, $"{actor:actor} killed {victim:victim}");
            }
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(5));
        Assert.That(alert, Is.Not.Null, "Expected a rapid-kill admin alert");
    }

    [Test]
    public async Task Door()
    {
        var tcs = AttachAlertHandler("mass door destruction", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
                AdminLog.Add(LogType.Z14Door, LogImpact.Medium, $"{actor:actor} destroyed an airlock");
            }
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(5));
        Assert.That(alert, Is.Not.Null, "Expected a mass door destruction admin alert");
    }

    [Test]
    public async Task Spawn()
    {
        var tcs = AttachAlertHandler("mass item spawning", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
                AdminLog.Add(LogType.EntitySpawn, LogImpact.Low, $"{actor:player} spawned something");
            }
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(5));
        Assert.That(alert, Is.Not.Null, "Expected a mass item spawning admin alert");
    }

    [Test]
    public async Task NoAlertForSingleKill()
    {
        var tcs = AttachAlertHandler("rapid killing", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
            var victim = "Victim";
            AdminLog.Add(LogType.Kill, LogImpact.High, $"{actor:actor} killed {victim:victim}");
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(1));
        Assert.That(alert, Is.Null, "Should not alert for a single kill");
    }

    [Test]
    public async Task NoAlertForLowDoor()
    {
        var tcs = AttachAlertHandler("mass door destruction", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
            AdminLog.Add(LogType.Z14Door, LogImpact.Low, $"{actor:actor} damaged an airlock");
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(1));
        Assert.That(alert, Is.Null, "Should not alert for a low-impact door log");
    }

    [Test]
    public async Task NoAlertForNonKill()
    {
        var tcs = AttachAlertHandler("rapid killing", ServerSession.UserId);

        await Server.WaitPost(() =>
        {
            var actor = new AdminLogPlayerValue(ServerSession.UserId, ServerSession.Name);
            AdminLog.Add(LogType.Action, LogImpact.High, $"{actor:actor} did something");
        });

        await RunTicks(5);

        var alert = await AwaitAlert(tcs, TimeSpan.FromSeconds(1));
        Assert.That(alert, Is.Null, "Should not alert for non-kill log types");
    }

    private TaskCompletionSource<AdminLogAddedEventArgs?> AttachAlertHandler(string messageFragment, NetUserId userId)
    {
        var tcs = new TaskCompletionSource<AdminLogAddedEventArgs?>();
        EventHandler<AdminLogAddedEventArgs> handler = (s, e) =>
        {
            if (e.Type != LogType.AdminAlert)
                return;

            if (!e.Players.Any(p => p.PlayerUserId == userId.UserId))
                return;

            if (!e.Log.Message.Contains(messageFragment, StringComparison.Ordinal))
                return;

            tcs.TrySetResult(e);
        };

        AdminLog.OnAdminLogAdded += handler;
        tcs.Task.ContinueWith(_ => AdminLog.OnAdminLogAdded -= handler, TaskScheduler.Default);

        return tcs;
    }

    private async Task<AdminLogAddedEventArgs?> WaitForAdminAlertAsync(string messageFragment, NetUserId userId)
    {
        var tcs = AttachAlertHandler(messageFragment, userId);
        return await AwaitAlert(tcs, TimeSpan.FromSeconds(5));
    }

    private async Task<AdminLogAddedEventArgs?> AwaitAlert(TaskCompletionSource<AdminLogAddedEventArgs?> tcs, TimeSpan timeout)
    {
        var delay = Task.Delay(timeout);
        var completed = await Task.WhenAny(tcs.Task, delay);
        if (completed == tcs.Task)
            return await tcs.Task;

        tcs.TrySetResult(null);
        return null;
    }
}

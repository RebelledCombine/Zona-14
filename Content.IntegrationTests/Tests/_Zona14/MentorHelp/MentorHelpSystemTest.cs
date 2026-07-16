// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Client._Zona14.Administration.Systems;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared._Zona14.Administration.MentorHelp;
using Content.Shared._Zona14.CCVar;
using Content.Shared.Database;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.IntegrationTests.Tests._Zona14.MentorHelp;

/// <summary>
/// Tests for the mentor help messaging system.
/// </summary>
[TestFixture]
public sealed class MentorHelpSystemTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false, Destructive = true };

    private MentorHelpSystem ClientMentor => CEntMan.System<MentorHelpSystem>();
    private IAdminLogManager AdminLog => Server.ResolveDependency<IAdminLogManager>();

    [Test]
    public async Task NoMentorsNotification()
    {
        var adminManager = Server.ResolveDependency<IAdminManager>();

        // The test player is the host and would be treated as staff; de-admin to test the no-mentors fallback.
        await Server.WaitPost(() =>
        {
            if (adminManager.IsAdmin(ServerSession))
                adminManager.DeAdmin(ServerSession);
        });
        await RunTicks(10);

        var received = new List<MentorHelpTextMessage>();
        ClientMentor.OnMentorHelpTextMessageReceived += (s, e) => received.Add(e);

        ClientMentor.Send(Client.Session!.UserId, "Hello");
        await RunTicks(10);

        Assert.That(received, Is.Not.Empty, "Expected at least the sent message to be echoed");

        var noMentors = received.FirstOrDefault(m => m.TrueSender == SharedMentorHelpSystem.SystemUserId);
        Assert.That(noMentors, Is.Not.Null, "Expected no-mentors notification");
        Assert.That(noMentors!.Text, Does.Contain("No mentors"), "No-mentors notification text mismatch");
    }

    [Test]
    public async Task StaffReply()
    {
        await Server.ExecuteCommand($"promotehost {ServerSession.Name}");
        await RunTicks(10);

        var received = new List<MentorHelpTextMessage>();
        ClientMentor.OnMentorHelpTextMessageReceived += (s, e) => received.Add(e);

        ClientMentor.Send(Client.Session!.UserId, "Hello");
        await RunTicks(10);

        var reply = received.FirstOrDefault(m => m.TrueSender == Client.Session!.UserId);
        Assert.That(reply, Is.Not.Null, "Expected a staff reply message");
        Assert.That(reply!.Text, Does.Contain("Hello"), "Reply should contain the original text");
        Assert.That(reply.Text, Does.Contain("[color=red]"), "Staff reply should be colored red");
    }

    [Test]
    public async Task AdminLogsMentorHelp()
    {
        ClientMentor.Send(Client.Session!.UserId, "Hello");
        await RunTicks(10);

        var logs = await AdminLog.CurrentRoundLogs(new LogFilter
        {
            Types = new HashSet<LogType> { LogType.MentorHelp },
            Limit = 10,
        });

        Assert.That(logs, Is.Not.Empty, "Expected a MentorHelp admin log");
    }

    [Test]
    public async Task RateLimit()
    {
        await Server.WaitPost(() =>
        {
            Server.CfgMan.SetCVar(Zona14CVars.MentorHelpRateLimitCount, 1);
            Server.CfgMan.SetCVar(Zona14CVars.MentorHelpRateLimitPeriod, 60f);
        });

        var received = new List<MentorHelpTextMessage>();
        ClientMentor.OnMentorHelpTextMessageReceived += (s, e) => received.Add(e);

        ClientMentor.Send(Client.Session!.UserId, "First");
        ClientMentor.Send(Client.Session!.UserId, "Second");
        await RunTicks(10);

        var rateLimited = received.FirstOrDefault(m => m.TrueSender == SharedMentorHelpSystem.SystemUserId);
        Assert.That(rateLimited, Is.Not.Null, "Expected a rate-limit message");
        Assert.That(rateLimited!.Text, Does.Contain("too fast"), "Rate-limit message text mismatch");
    }

    [Test]
    public async Task SendInputTextUpdatedDoesNotThrow()
    {
        ClientMentor.SendInputTextUpdated(Client.Session!.UserId, true);
        await RunTicks(5);

        // The server should process the typing notification without throwing.
    }
}

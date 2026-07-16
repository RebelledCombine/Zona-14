// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.Console;
using NUnit.Framework;

namespace Content.IntegrationTests.Tests._Zona14.MentorHelp;

/// <summary>
/// Tests for the mentor chat (`msay`) command.
/// </summary>
[TestFixture]
public sealed class MentorChatCommandTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, AdminLogsEnabled = true, DummyTicker = false, Destructive = true };

    [Test]
    public async Task AuthorizedMentorChatLogsChat()
    {
        var adminLog = Server.ResolveDependency<IAdminLogManager>();

        await Server.ExecuteCommand($"promotehost {ServerSession.Name}");
        await RunTicks(10);

        await Client.ExecuteCommand("msay hello");
        await RunTicks(10);

        var logs = await adminLog.CurrentRoundLogs(new LogFilter
        {
            Types = new HashSet<LogType> { LogType.Chat },
            Limit = 10,
        });

        Assert.That(logs.Any(l => l.Message.Contains("Mentor chat from") && l.Message.Contains("hello")), Is.True, "Expected a Mentor chat log for an authorized msay");
    }

    [Test]
    public async Task UnauthorizedMentorChatDoesNotLogMentorChat()
    {
        var adminLog = Server.ResolveDependency<IAdminLogManager>();
        var adminManager = Server.ResolveDependency<IAdminManager>();
        var cmd = Server.ConsoleHost.AvailableCommands["msay"];

        // Directly invoke the msay command with a non-admin shell.
        // ServerConsoleHost would block the command for an unauthorized player, but this exercises the command/ChatManager guard.
        await Server.WaitPost(() =>
        {
            if (adminManager.IsAdmin(ServerSession))
                adminManager.DeAdmin(ServerSession);

            var shell = new ConsoleShell(Server.ConsoleHost, ServerSession, true);
            cmd.Execute(shell, "msay hello", new[] { "hello" });
        });
        await RunTicks(10);

        var logs = await adminLog.CurrentRoundLogs(new LogFilter
        {
            Types = new HashSet<LogType> { LogType.Chat },
            Limit = 10,
        });

        Assert.That(logs.Any(l => l.Message.Contains("Mentor chat from")), Is.False, "Unauthorized msay should not produce a Mentor chat log");
    }
}

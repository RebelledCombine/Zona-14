// SPDX-License-Identifier: MIT

#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Client._Zona14.Administration.UI.MentorHelp;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Managers;
using NUnit.Framework;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests._Zona14.MentorHelp;

/// <summary>
/// Tests that the mentor help command opens the correct window for staff and users.
/// </summary>
[TestFixture]
public sealed class MentorHelpWindowTest : InteractionTest
{
    protected override PoolSettings Settings => new() { Connected = true, Dirty = true, Destructive = true };

    [Test]
    public async Task StaffOpensMentorHelpWindow()
    {
        await Server.ExecuteCommand($"promotehost {ServerSession.Name}");
        await RunTicks(10);

        await Client.ExecuteCommand("openmentorhelp");
        await RunTicks(10);

        var uiManager = Client.ResolveDependency<IUserInterfaceManager>();
        var window = FindControl<MentorHelpWindow>(uiManager.AllRoots);

        Assert.That(window, Is.Not.Null, "Expected MentorHelpWindow for a staff member");
    }

    [Test]
    public async Task UserOpensUserMentorHelpWindow()
    {
        var adminManager = Server.ResolveDependency<IAdminManager>();

        // The test player is the host; de-admin so the UI opens the user window.
        await Server.WaitPost(() =>
        {
            if (adminManager.IsAdmin(ServerSession))
                adminManager.DeAdmin(ServerSession);
        });
        await RunTicks(10);

        await Client.ExecuteCommand("openmentorhelp");
        await RunTicks(10);

        var uiManager = Client.ResolveDependency<IUserInterfaceManager>();
        var window = FindControl<UserMentorHelpWindow>(uiManager.AllRoots);

        Assert.That(window, Is.Not.Null, "Expected UserMentorHelpWindow for a non-staff user");
    }

    private static T? FindControl<T>(IEnumerable<UIRoot> roots) where T : Control
    {
        foreach (var root in roots)
        {
            var found = FindControl<T>(root);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T? FindControl<T>(Control parent) where T : Control
    {
        if (parent is T t)
            return t;

        foreach (var child in parent.Children)
        {
            var found = FindControl<T>(child);
            if (found != null)
                return found;
        }

        return null;
    }
}

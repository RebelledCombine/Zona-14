// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14PlayerActionWindow : DefaultWindow
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;

    private readonly LineEdit _jobEdit;

    public event Action<Z14AdminDashboardAction, string?>? OnAction;

    public Z14PlayerActionWindow(Z14AdminDashboardPlayer player, AdminFlags flags, Action<Z14AdminDashboardAction, string?>? onAction)
    {
        IoCManager.InjectDependencies(this);

        Title = $"Actions: {player.Name}";
        SetSize = new Vector2(350, 320);
        MinSize = new Vector2(300, 250);

        OnAction = onAction;

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 5,
            Margin = new Thickness(5),
        };

        box.AddChild(new Label { Text = $"Name: {player.Name}" });
        if (player.CharacterName != null)
            box.AddChild(new Label { Text = $"Character: {player.CharacterName}" });
        box.AddChild(new Label { Text = $"UserId: {player.UserId}" });

        AddActionButton(box, "Player Panel", Z14AdminDashboardAction.OpenPlayerPanel, AdminFlags.Admin);
        AddActionButton(box, "Player Logs", Z14AdminDashboardAction.OpenPlayerLogs, AdminFlags.Logs);
        AddActionButton(box, "Ban Panel", Z14AdminDashboardAction.OpenBanPanel, AdminFlags.Ban);
        AddActionButton(box, "Wipe Stash", Z14AdminDashboardAction.WipeStash, AdminFlags.Ban);
        AddActionButton(box, "Whitelist Add", Z14AdminDashboardAction.WhitelistAdd, AdminFlags.Ban);
        AddActionButton(box, "Whitelist Remove", Z14AdminDashboardAction.WhitelistRemove, AdminFlags.Ban);

        var jobBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
        };
        _jobEdit = new LineEdit
        {
            PlaceHolder = "job prototype",
            HorizontalExpand = true,
        };
        jobBox.AddChild(_jobEdit);
        AddActionButton(jobBox, "Add Job", Z14AdminDashboardAction.JobWhitelistAdd, AdminFlags.Ban);
        AddActionButton(jobBox, "Remove Job", Z14AdminDashboardAction.JobWhitelistRemove, AdminFlags.Ban);
        box.AddChild(jobBox);

        Contents.AddChild(box);
    }

    private void AddActionButton(BoxContainer container, string text, Z14AdminDashboardAction action, AdminFlags flag)
    {
        var button = new Button
        {
            Text = text,
            Disabled = !_adminManager.HasFlag(flag),
        };
        button.OnPressed += _ =>
        {
            var extra = action is Z14AdminDashboardAction.JobWhitelistAdd or Z14AdminDashboardAction.JobWhitelistRemove
                ? _jobEdit.Text
                : null;
            OnAction?.Invoke(action, extra);
        };
        container.AddChild(button);
    }
}

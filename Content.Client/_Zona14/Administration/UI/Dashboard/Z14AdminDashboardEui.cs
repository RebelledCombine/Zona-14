// SPDX-License-Identifier: MIT

using Content.Client.Eui;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Eui;
using Robust.Client.UserInterface;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14AdminDashboardEui : BaseEui
{
    private Z14AdminDashboardWindow? _window;

    public Z14AdminDashboardEui()
    {
        _window = new Z14AdminDashboardWindow();
        _window.OnClose += OnClose;
        _window.OnPlayerAction += action => SendMessage(action);
        _window.OnFeatureCommand += cmd => SendMessage(new FeatureCommand(cmd));
        _window.OnRefresh += () => SendMessage(new Refresh());
    }

    public override void Opened()
    {
        base.Opened();
        _window?.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Dispose();
        _window = null;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not Z14AdminDashboardState dashboardState)
            return;

        _window?.UpdateState(dashboardState);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is NewEvents newEvents)
        {
            _window?.AddEvents(newEvents.Events);
        }
    }

    private void OnClose()
    {
        SendMessage(new CloseEuiMessage());
    }
}

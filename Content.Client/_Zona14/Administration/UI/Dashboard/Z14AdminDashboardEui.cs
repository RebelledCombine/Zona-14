// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Content.Client.Eui;
using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Eui;
using Robust.Client.UserInterface;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Client._Zona14.Administration.UI.Dashboard;

public sealed class Z14AdminDashboardEui : BaseEui
{
    private Z14AdminDashboardWindow? _window;
    private readonly Dictionary<string, Z14DashboardOutputWindow> _outputWindows = new();

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

        foreach (var (_, outputWindow) in _outputWindows)
        {
            outputWindow.Close();
        }

        _outputWindows.Clear();
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

        switch (msg)
        {
            case NewEvents newEvents:
                _window?.AddEvents(newEvents.Events);
                break;

            case FeatureOutput output:
                ShowOutputWindow(output.Title, output.Command, output.Text);
                break;
        }
    }

    private void ShowOutputWindow(string title, string command, string text)
    {
        if (!_outputWindows.TryGetValue(command, out var window))
        {
            window = new Z14DashboardOutputWindow(title, command);
            window.OnRefresh += cmd => SendMessage(new FeatureCommand(cmd));
            window.OnClose += () =>
            {
                _outputWindows.Remove(command);
            };
            _outputWindows[command] = window;
        }

        window.SetText(text);

        if (!window.IsOpen)
            window.OpenCentered();
    }

    private void OnClose()
    {
        SendMessage(new CloseEuiMessage());
    }
}

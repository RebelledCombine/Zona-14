// SPDX-FileCopyrightText: 2026 taydeo <tay@funkystation.org>
//
// SPDX-License-Identifier: MIT
// Ported from funky-station Content.Client/_Funkystation/ContentWarning/ContentWarningUIController.cs@2e50750ab6

using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Shared._Zona14.CCVar;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Client._Zona14.ContentWarning;

[UsedImplicitly]
public sealed class ContentWarningUIController : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

    private ContentWarningPopup? _window;

    public void OnStateEntered(LobbyState _)
    {
        AttemptOpenContentWarningPopup();
    }

    public void OnStateEntered(GameplayState _)
    {
        AttemptOpenContentWarningPopup();
    }

    private void AttemptOpenContentWarningPopup()
    {
        if (!_cfg.GetCVar(Zona14CVars.ContentWarningDisplay) || _cfg.GetCVar(Zona14CVars.ContentWarningAcknowledged))
            return;

        OpenContentWarningPopup();
    }

    private void OpenContentWarningPopup()
    {
        if (_window != null)
            return;

        _window = new ContentWarningPopup();
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
        _window.OnContentWarningReject += () =>
        {
            _window?.Close();
            _window = null;

            if (_cfg.GetCVar(Zona14CVars.ContentWarningKickOnIgnore))
                _consoleHost.ExecuteCommand("quit");
        };
        _window.OnContentWarningAccept += () =>
        {
            _window?.Close();
            _window = null;
            _cfg.SetCVar(Zona14CVars.ContentWarningAcknowledged, true);
            _cfg.SaveToFile();
        };
    }
}

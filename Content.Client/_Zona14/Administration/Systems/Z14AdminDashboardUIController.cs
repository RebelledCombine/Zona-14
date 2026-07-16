// SPDX-License-Identifier: MIT

using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Shared.Administration;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Console;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;
using Robust.Shared.Input.Binding;

namespace Content.Client._Zona14.Administration.Systems;

[UsedImplicitly]
public sealed class Z14AdminDashboardUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnStateEntered<LobbyState>
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IClientConsoleHost _conHost = default!;
    [Dependency] private readonly IInputManager _input = default!;

    public override void Initialize()
    {
        base.Initialize();

        _input.SetInputCommand(ContentKeyFunctions.OpenZ14Dashboard,
            InputCmdHandler.FromDelegate(_ => Toggle()));
    }

    public void OnStateEntered(GameplayState state)
    {
    }

    public void OnStateEntered(LobbyState state)
    {
    }

    private void Toggle()
    {
        if (!_adminManager.HasFlag(AdminFlags.Admin))
            return;

        _conHost.ExecuteCommand("z14dashboard");
    }
}

// SPDX-License-Identifier: MIT

using Content.Client._Zona14.Administration.Systems;
using Content.Client._Zona14.Administration.UI.MentorHelp;
using Content.Client.Administration.Managers;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Zona14.Administration.MentorHelp;
using Content.Shared._Zona14.CCVar;
using Content.Shared.Administration;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Zona14.UserInterface.Systems.MentorHelp;

public interface IMentorHelpController
{
    void SendMessage(NetUserId channel, string text, bool playSound);
    void SendInputTextUpdated(NetUserId channel, bool typing);
    event Action<MentorHelpTextMessage>? MentorHelpReceived;
    event Action<MentorHelpPlayerTypingUpdated>? MentorHelpTypingUpdated;
}

[UsedImplicitly]
public sealed class MentorHelpUIController : UIController,
    IOnSystemChanged<MentorHelpSystem>,
    IOnStateChanged<GameplayState>,
    IOnStateChanged<LobbyState>,
    IMentorHelpController
{
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    private MentorHelpSystem? _mentorHelpSystem;
    private IMentorHelpUIHandler? _uiHelper;

    private MenuButton? GameMHelpButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.MHelpButton;
    private Button? LobbyMHelpButton => (UIManager.ActiveScreen as LobbyGui)?.MHelpButton;

    private bool _hasUnreadMHelp;
    private bool _mHelpSoundEnabled;
    private string? _mHelpSound;

    public event Action<MentorHelpTextMessage>? MentorHelpReceived;
    public event Action<MentorHelpPlayerTypingUpdated>? MentorHelpTypingUpdated;

    protected override string SawmillName => "c.s.go.es.mhelp";

    public override void Initialize()
    {
        base.Initialize();

        _adminManager.AdminStatusUpdated += OnAdminStatusUpdated;
        _config.OnValueChanged(Zona14CVars.MentorHelpSound, v => _mHelpSound = v, true);
        _config.OnValueChanged(Zona14CVars.MentorHelpSoundEnabled, v => _mHelpSoundEnabled = v, true);

        _input.SetInputCommand(ContentKeyFunctions.OpenMentorHelp,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
    }

    public void OnSystemLoaded(MentorHelpSystem system)
    {
        _mentorHelpSystem = system;
        _mentorHelpSystem.OnMentorHelpTextMessageReceived += ReceivedMentorHelp;
        _mentorHelpSystem.OnMentorHelpTypingUpdated += OnMentorHelpTypingReceived;
    }

    public void OnSystemUnloaded(MentorHelpSystem system)
    {
        DebugTools.Assert(_mentorHelpSystem != null);
        _mentorHelpSystem!.OnMentorHelpTextMessageReceived -= ReceivedMentorHelp;
        _mentorHelpSystem!.OnMentorHelpTypingUpdated -= OnMentorHelpTypingReceived;
        _mentorHelpSystem = null;
    }

    public void SendMessage(NetUserId channel, string text, bool playSound)
    {
        _mentorHelpSystem?.Send(channel, text, playSound);
    }

    public void SendInputTextUpdated(NetUserId channel, bool typing)
    {
        _mentorHelpSystem?.SendInputTextUpdated(channel, typing);
    }

    public void ToggleWindow()
    {
        EnsureUIHelper();
        _uiHelper!.ToggleWindow();
    }

    public void Open(NetUserId? channelId = null)
    {
        EnsureUIHelper();
        if (_uiHelper!.IsOpen)
            return;

        _uiHelper.Open(channelId);
    }

    private void ReceivedMentorHelp(object? sender, MentorHelpTextMessage message)
    {
        Log.Info($"@{message.UserId}: {message.Text}");

        var localPlayer = _playerManager.LocalSession;
        if (localPlayer == null)
            return;

        if (message.PlaySound && localPlayer.UserId != message.TrueSender)
        {
            if (_mHelpSound != null && (_mHelpSoundEnabled || !_adminManager.IsActive()))
                _audio.PlayGlobal(_mHelpSound, Filter.Local(), false);
            _clyde.RequestWindowAttention();
        }

        EnsureUIHelper();

        if (!_uiHelper!.IsOpen)
            UnreadMHelpReceived();
        else if (!_hasUnreadMHelp)
            UnreadMHelpRead();

        _uiHelper.Receive(message);
        MentorHelpReceived?.Invoke(message);
    }

    private void OnMentorHelpTypingReceived(object? sender, MentorHelpPlayerTypingUpdated message)
    {
        _uiHelper?.PeopleTypingUpdated(message);
        MentorHelpTypingUpdated?.Invoke(message);
    }

    public void UnloadButton()
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed -= MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    public void LoadButton()
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed += MHelpButtonPressed;

        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed += MHelpButtonPressed;
    }

    public void OnStateEntered(GameplayState state)
    {
        SetUpButton(GameMHelpButton);
    }

    public void OnStateExited(GameplayState state)
    {
        if (GameMHelpButton != null)
            GameMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    public void OnStateEntered(LobbyState state)
    {
        SetUpButton(LobbyMHelpButton);
    }

    public void OnStateExited(LobbyState state)
    {
        if (LobbyMHelpButton != null)
            LobbyMHelpButton.OnPressed -= MHelpButtonPressed;
    }

    private void SetUpButton(BaseButton? button)
    {
        if (button == null)
            return;

        button.OnPressed -= MHelpButtonPressed;
        button.OnPressed += MHelpButtonPressed;
        button.Pressed = _uiHelper?.IsOpen ?? false;

        if (_hasUnreadMHelp)
            UnreadMHelpReceived();
        else
            UnreadMHelpRead();
    }

    private void OnAdminStatusUpdated()
    {
        if (_uiHelper is not { IsOpen: true })
            return;
        EnsureUIHelper();
    }

    private void MHelpButtonPressed(ButtonEventArgs obj)
    {
        EnsureUIHelper();
        _uiHelper!.ToggleWindow();
    }

    private void EnsureUIHelper()
    {
        if (_uiHelper is { Disposed: false })
            return;

        var localUser = _playerManager.LocalSession?.UserId;
        if (localUser == null)
            return;

        var isAdmin = _adminManager.HasFlag(AdminFlags.Admin) || _adminManager.HasFlag(AdminFlags.Mentor);
        _uiHelper = isAdmin
            ? new MentorHelpWindow(localUser.Value, this)
            : new UserMentorHelpWindow(localUser.Value, this);

        _uiHelper.OnClose += () =>
        {
            SetMHelpPressed(false);
        };
        _uiHelper.OnOpen += () =>
        {
            SetMHelpPressed(true);
            UnreadMHelpRead();
        };
    }

    private void SetMHelpPressed(bool pressed)
    {
        UIManager.ClickSound();
        if (GameMHelpButton != null)
            GameMHelpButton.Pressed = pressed;
        if (LobbyMHelpButton != null)
            LobbyMHelpButton.Pressed = pressed;
    }

    private void UnreadMHelpReceived()
    {
        GameMHelpButton?.StyleClasses.Add(StyleClass.Negative);
        LobbyMHelpButton?.StyleClasses.Add(StyleClass.Negative);
        _hasUnreadMHelp = true;
    }

    private void UnreadMHelpRead()
    {
        GameMHelpButton?.StyleClasses.Remove(StyleClass.Negative);
        LobbyMHelpButton?.StyleClasses.Remove(StyleClass.Negative);
        _hasUnreadMHelp = false;
    }
}

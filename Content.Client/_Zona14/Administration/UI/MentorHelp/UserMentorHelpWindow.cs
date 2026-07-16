// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Client._Zona14.UserInterface.Systems.MentorHelp;
using Content.Shared._Zona14.Administration.MentorHelp;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client._Zona14.Administration.UI.MentorHelp;

public sealed class UserMentorHelpWindow : DefaultWindow, IMentorHelpUIHandler
{
    private readonly NetUserId _owner;
    private readonly IMentorHelpController _controller;
    private MentorHelpPanel? _chatPanel;

    public UserMentorHelpWindow(NetUserId owner, IMentorHelpController controller)
    {
        _owner = owner;
        _controller = controller;

        Title = Loc.GetString("mentorhelp-user-title");
        MinSize = new Vector2(500, 300);
        SetSize = new Vector2(600, 450);

        OnClose += () => _controller.SendInputTextUpdated(_owner, false);
    }

    public void Receive(MentorHelpTextMessage message)
    {
        EnsureInit();
        _chatPanel!.ReceiveLine(message);
    }

    public void PeopleTypingUpdated(MentorHelpPlayerTypingUpdated message)
    {
        if (message.Channel != _owner)
            return;

        _chatPanel?.UpdatePlayerTyping(message.PlayerName, message.Typing);
    }

    public void ToggleWindow()
    {
        EnsureInit();
        if (IsOpen)
            Close();
        else
            OpenCentered();
    }

    public void Open(NetUserId? channelId = null)
    {
        EnsureInit();
        if (!IsOpen)
            OpenCentered();
    }

    private void EnsureInit()
    {
        if (_chatPanel is { Disposed: false })
            return;

        _chatPanel = new MentorHelpPanel(text => _controller.SendMessage(_owner, text, true));
        _chatPanel.InputTextChanged += text => _controller.SendInputTextUpdated(_owner, !string.IsNullOrEmpty(text));
        Contents.AddChild(_chatPanel);

        var introText = Loc.GetString("mentorhelp-system-introductory-message");
        var introMessage = new MentorHelpTextMessage(_owner, SharedMentorHelpSystem.SystemUserId, introText);
        _chatPanel.ReceiveLine(introMessage);
    }

}

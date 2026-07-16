// SPDX-License-Identifier: MIT

using Content.Shared._Zona14.Administration.MentorHelp;
using Robust.Shared.Network;

namespace Content.Client._Zona14.Administration.UI.MentorHelp;

public interface IMentorHelpUIHandler
{
    bool IsOpen { get; }
    bool Disposed { get; }
    event Action? OnClose;
    event Action? OnOpen;

    void Receive(MentorHelpTextMessage message);
    void PeopleTypingUpdated(MentorHelpPlayerTypingUpdated message);
    void ToggleWindow();
    void Open(NetUserId? channelId = null);
}

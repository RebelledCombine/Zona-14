// SPDX-License-Identifier: MIT

using Content.Shared._Zona14.Administration.MentorHelp;
using Robust.Shared.Network;

namespace Content.Client._Zona14.Administration.Systems;

public sealed class MentorHelpSystem : SharedMentorHelpSystem
{
    public event EventHandler<MentorHelpTextMessage>? OnMentorHelpTextMessageReceived;
    public event EventHandler<MentorHelpPlayerTypingUpdated>? OnMentorHelpTypingUpdated;

    public void Send(NetUserId channelId, string text, bool playSound = true)
    {
        RaiseNetworkEvent(new MentorHelpTextMessage(channelId, channelId, text, playSound));
    }

    public void SendInputTextUpdated(NetUserId channelId, bool typing)
    {
        RaiseNetworkEvent(new MentorHelpClientTypingUpdated(channelId, typing));
    }

    protected override void OnMentorHelpTextMessage(MentorHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
        base.OnMentorHelpTextMessage(message, eventArgs);
        OnMentorHelpTextMessageReceived?.Invoke(this, message);
    }

    protected override void OnMentorHelpPlayerTypingUpdated(MentorHelpPlayerTypingUpdated message, EntitySessionEventArgs eventArgs)
    {
        base.OnMentorHelpPlayerTypingUpdated(message, eventArgs);
        OnMentorHelpTypingUpdated?.Invoke(this, message);
    }
}

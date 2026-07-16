// SPDX-License-Identifier: MIT

using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.MentorHelp;

public abstract class SharedMentorHelpSystem : EntitySystem
{
    public static readonly NetUserId SystemUserId = new(Guid.Empty);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MentorHelpTextMessage>(OnMentorHelpTextMessage);
        SubscribeNetworkEvent<MentorHelpPlayerTypingUpdated>(OnMentorHelpPlayerTypingUpdated);
    }

    protected virtual void OnMentorHelpTextMessage(MentorHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
    }

    protected virtual void OnMentorHelpPlayerTypingUpdated(MentorHelpPlayerTypingUpdated message, EntitySessionEventArgs eventArgs)
    {
    }
}

[Serializable, NetSerializable]
public sealed class MentorHelpTextMessage : EntityEventArgs
{
    public NetUserId UserId { get; }
    public NetUserId TrueSender { get; }
    public string Text { get; }
    public DateTime SentAt { get; }
    public bool PlaySound { get; }

    public MentorHelpTextMessage(NetUserId userId, NetUserId trueSender, string text, bool playSound = true, DateTime? sentAt = null)
    {
        UserId = userId;
        TrueSender = trueSender;
        Text = text;
        SentAt = sentAt ?? DateTime.Now;
        PlaySound = playSound;
    }
}

[Serializable, NetSerializable]
public sealed class MentorHelpClientTypingUpdated : EntityEventArgs
{
    public NetUserId Channel { get; }
    public bool Typing { get; }

    public MentorHelpClientTypingUpdated(NetUserId channel, bool typing)
    {
        Channel = channel;
        Typing = typing;
    }
}

[Serializable, NetSerializable]
public sealed class MentorHelpPlayerTypingUpdated : EntityEventArgs
{
    public NetUserId Channel { get; }
    public string PlayerName { get; }
    public bool Typing { get; }

    public MentorHelpPlayerTypingUpdated(NetUserId channel, string playerName, bool typing)
    {
        Channel = channel;
        PlayerName = playerName;
        Typing = typing;
    }
}

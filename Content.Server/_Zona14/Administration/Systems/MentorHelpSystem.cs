// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Players.RateLimiting;
using Content.Shared._Zona14.Administration.MentorHelp;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared._Zona14.CCVar;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._Zona14.Administration.Systems;

public sealed class MentorHelpSystem : SharedMentorHelpSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimit = default!;

    private const string RateLimitKey = "MentorHelp";

    public override void Initialize()
    {
        base.Initialize();

        _rateLimit.Register(RateLimitKey,
            new RateLimitRegistration(Zona14CVars.MentorHelpRateLimitPeriod,
                Zona14CVars.MentorHelpRateLimitCount,
                PlayerRateLimitedAction));

        SubscribeNetworkEvent<MentorHelpClientTypingUpdated>(OnClientTypingUpdated);
    }

    protected override void OnMentorHelpTextMessage(MentorHelpTextMessage message, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not ICommonSession senderSession)
            return;

        // Only the player or staff can send messages on this player's channel.
        var senderIsAdmin = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Admin) == true;
        var senderIsMentor = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Mentor) == true;
        var senderIsStaff = senderIsAdmin || senderIsMentor;

        if (senderSession.UserId != message.UserId && !senderIsStaff)
            return;

        if (_rateLimit.CountAction(senderSession, RateLimitKey) != RateLimitStatus.Allowed)
            return;

        if (message.Text.Length > _config.GetCVar(CCVars.ChatMaxMessageLength))
        {
            _chatManager.DispatchServerMessage(senderSession,
                Loc.GetString("chat-manager-max-message-length-exceeded-message",
                    ("limit", _config.GetCVar(CCVars.ChatMaxMessageLength))));
            return;
        }

        if (string.IsNullOrWhiteSpace(message.Text))
            return;

        var escapedText = FormattedMessage.EscapeText(message.Text);
        var senderName = GetMentorHelpPlayerName(senderSession.UserId);
        var color = senderIsAdmin ? "red" : senderIsMentor ? "purple" : "white";
        var bwoinkText = $"[color={color}]{senderName}[/color]: {escapedText}";

        var msg = new MentorHelpTextMessage(
            message.UserId,
            senderSession.UserId,
            bwoinkText,
            playSound: message.PlaySound);

        LogMentorHelp(msg);

        var staff = GetTargetStaff();
        foreach (var staffSession in staff)
        {
            RaiseNetworkEvent(msg, staffSession);
        }

        if (_playerManager.TryGetSessionById(message.UserId, out var userSession) && !staff.Contains(userSession))
        {
            RaiseNetworkEvent(msg, userSession.Channel);
        }

        // If no mentors are online, notify the player. Admins may still reply if they are available.
        if (!senderIsStaff && !GetTargetMentors().Any() && _playerManager.TryGetSessionById(message.UserId, out var user))
        {
            var noMentorsMsg = new MentorHelpTextMessage(
                message.UserId,
                SystemUserId,
                Loc.GetString("mentorhelp-system-no-mentors-online"),
                playSound: false);

            RaiseNetworkEvent(noMentorsMsg, user.Channel);
        }
    }

    private void OnClientTypingUpdated(MentorHelpClientTypingUpdated msg, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not ICommonSession senderSession)
            return;

        var senderIsAdmin = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Admin) == true;
        var senderIsMentor = _adminManager.GetAdminData(senderSession)?.HasFlag(AdminFlags.Mentor) == true;
        var senderIsStaff = senderIsAdmin || senderIsMentor;

        if (senderSession.UserId != msg.Channel && !senderIsStaff)
            return;

        var channel = senderIsStaff ? msg.Channel : senderSession.UserId;
        var update = new MentorHelpPlayerTypingUpdated(channel, senderSession.Name, msg.Typing);

        var staff = GetTargetStaff();
        foreach (var staffSession in staff)
        {
            if (staffSession.UserId == senderSession.UserId)
                continue;

            RaiseNetworkEvent(update, staffSession);
        }

        if (_playerManager.TryGetSessionById(channel, out var userSession) &&
            !staff.Contains(userSession) &&
            userSession.Channel != senderSession.Channel)
        {
            RaiseNetworkEvent(update, userSession.Channel);
        }
    }

    private void PlayerRateLimitedAction(ICommonSession obj)
    {
        RaiseNetworkEvent(
            new MentorHelpTextMessage(
                obj.UserId,
                SystemUserId,
                Loc.GetString("mentorhelp-system-rate-limited"),
                playSound: false),
            obj.Channel);
    }

    private string GetMentorHelpPlayerName(NetUserId userId)
    {
        if (_playerManager.TryGetSessionById(userId, out var session))
            return session.Name;

        return userId.ToString();
    }

    private void LogMentorHelp(MentorHelpTextMessage message)
    {
        var senderName = GetMentorHelpPlayerName(message.TrueSender);
        var recipientName = GetMentorHelpPlayerName(message.UserId);

        _adminLog.Add(
            LogType.MentorHelp,
            LogImpact.Low,
            $"{new AdminLogPlayerValue(message.TrueSender, senderName):player} -> {new AdminLogPlayerValue(message.UserId, recipientName):subject}: {message.Text}");
    }

    private List<ICommonSession> GetTargetStaff()
    {
        return _adminManager.ActiveAdmins
            .Where(p =>
            {
                var data = _adminManager.GetAdminData(p);
                return data?.HasFlag(AdminFlags.Admin) == true || data?.HasFlag(AdminFlags.Mentor) == true;
            })
            .ToList();
    }

    private List<ICommonSession> GetTargetMentors()
    {
        return _adminManager.ActiveAdmins
            .Where(p => _adminManager.GetAdminData(p)?.HasFlag(AdminFlags.Mentor) == true)
            .ToList();
    }
}

// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Eui;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.Dashboard;

[Serializable, NetSerializable]
public sealed class Z14AdminDashboardState : EuiStateBase
{
    public int RoundId { get; set; }
    public TimeSpan RoundDuration { get; set; }
    public string RunLevel { get; set; } = string.Empty;
    public int PlayerCount { get; set; }
    public int AdminCount { get; set; }
    public List<Z14AdminDashboardPlayer> Players { get; set; } = new();
    public List<Z14AdminDashboardMap> Maps { get; set; } = new();
    public Dictionary<string, int> EventCounts { get; set; } = new();
    public List<SharedAdminLog> RecentEvents { get; set; } = new();
    public uint Flags { get; set; }
    public bool ServerLoading { get; set; } = true;
    public List<Z14AdminDashboardCommandInfo> AllowedCommands { get; set; } = new();
}

[Serializable, NetSerializable]
public readonly record struct Z14AdminDashboardPlayer(NetUserId UserId, string Name, string? CharacterName, string? Job, bool IsAdmin);

[Serializable, NetSerializable]
public readonly record struct Z14AdminDashboardMap(int MapId, string Name, int GridCount, int EntityCount);

[Serializable, NetSerializable]
public readonly record struct Z14AdminDashboardCommandInfo(string Name, string Description, string Help, uint[] Flags);

public static class Z14AdminDashboardEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Refresh : EuiMessageBase
    {
    }

    [Serializable, NetSerializable]
    public sealed class NewEvents : EuiMessageBase
    {
        public NewEvents(List<SharedAdminLog> events, bool replace)
        {
            Events = events;
            Replace = replace;
        }

        public List<SharedAdminLog> Events { get; set; }
        public bool Replace { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class PlayerAction : EuiMessageBase
    {
        public PlayerAction(Z14AdminDashboardAction action, NetUserId userId, string targetName, string? extra = null)
        {
            Action = action;
            UserId = userId;
            TargetName = targetName;
            Extra = extra;
        }

        public Z14AdminDashboardAction Action { get; set; }
        public NetUserId UserId { get; set; }
        public string TargetName { get; set; }
        public string? Extra { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class FeatureCommand : EuiMessageBase
    {
        public FeatureCommand(string command)
        {
            Command = command;
        }

        public string Command { get; set; } = string.Empty;
    }

    [Serializable, NetSerializable]
    public sealed class FeatureOutput : EuiMessageBase
    {
        public FeatureOutput(string title, string command, string text)
        {
            Title = title;
            Command = command;
            Text = text;
        }

        public string Title { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    [Serializable, NetSerializable]
    public enum Z14AdminDashboardAction
    {
        OpenPlayerPanel,
        OpenPlayerLogs,
        OpenBanPanel,
        WipeStash,
        WhitelistAdd,
        WhitelistRemove,
        JobWhitelistAdd,
        JobWhitelistRemove,
    }
}

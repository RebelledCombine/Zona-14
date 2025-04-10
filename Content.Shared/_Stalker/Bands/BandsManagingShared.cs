using System;
using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Bands
{
    [Serializable, NetSerializable]
    public sealed class BandsManagingBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string? BandName { get; }
        public int MaxMembers { get; }
        public List<BandMemberInfo> Members { get; }
        public bool CanManage { get; }

        public BandsManagingBoundUserInterfaceState(string? bandName, int maxMembers, List<BandMemberInfo> members, bool canManage)
        {
            BandName = bandName;
            MaxMembers = maxMembers;
            Members = members;
            CanManage = canManage;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BandMemberInfo
    {
        public NetUserId UserId { get; }
        public string PlayerName { get; }
        public string RoleId { get; }

        public BandMemberInfo(NetUserId userId, string playerName, string roleId)
        {
            UserId = userId;
            PlayerName = playerName;
            RoleId = roleId;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BandsManagingAddMemberMessage : BoundUserInterfaceMessage
    {
        public string PlayerName { get; }

        public BandsManagingAddMemberMessage(string playerName)
        {
            PlayerName = playerName;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BandsManagingRemoveMemberMessage : BoundUserInterfaceMessage
    {
        public Guid PlayerUserId { get; }

        public BandsManagingRemoveMemberMessage(Guid playerUserId)
        {
            PlayerUserId = playerUserId;
        }
    }
}
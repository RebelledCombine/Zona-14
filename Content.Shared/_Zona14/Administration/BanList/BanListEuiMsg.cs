// SPDX-License-Identifier: MIT

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Administration.BanList;

[Serializable, NetSerializable]
public sealed class PardonBanMessage : EuiMessageBase
{
    public int BanId { get; }
    public bool IsRoleBan { get; }

    public PardonBanMessage(int banId, bool isRoleBan)
    {
        BanId = banId;
        IsRoleBan = isRoleBan;
    }
}

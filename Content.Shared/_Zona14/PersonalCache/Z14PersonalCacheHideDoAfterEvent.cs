// SPDX-License-Identifier: MIT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.PersonalCache;

/// <summary>
/// Raised when a cache hide/unhide do-after completes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class Z14PersonalCacheHideDoAfterEvent : SimpleDoAfterEvent
{
}

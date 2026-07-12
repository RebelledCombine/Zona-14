// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.PersonalCache;

/// <summary>
/// State sent to the PDA cartridge UI listing the owner's caches.
/// </summary>
[Serializable, NetSerializable]
public sealed class Z14PersonalCacheUiState : BoundUserInterfaceState
{
    public List<Z14PersonalCacheUiEntry> Caches = new();
}

/// <summary>
/// Single cache entry for the PDA list UI.
/// </summary>
[Serializable, NetSerializable]
public sealed class Z14PersonalCacheUiEntry
{
    public string MapKey = string.Empty;
    public float X;
    public float Y;
    public bool Hidden;
    public float Weight;
}

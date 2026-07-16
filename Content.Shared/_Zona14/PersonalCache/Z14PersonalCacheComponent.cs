// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.PersonalCache;

/// <summary>
/// Persistent per-account hidden world cache.
/// </summary>
[RegisterComponent]
public sealed partial class Z14PersonalCacheComponent : Component
{
    [DataField]
    public string CacheId = string.Empty;

    [DataField]
    public string OwnerUserId = string.Empty;

    [DataField]
    public string MapKey = string.Empty;

    [DataField]
    public float X;

    [DataField]
    public float Y;

    [DataField]
    public float Z;

    [DataField]
    public bool Hidden;
}

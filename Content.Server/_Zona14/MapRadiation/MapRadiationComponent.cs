// SPDX-License-Identifier: MIT

using Content.Shared.Damage;

namespace Content.Server._Zona14.MapRadiation;

[RegisterComponent]
public sealed partial class MapRadiationComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Interval = 1f;

    [DataField]
    public DamageSpecifier Damage = new();

    // runtime
    public TimeSpan NextDamageTime;
}

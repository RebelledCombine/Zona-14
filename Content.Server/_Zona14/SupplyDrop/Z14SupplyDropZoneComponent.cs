// SPDX-License-Identifier: MIT

using Content.Shared._Zona14.SupplyDrop;
using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.SupplyDrop;

[RegisterComponent]
public sealed partial class Z14SupplyDropZoneComponent : Component
{
    [DataField]
    public float DangerZoneRadius = 7f;

    [DataField]
    public float CrateMinRadius = 1.5f;

    [DataField]
    public float CrateMaxRadius = 3f;

    [DataField]
    public float GuardianMinRadius = 3f;

    [DataField]
    public float GuardianMaxRadius = 7f;

    [DataField]
    public float WarningRadius = 14f;

    [DataField]
    public TimeSpan WarningDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Announcements/attention.ogg");

    [DataField]
    public SoundSpecifier ImpactSound = new SoundPathSpecifier("/Audio/Effects/explosion2.ogg");

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionPrototype = "Z14SupplyDropExplosion";

    [DataField]
    public List<Z14SupplyDropVariant> AllowedVariants = new() { Z14SupplyDropVariant.Any };

    [DataField]
    public bool DeleteAfterSpawn;
}

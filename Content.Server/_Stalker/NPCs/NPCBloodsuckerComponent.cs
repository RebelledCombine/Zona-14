using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;

namespace Content.Server._Stalker.NPCs;

[RegisterComponent, Access(typeof(NPCBloodsuckerSystem)), AutoGenerateComponentPause]
public sealed partial class NPCBloodsuckerComponent : Component
{
    /// <summary>
    /// HTN blackboard key for the target entity
    /// </summary>
    [DataField]
    public string TargetKey = "Target";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AttackRadius = 1.5f;


    [DataField("nextTimeUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan? NextTimeUpdate = null;

    [DataField]
    public float UpdateCooldown;

    [DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan? StartTime = TimeSpan.FromSeconds(0f);

    [DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan? EndTime = TimeSpan.FromSeconds(0f);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float ReloadTime = 12f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RandomiseReloadTime = 6f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float StunTime = 3f;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier DamageOnSuck = default!;


    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier HealOnSuck = default!;
}

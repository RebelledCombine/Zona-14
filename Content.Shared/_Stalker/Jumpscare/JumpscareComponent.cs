using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Jumpscare
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
    public sealed partial class JumpscareComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float StaminaDamage = 0f;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float AttackRadius = 6f;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float AttackDistance = 0.1f;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float JumpPower = 30f;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float JumpDistance = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Jumping = false;

        [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ChargeDamage = default!;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float DamageRadius = 0.5f;
        [AutoPausedField]
        public TimeSpan? StartTime = TimeSpan.FromSeconds(0f);
        [AutoPausedField]
        public TimeSpan? EndTime = TimeSpan.FromSeconds(0f);

        public float ReloadTime = 6f;
        public float RandomiseReloadTime = 2f;

        [AutoPausedField]
        public TimeSpan? JumpStartTime = TimeSpan.FromSeconds(0f);
        [AutoPausedField]
        public TimeSpan? JumpEndTime = TimeSpan.FromSeconds(0f);
        [AutoPausedField]
        public TimeSpan? PreparingStartTime = TimeSpan.FromSeconds(0f);
        [AutoPausedField]
        public TimeSpan? PreparingEndTime = TimeSpan.FromSeconds(0f);
        [DataField, AutoPausedField]
        public TimeSpan? PreparingReloadTime = TimeSpan.FromSeconds(0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnCoolDown = true;
        public Vector2 Targeting = new(0, 0);

        [AutoPausedField]
        public TimeSpan? NextTimeUpdate;
        // seconds
        [DataField]
        public float UpdateCooldown;
    }
}

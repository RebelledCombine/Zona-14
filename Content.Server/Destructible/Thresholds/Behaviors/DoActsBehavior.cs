using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Doors.Components;
using Content.Shared._Zona14.Administration.Logs; // Zona14
using Robust.Shared.Player;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class DoActsBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     What acts should be triggered upon activation.
        /// </summary>
        [DataField("acts")]
        public ThresholdActs Acts { get; set; }

        public bool HasAct(ThresholdActs act)
        {
            return (Acts & act) != 0;
        }

        public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
        {
            if (HasAct(ThresholdActs.Breakage))
            {
                system.BreakEntity(owner);
            }

            if (HasAct(ThresholdActs.Destruction))
            {
                // Zona14: log player-destroyed doors for anti-cheat/alerting.
                if (system.EntityManager.HasComponent<DoorComponent>(owner) &&
                    cause != null &&
                    system.EntityManager.TryGetComponent(cause.Value, out ActorComponent? actorComp))
                {
                    var actor = new AdminLogPlayerValue(actorComp.PlayerSession.UserId, actorComp.PlayerSession.Name);
                    system.AdminLogger.Add(LogType.Z14Door, LogImpact.Medium,
                        $"{actor} destroyed door {system.EntityManager.ToPrettyString(owner)}");
                }
                // End Zona14

                system.DestroyEntity(owner);
            }
        }
    }
}

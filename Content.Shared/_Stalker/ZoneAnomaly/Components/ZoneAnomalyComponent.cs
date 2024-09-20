using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.ZoneAnomaly.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ZoneAnomalyComponent : Component
{
    public bool Charged => State == ZoneAnomalyState.Idle;

    [DataField]
    public bool Detected = true;

    [DataField]
    public int DetectedLevel = 0;

    [DataField]
    public ZoneAnomalyState State = ZoneAnomalyState.Idle;

    [DataField]
    public TimeSpan PreparingDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan PreparingTime;

    [DataField]
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ActivationTime;

    [DataField]
    public TimeSpan ChargingDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan ChargingTime;

    [DataField]
    public HashSet<EntityUid> Triggers = new();

    [DataField]
    public HashSet<EntityUid> InAnomaly = new();

    [DataField]
    public EntityWhitelist CollisionWhitelist = new();

    [DataField]
    public EntityWhitelist CollisionBlacklist = new();
}

public sealed partial class ZoneAnomalyStartCollideArgs : EventArgs
{
    public readonly EntityUid Anomaly;
    public readonly EntityUid OtherEntity;

    public bool Activate;

    public ZoneAnomalyStartCollideArgs(EntityUid anomaly, EntityUid otherEntity)
    {
        Anomaly = anomaly;
        OtherEntity = otherEntity;
    }
}

public sealed partial class ZoneAnomalyEndCollideArgs : EventArgs
{
    public readonly EntityUid Anomaly;
    public readonly EntityUid OtherEntity;
    public readonly bool IgnoreWhitelist;

    public bool Activate;

    public ZoneAnomalyEndCollideArgs(EntityUid anomaly, EntityUid otherEntity, bool ignoreWhitelist)
    {
        Anomaly = anomaly;
        OtherEntity = otherEntity;
        IgnoreWhitelist = ignoreWhitelist;
    }
}

[ByRefEvent]
public readonly record struct ZoneAnomalyChangedState(EntityUid Anomaly, ZoneAnomalyState Current, ZoneAnomalyState Previous);

[ByRefEvent]
public readonly record struct ZoneAnomalyActivateEvent(EntityUid Anomaly, HashSet<EntityUid> Triggers);

[ByRefEvent]
public readonly record struct ZoneAnomalyEntityAddEvent(EntityUid Anomaly, EntityUid Entity);

[ByRefEvent]
public readonly record struct ZoneAnomalyEntityRemoveEvent(EntityUid Anomaly, EntityUid Entity);


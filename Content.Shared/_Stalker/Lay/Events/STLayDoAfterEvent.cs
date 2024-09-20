using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Stalker.Lay.Events;

[Serializable, NetSerializable]
public sealed partial class STLayDoAfterEvent : DoAfterEvent
{
    [DataField]
    public STLayState NextState;

    public STLayDoAfterEvent(STLayState nextState)
    {
        NextState = nextState;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}

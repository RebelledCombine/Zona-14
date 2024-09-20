using Content.Shared._Stalker.Lay;
using Robust.Shared.Serialization;

namespace Content.Server._Stalker.Lay.Events;

[Serializable]
public sealed class STLayStateChangedEvent(STLayState state) : EntityEventArgs
{
    public readonly STLayState State = state;
}

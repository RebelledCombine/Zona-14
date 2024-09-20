using Content.Shared._Stalker.Lay;

namespace Content.Server._Stalker.Lay;

[RegisterComponent]
public sealed partial class STLayComponent : Component
{
    [DataField]
    public STLayState State;

    [DataField]
    public Dictionary<STLayState, TimeSpan> ChangeStateDelay = new()
    {
        { STLayState.Stand, TimeSpan.FromSeconds(2.5f) },
        { STLayState.Laid, TimeSpan.Zero },
    };

    [DataField]
    public Dictionary<STLayState, STLayState> StateTransitions = new()
    {
        { STLayState.Stand, STLayState.Laid },
        { STLayState.Laid, STLayState.Stand },
    };
}


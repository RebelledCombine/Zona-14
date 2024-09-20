using Content.Shared.Whitelist;

namespace Content.Server._Stalker.ApproachTrigger;

[RegisterComponent]
public sealed partial class ApproachTriggerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Enabled = true;
}

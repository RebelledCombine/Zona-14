using Content.Shared._Stalker.Modifier;
using Content.Shared._Stalker.Weight;

namespace Content.Server._Stalker.Weight;

public sealed partial class STWeightSystem
{
    private void InitializeModifier()
    {
        SubscribeLocalEvent<STWeightComponent, UpdatedFloatModifierEvent<Modifier.STWeightMaximumModifierComponent>>(OnUpdatedMaximum);
    }

    private void OnUpdatedMaximum(Entity<STWeightComponent> weight, ref UpdatedFloatModifierEvent<Modifier.STWeightMaximumModifierComponent> args)
    {
        weight.Comp.MaximumModifier = args.Modifier;
    }
}

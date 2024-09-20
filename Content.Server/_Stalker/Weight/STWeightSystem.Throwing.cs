using Content.Shared._Stalker.Throwing;
using Content.Shared._Stalker.Weight;

namespace Content.Server._Stalker.Weight;

public sealed partial class STWeightSystem
{
    private void InitializeThrowing()
    {
        SubscribeLocalEvent<STWeightComponent, STBeforeThrowedEvent>(OnBeforeThrow);
    }

    private void OnBeforeThrow(Entity<STWeightComponent> entity, ref STBeforeThrowedEvent args)
    {
        var modifier = Math.Max(entity.Comp.WeightThrowMinStrengthModifier, entity.Comp.Total * entity.Comp.WeightThrowModifier);
        args.Strength /= modifier;
    }
}

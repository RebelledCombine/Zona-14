using System;
using System.Collections.Generic;
using Content.Shared._Stalker.Weight;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Events;

namespace Content.Shared._Zona14.Weight;

/// <summary>
/// Zona14: makes the weight of a pulled (dragged) entity count toward the puller's carry weight.
/// Closes the exploit where a loaded backpack/crate could be dragged (or a chain of them thrown/dragged)
/// across the map with no weight penalty, because pulled entities are joint-attached to the map rather than
/// parented to the puller and so were invisible to <see cref="STWeightSystem"/>.
///
/// Items/containers contribute <see cref="STWeightComponent.PulledWeightFraction"/> of their total weight;
/// mobs/bodies contribute the lighter <see cref="STWeightComponent.PulledMobWeightFraction"/> so that dragging
/// a downed teammate to safety stays viable. The set of active pulls is tracked from the pull messages and
/// re-evaluated on a throttled tick, which also keeps the puller in sync when a dragged container's contents
/// change mid-drag (no reliance on <c>STWeightChangedEvent</c>, which another system already owns).
/// </summary>
public sealed class STPulledWeightSystem : EntitySystem
{
    [Dependency] private readonly STWeightSystem _weight = default!;

    private const float PollInterval = 0.5f;
    private float _accumulator;

    // puller -> the entity it is currently pulling.
    private readonly Dictionary<EntityUid, EntityUid> _activePulls = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STWeightComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<STWeightComponent, PullStoppedMessage>(OnPullStopped);
    }

    private void OnPullStarted(Entity<STWeightComponent> ent, ref PullStartedMessage args)
    {
        // PullStartedMessage is raised on both entities; only act on the puller.
        if (args.PullerUid != ent.Owner)
            return;

        _activePulls[ent.Owner] = args.PulledUid;
        RefreshPulledWeight(ent, args.PulledUid);
    }

    private void OnPullStopped(Entity<STWeightComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PullerUid != ent.Owner)
            return;

        _activePulls.Remove(ent.Owner);
        ClearPulledWeight(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < PollInterval)
            return;
        _accumulator = 0f;

        if (_activePulls.Count == 0)
            return;

        // Re-evaluate each live pull so mid-drag content changes (loot dumped into a dragged bag) stay reflected.
        foreach (var (puller, pulled) in new List<KeyValuePair<EntityUid, EntityUid>>(_activePulls))
        {
            if (!Exists(puller) || !TryComp<STWeightComponent>(puller, out var weight))
            {
                _activePulls.Remove(puller);
                continue;
            }

            if (!Exists(pulled))
            {
                _activePulls.Remove(puller);
                ClearPulledWeight((puller, weight));
                continue;
            }

            RefreshPulledWeight((puller, weight), pulled);
        }
    }

    private void RefreshPulledWeight(Entity<STWeightComponent> puller, EntityUid pulled)
    {
        if (!TryComp<STWeightComponent>(pulled, out var pulledWeight))
        {
            ClearPulledWeight(puller);
            return;
        }

        // Three tiers: humanoids (downed players) drag light for rescues; creatures/mutants drag at a
        // moderate weight-scaled cost; items and containers (loaded backpacks) drag at full weight.
        float fraction;
        if (HasComp<HumanoidAppearanceComponent>(pulled))
            fraction = puller.Comp.PulledHumanoidWeightFraction;
        else if (HasComp<MobStateComponent>(pulled))
            fraction = puller.Comp.PulledCreatureWeightFraction;
        else
            fraction = puller.Comp.PulledWeightFraction;

        SetPulledWeight(puller, GetDraggedWeight(pulled, pulledWeight) * fraction);
    }

    /// <summary>Flat extra drag weight for a worn storage container (backpack/belt). Loot items are each very
    /// light, so weight alone never makes a loaded bag heavy — this makes hauling one a genuine slog, not a
    /// free loot sled. Tune here.</summary>
    private const float ContainerDragBulk = 60f;

    /// <summary>
    /// The weight a pulled entity actually drags at. For a worn storage container
    /// (<see cref="STWeightInsideReductionComponent"/>): its inside-weight reduction models load spread across
    /// your back and must NOT make it cheaper to haul on the ground, so we undo it and drag the full contents;
    /// plus a flat <see cref="ContainerDragBulk"/> so a loaded bag is cumbersome regardless of how light its
    /// individual loot is.
    /// </summary>
    private float GetDraggedWeight(EntityUid pulled, STWeightComponent comp)
    {
        if (!TryComp<STWeightInsideReductionComponent>(pulled, out var reduction) || reduction.ReductionFraction <= 0f)
            return comp.Total;

        var keep = 1f - Math.Clamp(reduction.ReductionFraction, 0f, 1f);
        // InsideWeight is already reduced by `keep`; divide it back out to recover the unreduced contents.
        var rawInside = keep > 0f ? comp.InsideWeight / keep : comp.InsideWeight;

        return comp.Self + rawInside + ContainerDragBulk;
    }

    private void ClearPulledWeight(Entity<STWeightComponent> puller)
    {
        SetPulledWeight(puller, 0f);
    }

    private void SetPulledWeight(Entity<STWeightComponent> puller, float value)
    {
        if (Math.Abs(puller.Comp.PulledWeight - value) < 0.001f)
            return;

        puller.Comp.PulledWeight = value;
        Dirty(puller.Owner, puller.Comp);
        // Recomputes InsideWeight and refreshes the movement-speed penalty from the new Total.
        _weight.TryUpdateWeight(puller.Owner);
    }
}

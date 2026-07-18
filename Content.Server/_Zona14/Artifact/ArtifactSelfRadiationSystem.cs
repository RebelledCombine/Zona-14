using Content.Server.Radiation.Systems;
using Content.Shared.Inventory;

namespace Content.Server._Zona14.Artifact;

/// <summary>
///     Applies <see cref="ArtifactSelfRadiationComponent"/> radiation to the wearer only, once per
///     second. Every equipped artifact's rads are summed per wearer and applied as a single
///     radiation event, so armor's flat radiation reduction is spent once against the combined
///     total rather than once per artifact — this makes stacking rad-emitting artifacts scale
///     against you instead of being individually shrugged off.
/// </summary>
public sealed class ArtifactSelfRadiationSystem : EntitySystem
{
    [Dependency] private readonly RadiationSystem _radiation = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const float TickInterval = 1f;
    private float _accumulator;

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < TickInterval)
            return;

        var elapsed = _accumulator;
        _accumulator = 0f;

        var totals = new Dictionary<EntityUid, float>();

        var query = EntityQueryEnumerator<ArtifactSelfRadiationComponent, TransformComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform, out var meta))
        {
            if (comp.Rads <= 0f)
                continue;

            // Only counts while actually equipped in an ARTIFACT slot.
            if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slot))
                continue;
            if ((slot.SlotFlags & SlotFlags.ARTIFACT) == 0)
                continue;

            // An equipped item is parented to its wearer.
            var wearer = xform.ParentUid;
            if (!wearer.IsValid())
                continue;

            totals[wearer] = (totals.TryGetValue(wearer, out var acc) ? acc : 0f) + comp.Rads;
        }

        foreach (var (wearer, total) in totals)
        {
            if (total <= 0f)
                continue;

            _radiation.IrradiateEntity(wearer, new Dictionary<string, float> { ["Radiation"] = total }, elapsed);
        }
    }
}

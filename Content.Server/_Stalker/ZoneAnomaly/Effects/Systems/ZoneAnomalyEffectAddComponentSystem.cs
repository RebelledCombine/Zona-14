using Content.Shared._Stalker.ZoneAnomaly.Components;
using Content.Shared._Stalker.ZoneAnomaly.Effects.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server._Stalker.ZoneAnomaly.Effects.Systems;

public sealed class ZoneAnomalyEffectAddComponentSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZoneAnomalyEffectAddComponentComponent, ZoneAnomalyEntityAddEvent>(OnAdd);
        SubscribeLocalEvent<ZoneAnomalyEffectAddComponentComponent, ZoneAnomalyEntityRemoveEvent>(OnRemove);
    }

    private void OnAdd(Entity<ZoneAnomalyEffectAddComponentComponent> effect, ref ZoneAnomalyEntityAddEvent args)
    {
        AddComponents(args.Entity, effect.Comp.Components);
    }

    private void OnRemove(Entity<ZoneAnomalyEffectAddComponentComponent> effect, ref ZoneAnomalyEntityRemoveEvent args)
    {
        RemoveComponents(args.Entity, effect.Comp.Components);
    }

    private void AddComponents(EntityUid uid, ComponentRegistry components)
    {
        foreach (var (name, data) in components)
        {
            var component = (Component)_componentFactory.GetComponent(name);
            component.Owner = uid;

            var temp = (object)component;
            _serializationManager.CopyTo(data.Component, ref temp);
            RemComp(uid, temp!.GetType());
            AddComp(uid, (Component)temp);
        }
    }

    private void RemoveComponents(EntityUid uid, ComponentRegistry components)
    {
        foreach (var (name, data) in components)
        {
            var component = (Component)_componentFactory.GetComponent(name);
            component.Owner = uid;

            var temp = (object)component;
            _serializationManager.CopyTo(data.Component, ref temp);
            RemComp(uid, temp!.GetType());
        }
    }
}

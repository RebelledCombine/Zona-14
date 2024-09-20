using Content.Shared.Actions;
using Content.Shared.Popups;

namespace Content.Shared._Stalker.Psyonics.Actions;

public abstract class BasePsyonicsActionSystem<TActionComponent, TActionEvent> : EntitySystem where TActionComponent : BasePsyonicsActionComponent where TActionEvent : BaseActionEvent
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PsyonicsSystem _psyonics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TActionComponent, ComponentStartup>(ActionStartup);
        SubscribeLocalEvent<TActionComponent, TActionEvent>(ActionStarter);
    }

    private void ActionStartup(Entity<TActionComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ent.Comp.ActionId);

        OnStartup(ent, ref args);
    }

    private void ActionStarter(Entity<TActionComponent> ent, ref TActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PsyonicsComponent>(ent, out var comp))
            return;

        if (comp is not { } psyComp)
            return;

        var psyonics = (ent, psyComp);

        if (!_psyonics.HasPsy(psyonics, ent.Comp.Cost))
        {
            _popup.PopupEntity(Loc.GetString("psy-not-enough"), ent, ent);
            args.Handled = false;
            return;
        }

        OnAction(ent, ref args);

        if (args.Handled && ent.Comp.Consumable)
        {
            _psyonics.RemovePsy(psyonics, ent.Comp.Cost);
        }
    }

    protected virtual void OnStartup(Entity<TActionComponent> entity, ref ComponentStartup args)
    {

    }

    protected virtual void OnAction(Entity<TActionComponent> entity, ref TActionEvent args)
    {

    }
}

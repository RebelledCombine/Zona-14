using Content.Server._Stalker.Lay.Events;
using Content.Server.DoAfter;
using Content.Shared._Stalker.Lay;
using Content.Shared._Stalker.Lay.Events;
using Content.Shared.DoAfter;

namespace Content.Server._Stalker.Lay;

public sealed partial class STLaySystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeLaid();
        InitializeHandle();

        SubscribeLocalEvent<STLayComponent, STLayDoAfterEvent>(OnStateDoAfter);
        SubscribeLocalEvent<STLayComponent, STLayStateChangedEvent>(OnStateChanged);
    }

    private void OnStateDoAfter(Entity<STLayComponent> lay, ref STLayDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        SetState(lay, args.NextState);
    }

    private void OnStateChanged(Entity<STLayComponent> lay, ref STLayStateChangedEvent args)
    {
        switch (args.State)
        {
            case STLayState.Stand:
                RemComp<STLaidComponent>(lay);
                break;

            case STLayState.Laid:
                AddComp<STLaidComponent>(lay);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void StartSetState(Entity<STLayComponent> lay, STLayState state)
    {
        var delay = lay.Comp.ChangeStateDelay[state];

        if (delay == TimeSpan.Zero)
        {
            SetState(lay, state);
            return;
        }

        var args = new DoAfterArgs(EntityManager, lay, delay, new STLayDoAfterEvent(state), lay)
        {
            NeedHand = true,
            BreakOnHandChange = false,
            BreakOnMove = true,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void SetState(Entity<STLayComponent> lay, STLayState state)
    {
        lay.Comp.State = state;

        var ev = new STLayStateChangedEvent(state);
        RaiseLocalEvent(lay, ev);
    }
}

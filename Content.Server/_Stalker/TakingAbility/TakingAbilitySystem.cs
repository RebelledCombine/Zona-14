using Content.Server.Forensics;
using Content.Shared.Examine;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;

namespace Content.Server._Stalker.TakingAbility;

public sealed class TakingAbilitySystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TakingAbilityComponent, GetVerbsEvent<AlternativeVerb>>(OnAlt);
        SubscribeLocalEvent<TakingAbilityComponent, ExaminedEvent>(OnExamine);
    }

    private void OnAlt(Entity<TakingAbilityComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!_tag.HasTag(user, entity.Comp.Tag))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("st-taking-ability-toggle-verb"), // Zona14: localized
            Act = () =>
            {
                ToggleRemovable(entity);
            },
            Message = Loc.GetString("st-taking-ability-toggle-verb") // Zona14: localized
        };
        args.Verbs.Add(verb);
    }

    private void OnExamine(Entity<TakingAbilityComponent> entity, ref ExaminedEvent args)
    {
        switch (HasComp<UnremoveableComponent>(entity))
        {
            case true:
            {
                args.PushMarkup(Loc.GetString("st-taking-ability-locked")); // Zona14: localized
                break;
            }
            case false:
            {
                args.PushMarkup(Loc.GetString("st-taking-ability-unlocked")); // Zona14: localized
                break;
            }
        }
    }

    private void ToggleRemovable(EntityUid entity)
    {
        var xform = Transform(entity);

        // Check for wearing by human
        if (!HasComp<DnaComponent>(xform.ParentUid))
            return;

        if (HasComp<UnremoveableComponent>(entity))
        {
            RemComp<UnremoveableComponent>(entity);
            return;
        }
        AddComp<UnremoveableComponent>(entity);
    }
}

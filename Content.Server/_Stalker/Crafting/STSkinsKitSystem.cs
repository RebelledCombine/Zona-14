using Content.Shared.Crafting.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Tag;
using Content.Shared._Stalker.Crafting.Components;

namespace Content.Server.Crafting;
/// <summary>
/// Recipe system. Adds a list of recipes that can be crafted using light recipes and this kit to tool descriptions (e.g. skin kits) // Zona14: translated comment
/// </summary>
public sealed class STSkinsKitSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;
    private Dictionary<string, string> _descriptionsBySkinsKit = new();

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _logManager.GetSawmill("SkinKits");
        AddDescriptions();
        SubscribeLocalEvent<STSkinsKitComponent, ExaminedEvent>(OnSkinKitExamine);
    }

    /// <summary>
    /// On Shift-Right click shows the detailed crafting recipe in the description // Zona14: translated comment
    /// </summary>
    public void OnSkinKitExamine(EntityUid uid, STSkinsKitComponent component, ExaminedEvent args)
    {
        if (!TryComp<MetaDataComponent>(args.Examined, out var meta))
            return;
        if (meta.EntityPrototype is null)
            return;
        if (!args.IsInDetailsRange)
            return;
        if (!_descriptionsBySkinsKit.TryGetValue(meta.EntityPrototype.ID, out var description))
            return;

        args.PushMarkup(description);
    }

    private void AddDescriptions()
    {
        var lightCrafts = _proto.EnumeratePrototypes<LightCraftingPrototype>();

        foreach (var skinCraft in lightCrafts)
        {
            var (ingredientId, keepedName) = skinCraft.Steps.KeepFirst
                ? (skinCraft.Steps.SecondIngredient, skinCraft.Steps.FirstIngredient.Id)
                : (skinCraft.Steps.FirstIngredient, skinCraft.Steps.SecondIngredient.Id);

            if (!_proto.TryIndex(ingredientId, out var ingredient))
                continue;
            if (skinCraft.Results[0] is null)
                continue;
            if (!_proto.TryIndex(skinCraft.Results[0], out var result))
                continue;

            var ingredientName = $"{ingredient.Name} {Loc.GetString("st-lightcraft-arrow")} {result.Name}\n";
            _descriptionsBySkinsKit[keepedName] = _descriptionsBySkinsKit.TryGetValue(keepedName, out var description)
                ? description + ingredientName
                : $"{Loc.GetString("st-lightcraft-skins-recipes")}\n{ingredientName}";
        }
    }
}

using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent; // stalker-changes
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Server.Fluids.EntitySystems;

public sealed partial class PuddleSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    // stalker-changes-start
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string STWater = "STWater";

    [ValidatePrototypeId<ReagentPrototype>]
    private const string Vodka = "Vodka";
    [ValidatePrototypeId<ReagentPrototype>]
    private static string[] evaporationReagents = new[] {"Absinthe", "BlueCuracao", "Champagne", "Cognac", "Cola",
                                                        "Grenadine", "Gin", "Gildlager", "CoffeeLiqueur", "MelonLiquor",
                                                        "Patron", "PoisonWine", "Rum", "SpaceMountainWind", "SpaceUp", "Tequila",
                                                        "Vermouth", "Vodka", "Whiskey", "Wine", "Beer", "Ale",
                                                        "Water", "SodaWater", "TonicWater", "Sake", "JuiceLime", "JuiceOrange",
                                                        "Cream", "Sugar", "LemonLime", "Mead", "Ice", "CoconutWater", "Coffee",
                                                        "Tea", "GreenTea", "IcedTea", "DrGibb", "RootBeer", "JuiceWatermelon", "EnergyDrink",
                                                        "STWater","Blood","Lemonade", "STTaurine", "RootBeer", "Tea", "Milk", "SolDry", "JuiceTomato"};

    public static global::System.String[] EvaporationReagents { get => evaporationReagents; set => evaporationReagents = value; }

    //stalker-changes-end
    private void OnEvaporationMapInit(Entity<EvaporationComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextTick = _timing.CurTime + EvaporationCooldown;
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (HasComp<EvaporationComponent>(uid))
        {
            return;
        }

        if (solution.GetTotalPrototypeQuantity(EvaporationReagents) > FixedPoint2.Zero)
        {
            var evaporation = AddComp<EvaporationComponent>(uid);
            evaporation.NextTick = _timing.CurTime + EvaporationCooldown;
            return;
        }

        RemComp<EvaporationComponent>(uid);
    }

    private void TickEvaporation()
    {
        var query = EntityQueryEnumerator<EvaporationComponent, PuddleComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var evaporation, out var puddle))
        {
            if (evaporation.NextTick > curTime)
                continue;

            evaporation.NextTick += EvaporationCooldown;

            if (!_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var puddleSolution))
                continue;

            var reagentTick = evaporation.EvaporationAmount * EvaporationCooldown.TotalSeconds;
            puddleSolution.SplitSolutionWithOnly(reagentTick, EvaporationReagents);

            // Despawn if we"re done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // Spawn a *sparkle*
                Spawn("PuddleSparkle", xformQuery.GetComponent(uid).Coordinates);
                QueueDel(uid);
            }
        }
    }
}

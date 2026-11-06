using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using FrierenMod.Scripts.Relics;

namespace FrierenMod.Scripts.Events;

public sealed class FoodEvent : FrierenStoryEvent
{
    protected override string[] Portraits => ["FoodEvent", "FreeFood", "Package", "Meal", "BigMeal"];
    public override bool IsAllowed(IRunState runState) => base.IsAllowed(runState) && runState.Players.All(p => p.Gold >= 75);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MealHeal", 0), new DynamicVar("FeastHeal", 0)];
    public override void CalculateVars()
    {
        base.CalculateVars();
        DynamicVars["MealHeal"].BaseValue = decimal.Floor(Owner!.Creature.MaxHp * 0.50m);
        DynamicVars["FeastHeal"].BaseValue = Owner.Creature.MaxHp;
    }
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("FREE", Free), CardChoice<EmergencyFood>("PACKAGE", Package, Owner!.Gold >= 75),
        Choice(Owner!.GetRelic<MercuryPudding>() == null ? "MEAL" : "MEAL_STRAWBERRY", Meal, Owner!.Gold >= 150), Choice("FEAST", Feast, Owner!.Gold >= 250)
    ];
    private async Task Free() { await CreatureCmd.Heal(Owner!.Creature, 15); Finish("FREE", 1); }
    private async Task Package()
    {
        await PlayerCmd.LoseGold(75, Owner!, GoldLossType.Spent);
        await AddCard<EmergencyFood>(); Finish("PACKAGE", 2);
    }
    private async Task Meal()
    {
        var heal = DynamicVars["MealHeal"].BaseValue;
        await PlayerCmd.LoseGold(150, Owner!, GoldLossType.Spent);
        if (Owner!.GetRelic<MercuryPudding>() == null) await RelicCmd.Obtain<MercuryPudding>(Owner);
        else await RelicCmd.Obtain<Strawberry>(Owner);
        await CreatureCmd.Heal(Owner!.Creature, heal); Finish("MEAL", 3);
    }
    private async Task Feast()
    {
        await PlayerCmd.LoseGold(250, Owner!, GoldLossType.Spent);
        await RelicCmd.Obtain<Mango>(Owner!);
        await CreatureCmd.Heal(Owner!.Creature, Owner.Creature.MaxHp); Finish("FEAST", 4);
    }
}

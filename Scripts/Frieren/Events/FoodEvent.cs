using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Relics;

namespace FrierenMod.Scripts.Events;

public sealed class FoodEvent : FrierenStoryEvent
{
    protected override string[] Portraits => ["FoodEvent", "FreeFood", "Package", "Meal", "BigMeal"];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MealHeal", 0), new DynamicVar("FeastHeal", 0)];
    public override void CalculateVars()
    {
        base.CalculateVars();
        DynamicVars["MealHeal"].BaseValue = decimal.Floor(Owner!.Creature.MaxHp * 0.30m);
        DynamicVars["FeastHeal"].BaseValue = decimal.Floor(Owner.Creature.MaxHp * 0.50m);
    }
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("FREE", Free), Choice("PACKAGE", Package, Owner!.Gold >= 75),
        Choice("MEAL", Meal, Owner!.Gold >= 150), Choice("FEAST", Feast, Owner!.Gold >= 250)
    ];
    private async Task Free() { await CreatureCmd.Heal(Owner!.Creature, 8); Finish("FREE", 1); }
    private async Task Package()
    {
        await PlayerCmd.LoseGold(75, Owner!, GoldLossType.Spent);
        await AddCard<EmergencyFood>(); Finish("PACKAGE", 2);
    }
    private async Task Meal()
    {
        var heal = DynamicVars["MealHeal"].BaseValue;
        await PlayerCmd.LoseGold(150, Owner!, GoldLossType.Spent);
        await RelicCmd.Obtain<Strawberry>(Owner!);
        await CreatureCmd.Heal(Owner!.Creature, heal); Finish("MEAL", 3);
    }
    private async Task Feast()
    {
        var heal = DynamicVars["FeastHeal"].BaseValue;
        await PlayerCmd.LoseGold(250, Owner!, GoldLossType.Spent);
        await RelicCmd.Obtain<Mango>(Owner!);
        await CreatureCmd.Heal(Owner!.Creature, heal); Finish("FEAST", 4);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace FrierenMod.Scripts.Events;

public sealed class LookUp : FrierenStoryEvent
{
    protected override string[] Portraits => ["LookUp"];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var up = CardChoice<Enlightenment>("UP", Up, Owner!.Creature.MaxHp > 7, configure: MakeSteady);
        up.HoverTips = up.HoverTips.Concat(HoverTipFactory.FromEnchantment<Steady>());
        return [up, Choice("DOWN", Down)];
    }

    private static void MakeSteady(CardModel card) => CardCmd.Enchant<Steady>(card, 1);

    private async Task Up()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner!.Creature, 7, null);
        if (Owner.Creature.IsDead) return;
        await AddCard<Enlightenment>(configure: MakeSteady);
        Finish("UP", 0);
    }

    private async Task Down()
    {
        await PlayerCmd.GainGold(Rng.NextInt(75, 101), Owner!);
        Finish("DOWN", 0);
    }
}

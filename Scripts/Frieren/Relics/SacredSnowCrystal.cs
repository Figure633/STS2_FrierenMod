using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Relics;

public sealed class SacredSnowCrystal : FrierenRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext context, ICombatState combat)
    {
        if (player != Owner || Owner.PlayerCombatState!.TurnNumber != 1) return;
        Flash();
        var pool = Owner.Character.CardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Rare);
        var choices = CardFactory.GetDistinctForCombat(Owner, pool, 2, Owner.RunState.Rng.CombatCardGeneration).ToList();
        var selected = await CardSelectCmd.FromChooseACardScreen(context, choices, Owner);
        if (selected == null) return;
        selected.AddKeyword(CardKeyword.Retain);
        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, Owner);
    }
}

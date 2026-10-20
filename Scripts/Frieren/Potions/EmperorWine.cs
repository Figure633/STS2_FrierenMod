using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace FrierenMod.Scripts.Potions;

public sealed class EmperorWine : FrierenPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override string CustomPackedImagePath => ModelDb.Potion<EnergyPotion>().ImagePath;
    public override string? CustomPackedOutlinePath => ModelDb.Potion<EnergyPotion>().OutlinePath;
    protected override async Task OnUse(PlayerChoiceContext context, Creature? target)
    {
        var pool = Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Rare && c.CanBeGeneratedInCombat);
        var cards = CardFactory.GetDistinctForCombat(Owner, pool, 3, Owner.RunState.Rng.CombatCardGeneration).ToArray();
        if (cards.Length == 0) return;
        var selected = await CardSelectCmd.FromChooseACardScreen(context, cards, Owner, canSkip: true);
        if (selected == null) return;
        selected.SetToFreeThisTurn(); await CardFlowCmd.AddGenerated(selected, PileType.Hand, Owner);
    }
}

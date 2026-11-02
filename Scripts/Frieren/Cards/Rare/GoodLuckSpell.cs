using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class GoodLuckSpell() : LegendaryCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public CardModel[] CreateChoices() => new CardModel[] {
        CombatState!.CreateCard<StrongerWish>(Owner), CombatState.CreateCard<RewardWish>(Owner), CombatState.CreateCard<ManaWish>(Owner)
    };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var choices = CreateChoices();
        var selected = await CardSelectCmd.FromChooseACardScreen(context, choices, Owner);
        if (selected != null) await ResolveChoice(context, selected);
        foreach (var card in choices) card.RemoveFromState();
    }
    public async Task ResolveChoice(PlayerChoiceContext context, CardModel choice)
    {
        if (choice is StrongerWish) {
            var candidates = Owner.Deck.Cards.Where(c => c.IsUpgradable).ToArray();
            if (candidates.Length > 0) CardCmd.Upgrade(candidates[Owner.RunState.Rng.CombatCardSelection.NextInt(candidates.Length)]);
        } else if (choice is RewardWish) {
            await PowerCmd.Apply<GoodLuckRewardPower>(context, Owner.Creature, 1, Owner.Creature, this);
        } else if (choice is ManaWish) {
            int limit = BaseLib.Patches.Hooks.MaxHandSizePatch.GetMaxHandSize(Owner, CardPile.MaxCardsInHand);
            await ManaCmd.Generate(Owner, PileType.Hand, Math.Max(0, limit - Owner.PlayerCombatState!.Hand.Cards.Count));
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

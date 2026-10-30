using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class GoodLuckSpell() : LegendaryCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Mana", 6)];
    public CardModel[] CreateChoices() => new CardModel[] {
        CombatState!.CreateCard<StrongerWish>(Owner), CombatState.CreateCard<RewardWish>(Owner), CombatState.CreateCard<ManaWish>(Owner)
    };
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var choices = CreateChoices();
        if (IsUpgraded) foreach (var card in choices.Where(c => c.IsUpgradable)) CardCmd.Upgrade(card);
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
            await ManaCmd.Generate(Owner, PileType.Hand, DynamicVars["Mana"].IntValue);
        }
    }
    protected override void OnUpgrade() => DynamicVars["Mana"].UpgradeValueBy(2);
}

using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class Snuggle() : FrierenCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1)];

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var first = Owner.Creature.CombatState!.CreateCard<Snuggle1>(Owner);
        first.DynamicVars["Amount"].BaseValue = DynamicVars["Amount"].BaseValue;
        var second = Owner.Creature.CombatState.CreateCard<Snuggle2>(Owner);
        var selected = await CardSelectCmd.FromChooseACardScreen(context, [first, second], Owner);
        if (selected is Snuggle1) {
            // 一代实际允许弃任意手牌；仅弃掉的魔力返能，普通牌不返能。
            var prefs = new CardSelectorPrefs(new LocString("frieren", "SNUGGLE_DISCARD"), 0, (int)DynamicVars["Amount"].BaseValue);
            var cards = (await CardSelectCmd.FromHand(context, Owner, prefs, c => c != this, this)).ToArray();
            int mana = cards.Count(ManaCmd.IsMana);
            await CardCmd.Discard(context, cards);
            if (mana > 0) await PlayerCmd.GainEnergy(mana, Owner);
        } else if (selected is Snuggle2) {
            await ManaCmd.Generate(Owner, PileType.Hand, 1);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);
}

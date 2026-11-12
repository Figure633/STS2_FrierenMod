using FrierenMod.Scripts.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class RustCleanMagic() : FrierenCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ManaSource",1)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var selected = await CardSelectCmd.FromCombatPile(context, PileType.Draw.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt,1), card => card.IsTransformable);
        foreach (var card in selected) {
            var mana = CombatState!.CreateCard<Mana>(Owner);
            CardCmd.Enchant<ManaSource>(mana, DynamicVars["ManaSource"].BaseValue);
            await CardCmd.Transform(card, mana);
        }
    }
    protected override void OnUpgrade() => DynamicVars["ManaSource"].UpgradeValueBy(1);
}

using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class SwampSpell() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 3)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int needed=DynamicVars["Amount"].IntValue,moved=0;
        var cards=ManaCmd.InPile(Owner,PileType.Draw).Take(needed).ToArray();
        foreach(var card in cards) {
            var result=await CardPileCmd.Add(card,PileType.Hand);
            // 满手牌转入弃牌堆也算已从抽牌堆取出，不再为这一张重复补发。
            if(result.success && card.Pile?.Type!=PileType.Draw)moved++;
        }
        if(moved<needed)await PowerCmd.Apply<SwampManaNextTurnPower>(context,Owner.Creature,needed-moved,Owner.Creature,this);
    }
    protected override void OnUpgrade() { DynamicVars["Amount"].UpgradeValueBy(1); }
}

using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class ReadyStance() : FrierenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, play).Targeting(play.Target!).Execute(context);
        // 先选定费技能；同费用保留牌顶顺序，仅剩X费技能时才抽X费牌。
        var skill = Owner.PlayerCombatState!.DrawPile.Cards.Where(c=>c.Type==CardType.Skill)
            .OrderBy(c=>c.EnergyCost.CostsX)
            .ThenBy(c=>c.EnergyCost.CostsX?0:Math.Max(0,c.EnergyCost.GetWithModifiers(CostModifiers.All))).FirstOrDefault();
        if (skill == null) return;
        await CardPileCmd.Add(skill, PileType.Draw, CardPilePosition.Top);
        await CardPileCmd.Draw(context, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

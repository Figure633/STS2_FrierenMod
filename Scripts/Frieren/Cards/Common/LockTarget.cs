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

public sealed class LockTarget() : FrierenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int hits = ManaCmd.InPile(Owner, PileType.Draw).Count() >= ManaCmd.InPile(Owner, PileType.Discard).Count() ? 2 : 1;
        for (int i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
            await CardPileCmd.Draw(context, 1, Owner);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3); }
}

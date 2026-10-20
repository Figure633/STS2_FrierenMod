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

public sealed class Envy() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IChantListener
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (Pile?.Type == PileType.Discard) await CardPileCmd.Add(this, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

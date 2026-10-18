using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Famehameha() : LegendaryCard(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IChantListener
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(31, ValueProp.Move)];
    public Task AfterChant(PlayerChoiceContext context, CardPlay play) { EnergyCost.AddThisTurn(-1); return Task.CompletedTask; }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(10);
}

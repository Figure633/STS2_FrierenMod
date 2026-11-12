using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class Kamehameha() : LegendaryCard(5, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy), IChantListener, IGeneratedOnlyCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(66, ValueProp.Move)];
    public Task AfterChant(PlayerChoiceContext context, CardPlay play) { EnergyCost.AddThisTurn(-1); return Task.CompletedTask; }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCardCompatibility(this, play).Targeting(play.Target!).Execute(context);
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(22);
}

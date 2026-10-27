using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class Recycle() : FrierenCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<RecyclePower>(2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<RecyclePower>(context, Owner.Creature, DynamicVars["RecyclePower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

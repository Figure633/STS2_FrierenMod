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

public sealed class MultipleOffensiveMagic() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int x = ResolveEnergyXValue();
        int hits = x + (IsUpgraded ? 1 : 0);
        if (hits > 0) await DamageCmd.Attack(x + 4).FromCard(this).TargetingRandomOpponents(CombatState!).WithHitCount(hits).Execute(context);
        await ManaCmd.Generate(Owner, PileType.Hand, x);
    }
    protected override bool HasEnergyCostX => true;
}

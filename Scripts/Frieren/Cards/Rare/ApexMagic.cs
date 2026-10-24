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

public sealed class ApexMagic() : FrierenCard(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override bool ShouldGlowGoldInternal => CombatState != null &&
        new[] { PileType.Draw, PileType.Hand, PileType.Discard }.All(p => ManaCmd.InPile(Owner, p).Count() == 4);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (new[] { PileType.Draw, PileType.Hand, PileType.Discard }.All(p => ManaCmd.InPile(Owner, p).Count() == 4)) {
            Presentation.FrierenAchievements.Unlock(Owner, "4003");
            await CreatureCmd.Kill(CombatState!.Enemies.Where(e => e.IsAlive).ToArray());
        }
    }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }
}

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
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
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
            var finale = NGrandFinaleVfx.Create(Owner.Creature);
            if (finale != null) {
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(finale);
                await Cmd.Wait(NGrandFinaleVfx.totalAnticipationDuration);
            }
            var targets = CombatState!.Enemies.Where(e => e.IsAlive).ToArray();
            foreach (var target in targets)
                target.GetVfxContainer()?.AddChildSafely(NGrandFinaleImpactVfx.Create(target));
            await CreatureCmd.Kill(targets);
        }
    }
    protected override void OnUpgrade() { AddKeyword(CardKeyword.Retain); }
}

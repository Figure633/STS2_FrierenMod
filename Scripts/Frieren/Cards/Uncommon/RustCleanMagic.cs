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

public sealed class RustCleanMagic() : LegendaryCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (Owner.Creature.CombatState!.Encounter?.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
            await PowerCmd.Remove(play.Target!.Powers.FirstOrDefault(x => x.Type == PowerType.Buff));
        await PowerCmd.Remove(Owner.Creature.Powers.FirstOrDefault(x => x.Type == PowerType.Debuff));
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

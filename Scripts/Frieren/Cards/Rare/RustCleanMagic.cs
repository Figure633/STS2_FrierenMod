using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class RustCleanMagic() : LegendaryCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Gold",40)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var debuffs = Owner.Creature.Powers.Where(p=>p.Type==PowerType.Debuff).ToArray();
        foreach(var power in debuffs) await PowerCmd.Remove(power);
        int removed = debuffs.Count(power=>!Owner.Creature.Powers.Contains(power));
        if (removed > 0) await PlayerCmd.GainGold(removed * DynamicVars["Gold"].IntValue, Owner);
        if(removed>=3)
            Presentation.FrierenAchievements.Unlock(Owner,"4006");
    }
    protected override void OnUpgrade() => DynamicVars["Gold"].UpgradeValueBy(10);
}

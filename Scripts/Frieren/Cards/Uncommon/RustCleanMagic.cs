using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

public sealed class RustCleanMagic() : LegendaryCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var debuffs = Owner.Creature.Powers.Where(p=>p.Type==PowerType.Debuff).ToArray();
        if (debuffs.Length==0) { await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue,Owner); return; }
        bool hex=debuffs.Any(p=>p is MegaCrit.Sts2.Core.Models.Powers.HexPower);
        foreach(var power in debuffs) await PowerCmd.Remove(power);
        if(hex && !Owner.Creature.HasPower<MegaCrit.Sts2.Core.Models.Powers.HexPower>())
            Presentation.FrierenAchievements.Unlock(Owner,"4006");
    }
    protected override void OnUpgrade() => DynamicVars.Energy.UpgradeValueBy(1);
}

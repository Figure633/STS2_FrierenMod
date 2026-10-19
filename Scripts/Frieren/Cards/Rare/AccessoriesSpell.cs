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

public sealed class AccessoriesSpell() : LegendaryCard(4, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override bool CanBeGeneratedInCombat => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Innate, CardKeyword.Ethereal];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<GainRelicPower>(context, Owner.Creature, (int)DynamicVars["Amount"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { EnergyCost.UpgradeBy(-1); }
}

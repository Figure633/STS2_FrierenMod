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

public sealed class ManaParcel() : FrierenCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 3)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await Task.CompletedTask;
    }
    protected override void OnUpgrade() { DynamicVars["Amount"].UpgradeValueBy(1); }
    public override async Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await CardCmd.Exhaust(context, this);
        await ManaCmd.Generate(Owner, PileType.Hand, (int)DynamicVars["Amount"].BaseValue, ignoreBan: true);
    }
}

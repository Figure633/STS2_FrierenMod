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
using FrierenMod.Scripts.Presentation;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

public sealed class HighDensityMana() : FrierenCard(0, CardType.Status, CardRarity.Uncommon, TargetType.Self), IManaCard
{
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("Synchro", new LocString("frieren", $"TIP.{SynchroPresentation.Key(this)}.title").GetFormattedText());
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 1)];
    protected override PileType GetResultPileTypeForCardPlay()
    {
        var pile = base.GetResultPileTypeForCardPlay();
        return pile == PileType.Discard ? PileType.Exhaust : pile;
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await SynchroCmd.Play(context, this);
    }
    protected override void OnUpgrade() { DynamicVars["Amount"].UpgradeValueBy(1); }
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card == this) await PlayerCmd.GainEnergy((int)DynamicVars["Amount"].BaseValue, Owner);
    }
}

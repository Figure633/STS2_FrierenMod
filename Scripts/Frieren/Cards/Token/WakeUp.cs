using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace FrierenMod.Scripts.Cards;

public sealed class WakeUp() : FrierenCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self), IGeneratedOnlyCard
{
    protected override IEnumerable<MegaCrit.Sts2.Core.HoverTips.IHoverTip> ExtraHoverTips =>
        base.ExtraHoverTips.Append(MegaCrit.Sts2.Core.HoverTips.HoverTipFactory.FromPower<BarricadePower>());
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("StoredEnergy", IsMutable && Owner?.Creature.GetPower<SleepInPower>() is {} power ? power.StoredEnergy : 0);
    }
    public static async Task Resolve(PlayerChoiceContext context, MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        int energy = 0;
        if (player.Creature.GetPower<SleepInPower>() is {} sleep) {
            energy = sleep.TakeEnergy();
            await PowerCmd.Remove(sleep);
        }
        if (player.Creature.GetPower<BarricadePower>() is {} barricade) await PowerCmd.Remove(barricade);
        if (energy > 0) await PlayerCmd.GainEnergy(energy, player);
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => Resolve(context, Owner);
    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (card == this && Owner.Creature.HasPower<SleepInPower>()) await Resolve(context, Owner);
    }
}

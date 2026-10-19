using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

public sealed class LegendaryCraftChoice() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public LegendaryEffect Effect { get; set; } = new("Cost", 0);
    protected override string ArtName => nameof(SerieGrimoire);
    protected override bool IsPlayable => false;
    public override int MaxUpgradeLevel => 0;
    protected override void AddExtraArgsToDescription(LocString description)
    {
        var text = new LocString("frieren", "CRAFT." + Effect.Kind);
        text.Add("Amount", Effect.Amount);
        description.Add("Effect", text.GetFormattedText());
    }
}

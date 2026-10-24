using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Cards;

public sealed class LegendaryCraftChoice() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public LegendaryEffect Effect { get; set; } = new("Cost", 0);
    // 一代 optionCards/SerieGrimoire 各选项的原始插画。
    protected override string ArtName => Effect.Kind switch {
        "Cost" => $"Cost{Effect.Amount}",
        "Chant" => "ChantOption",
        "ManaDraw" => "ManaInDrawOption",
        "ManaHand" => "ManaInHandOption",
        "ManaDiscard" => "ManaInDiscardOption",
        "Scry" => "ScryOption",
        "RandomCard" => "GainRandomCardOption",
        "Retain" => "SelfRetainOption",
        "ZeroCost" => "CostZeroOption",
        "Block" => "BlockOption",
        "Damage" => "DamageOption",
        "DamageTriple" => "DamageThirdOption",
        "DamageAll" => "DamageAllOption",
        "Energy" => "EnergyOption",
        "Execute" => "ExtinguishOption",
        "Draw" => "DrawOption",
        "Dexterity" => "DexterityOption",
        "Vulnerable" => "VulnerableOption",
        "Intangible" => "IntangibleOption",
        "UpgradeHand" => "UpgradeOption",
        "DoubleStrength" => "StrengthOption",
        "RemoveDebuffs" => "RemoveDebuffOption",
        "UpgradeAll" => "UpgradeAllOption",
        _ => nameof(SerieGrimoire)
    };
    public override string Title => new LocString("frieren", "CRAFT_TITLE." + ArtName).GetFormattedText();
    protected override bool IsPlayable => false;
    public override int MaxUpgradeLevel => 0;
    protected override void AddExtraArgsToDescription(LocString description)
    {
        var text = new LocString("frieren", "CRAFT." + Effect.Kind);
        if (Effect.Kind is "Cost" or "Energy")
            text.Add(new EnergyVar("Amount", Effect.Amount) { ColorPrefix = EnergyIconHelper.GetPrefix(this) });
        else text.Add("Amount", Effect.Amount);
        description.Add("Effect", text.GetFormattedText());
    }
}

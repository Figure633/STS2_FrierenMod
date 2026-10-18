using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Cards;

public abstract class ChantChoice() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    private string _spellId = "AdvancedDefensiveMagic";
    protected override string ArtName => _spellId;
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ManaCost", 0), new DynamicVar("Reward", 0)];

    public void Configure(ChantOption option)
    {
        AssertMutable();
        _spellId = option.SpellId;
        DynamicVars["ManaCost"].BaseValue = option.Cost;
        DynamicVars["Reward"].BaseValue = option.Reward;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        var effect = new LocString("cards", $"FRIERENMOD-CHANT_EFFECT.{_spellId}");
        effect.Add("Reward", DynamicVars["Reward"].IntValue);
        description.Add("Effect", effect.GetFormattedText());
    }
}

public sealed class DrawChantChoice : ChantChoice;
public sealed class HandChantChoice : ChantChoice;
public sealed class DiscardChantChoice : ChantChoice;

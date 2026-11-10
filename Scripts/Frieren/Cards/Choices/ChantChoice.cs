using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.Cards;

public abstract class ChantChoice() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    private string _spellId = "AdvancedDefensiveMagic";
    public int GenerationAmount { get; private set; }
    public ManaSlot Slot => this is DrawChantChoice ? ManaSlot.Draw : this is HandChantChoice ? ManaSlot.Hand : ManaSlot.Discard;
    public static string ArtFor(ManaSlot slot)=>slot switch {
        ManaSlot.Draw=>nameof(DrawChantChoice),ManaSlot.Hand=>nameof(HandChantChoice),ManaSlot.Discard=>nameof(DiscardChantChoice),
        _=>throw new ArgumentOutOfRangeException(nameof(slot))};
    protected override string ArtName => ArtFor(Slot);
    public override string Title => GenerationAmount > 0 ? new LocString("frieren", "SLOT." + Slot).GetFormattedText() : base.Title;
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("ManaCost", 0), new DynamicVar("Reward", 0)];

    public void Configure(ChantOption option)
    {
        AssertMutable();
        GenerationAmount = 0;
        _spellId = option.SpellId;
        DynamicVars["ManaCost"].BaseValue = option.Cost;
        DynamicVars["Reward"].BaseValue = option.Reward;
    }

    public static ChantChoice[] ManaGenerationChoices(Player player, int amount)
    {
        ChantChoice[] cards = [(ChantChoice)ModelDb.Card<DrawChantChoice>().ToMutable(),
            (ChantChoice)ModelDb.Card<HandChantChoice>().ToMutable(), (ChantChoice)ModelDb.Card<DiscardChantChoice>().ToMutable()];
        foreach (var card in cards) { card.Owner = player; card.GenerationAmount = amount; }
        return cards;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        var text = new LocString("frieren", GenerationAmount > 0 ? "MANA_GENERATE_CHOICE" : "LEGACY_CHANT_CHOICE");
        text.Add("Slot", new LocString("frieren", "SLOT." + Slot).GetFormattedText());
        text.Add("Amount", GenerationAmount > 0 ? GenerationAmount : DynamicVars["ManaCost"].IntValue);
        if (GenerationAmount > 0) { description.Add("Effect", text.GetFormattedText()); return; }
        var effect = new LocString("cards", $"FRIERENMOD-CHANT_EFFECT.{_spellId}");
        effect.Add("Reward", DynamicVars["Reward"].IntValue);
        text.Add("Effect", effect.GetFormattedText());
        description.Add("Effect", text.GetFormattedText());
    }
}

using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class CustomLegendarySpell() : LegendaryCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private string _recipe = "[]";
    [SavedProperty]
    public string Recipe
    {
        get => _recipe;
        set
        {
            AssertMutable();
            _recipe = value;
            DynamicVars.Damage.BaseValue = Effects.FirstOrDefault(e => e.Kind is "Damage" or "DamageAll" or "DamageTriple").Amount;
            DynamicVars.Block.BaseValue = Effects.FirstOrDefault(e => e.Kind == "Block").Amount;
            if (Effects.Any(e => e.Kind is "Dexterity" or "Intangible" or "DoubleStrength" or "UpgradeAll")) AddKeyword(CardKeyword.Exhaust);
            else RemoveKeyword(CardKeyword.Exhaust);
        }
    }
    private int _craftedCost;
    [SavedProperty]
    public int CraftedCost { get => _craftedCost; set { AssertMutable(); _craftedCost = value; EnergyCost.SetCustomBaseCost(value); } }
    public LegendaryEffect[] Effects => JsonSerializer.Deserialize<LegendaryEffect[]>(Recipe)!;
    public override int MaxUpgradeLevel => 0;
    public override bool CanBeGeneratedInCombat => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(0, ValueProp.Move), new BlockVar(0, ValueProp.Move)];
    public override CardType Type => Effects.Any(e => e.Kind is "Damage" or "DamageAll" or "DamageTriple") ? CardType.Attack : CardType.Skill;
    public override TargetType TargetType => Effects.Any(e => e.Kind is "Damage" or "DamageTriple" or "Execute")
        ? TargetType.AnyEnemy : Effects.Any(e => e.Kind == "DamageAll") ? TargetType.AllEnemies : TargetType.Self;
    public void Configure(int cost, IEnumerable<LegendaryEffect> effects)
    {
        Recipe = JsonSerializer.Serialize(effects);
        CraftedCost = cost;
    }
    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("Effects", string.Join("\n", Effects.Select(e => {
            var text = new LocString("frieren", "CRAFT." + e.Kind);
            // 与原版 diff() 使用同一格式化入口：显示实际整数，并保留增减益颜色。
            text.Add("Amount", e.Kind switch {
                "Damage" or "DamageAll" or "DamageTriple" => DynamicVars.Damage.ToHighlightedString(inverse: false),
                "Block" => DynamicVars.Block.ToHighlightedString(inverse: false),
                _ => e.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
            return text.GetFormattedText();
        })));
    }
    protected override Task OnPlay(PlayerChoiceContext context, CardPlay play) => LegendaryCraftCmd.Play(context, play, this);
}

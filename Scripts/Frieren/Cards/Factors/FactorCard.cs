using FrierenMod.Core;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Hooks;

namespace FrierenMod.Scripts.Cards;

// 因子仅作为选择与预览模型，不进入普通卡牌奖励池和战斗牌堆。
public abstract class FactorCard() : FrierenCard(-1, CardType.Skill, CardRarity.Token, TargetType.Self), ICustomTypeTextCard
{
    public string FactorId => GetType().Name;
    public override CardRarity Rarity => SpellCatalog.Get(FactorId).Rarity switch {
        FactorRarity.Uncommon => CardRarity.Uncommon,
        FactorRarity.Rare => CardRarity.Rare,
        _ => CardRarity.Common
    };
    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public IEnumerable<LocString> GetTypeModifiers() => [new("frieren", "FACTOR_TYPE")];
    public ManaSlot? Slot { get; private set; }
    public ChantOption? Option { get; private set; }
    public bool CanSelectForChant { get; private set; } = true;
    public int? InventoryInstanceId { get; set; }
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ManaCost", 0), new DynamicVar("Reward", 0)];

    public static FactorCard Create(Player player, string id, ManaSlot? slot = null, ChantOption? option = null,
        bool canSelectForChant = true)
    {
        _ = SpellCatalog.Get(id);
        var type = typeof(FactorCard).Assembly.GetType(typeof(FactorCard).Namespace + "." + id, throwOnError: true)!;
        var card = (FactorCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
        card.Owner = player;
        card.Slot = slot;
        card.Option = option;
        card.CanSelectForChant = canSelectForChant;
        if (option.HasValue)
        {
            card.DynamicVars["ManaCost"].BaseValue = option.Value.Cost;
            card.DynamicVars["Reward"].BaseValue = option.Value.Reward;
        }
        return card;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        string contextText = "";
        if (Option.HasValue || Slot.HasValue || InventoryInstanceId.HasValue) {
            string key = InventoryInstanceId.HasValue ? "INVENTORY_CONTEXT" : Option.HasValue ? "CHANT" : "INITIAL";
            var context = new LocString("frieren", key);
            context.Add("Slot", new LocString("frieren", Slot.HasValue ? $"SLOT.{Slot}" : "UNEQUIPPED").GetFormattedText());
            context.Add("Id", InventoryInstanceId ?? 0);
            context.Add("Cost", Option?.Cost ?? 0);
            contextText = context.GetFormattedText() + "\n";
        }
        description.Add("Context", contextText);
        var effect = new LocString("frieren", (Option.HasValue ? "EFFECT." : "FACTOR.") + FactorId);
        if (Option.HasValue && FactorId == nameof(AdvancedDefensiveMagic)) {
            // 选择模型不在战斗牌堆，原生预览不会自动跑增益钩子；显式复用格挡计算。
            var block = new BlockVar("Reward", Option.Value.Reward, ValueProp.Move);
            if (Owner?.Creature.CombatState is { } combat)
                block.PreviewValue = Hook.ModifyBlock(combat, Owner.Creature, block.BaseValue, ValueProp.Move, this, null, out _);
            effect.Add(block);
        } else effect.Add("Reward", Option?.Reward ?? 0);
        effect.Add("Cost", Option?.Cost ?? 0);
        if (Option.HasValue && SpellCatalog.Get(FactorId).Puzzle)
        {
            // 一代战斗卡面只提示当前槽位的符号，不揭示解法。
            ManaSlot clueSlot = FactorId switch {
                nameof(Factor100) => ManaSlot.Draw,
                nameof(Factor010) => ManaSlot.Hand,
                _ => ManaSlot.Discard
            };
            effect.Add("PuzzleMark", new LocString("frieren", Option.Value.Slot == clueSlot ? "PUZZLE_MATCH" : "PUZZLE_MISS").GetFormattedText());
        }
        description.Add("Effect", effect.GetFormattedText());
    }
}

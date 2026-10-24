using FrierenMod.Core;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Helpers;

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
    private CardModel? _chantSource;
    public bool CanSelectForChant { get; private set; } = true;
    public int? InventoryInstanceId { get; set; }
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ManaCost", 0), new DynamicVar("Reward", 0)];

    public static FactorCard Create(Player player, string id, ManaSlot? slot = null, ChantOption? option = null,
        bool canSelectForChant = true, CardModel? chantSource = null)
    {
        _ = SpellCatalog.Get(id);
        var type = typeof(FactorCard).Assembly.GetType(typeof(FactorCard).Namespace + "." + id, throwOnError: true)!;
        var card = (FactorCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
        card.Owner = player;
        card.Slot = slot;
        card.Option = option;
        card._chantSource = chantSource;
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
        if ((Option.HasValue || Slot.HasValue || InventoryInstanceId.HasValue) && !(Option.HasValue && FactorId == nameof(BloodSpell))) {
            string key = InventoryInstanceId.HasValue ? "INVENTORY_CONTEXT" : Option.HasValue ? "CHANT" : "INITIAL";
            var context = new LocString("frieren", key);
            context.Add("Slot", new LocString("frieren", Slot.HasValue ? $"SLOT.{Slot}" : "UNEQUIPPED").GetFormattedText());
            context.Add("Id", InventoryInstanceId ?? 0);
            context.Add("Cost", Option?.Cost ?? 0);
            contextText = context.GetFormattedText() + "\n";
        }
        description.Add("Context", contextText);
        var effect = new LocString("frieren", (Option.HasValue ? "EFFECT." : "FACTOR.") + FactorId);
        effect.Add("Reward", Option?.Reward ?? 0);
        effect.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
        if (FactorId is nameof(Strategizing) or nameof(NatureBeauty))
            effect.Add(new EnergyVar("Reward", Option?.Reward ?? 0) { ColorPrefix = EnergyIconHelper.GetPrefix(this) });
        if (Option.HasValue && FactorId is nameof(AdvancedDefensiveMagic) or nameof(BarrierMagic)) {
            // 格挡与残影等基础奖励分开；沿用实际咏唱牌的原生修正，包含附魔。
            var block = new BlockVar(Option.Value.Reward, ValueProp.Move);
            if (Owner?.Creature.CombatState is { } combat)
                block.PreviewValue = Hook.ModifyBlock(combat, Owner.Creature, block.BaseValue, ValueProp.Move, _chantSource ?? this, null, out _);
            effect.Add(block);
        }
        effect.Add("Cost", Option?.Cost ?? 0);
        description.Add("Effect", effect.GetFormattedText());
    }
}

using FrierenMod.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Cards;

// 因子仅作为选择与预览模型，不进入普通卡牌奖励池和战斗牌堆。
public abstract class FactorCard() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public string FactorId => GetType().Name;
    public ManaSlot? Slot { get; private set; }
    public ChantOption? Option { get; private set; }
    public int? InventoryInstanceId { get; set; }
    public override int MaxUpgradeLevel => 0;
    protected override bool IsPlayable => false;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("ManaCost", 0), new DynamicVar("Reward", 0)];

    public static FactorCard Create(Player player, string id, ManaSlot? slot = null, ChantOption? option = null)
    {
        _ = SpellCatalog.Get(id);
        var type = typeof(FactorCard).Assembly.GetType(typeof(FactorCard).Namespace + "." + id, throwOnError: true)!;
        var card = (FactorCard)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
        card.Owner = player;
        card.Slot = slot;
        card.Option = option;
        if (option.HasValue)
        {
            card.DynamicVars["ManaCost"].BaseValue = option.Value.Cost;
            card.DynamicVars["Reward"].BaseValue = option.Value.Reward;
        }
        return card;
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        string key = Option.HasValue ? "CHANT" : Slot.HasValue ? "INITIAL" : "COLLECT";
        var context = new LocString("frieren", InventoryInstanceId.HasValue ? "INVENTORY_CONTEXT" : key);
        context.Add("Slot", new LocString("frieren", Slot.HasValue ? $"SLOT.{Slot}" : "UNEQUIPPED").GetFormattedText());
        context.Add("Id", InventoryInstanceId ?? 0);
        context.Add("Cost", Option?.Cost ?? 0);
        description.Add("Context", context.GetFormattedText());
        var effect = new LocString("frieren", (Option.HasValue ? "EFFECT." : "FACTOR.") + FactorId);
        effect.Add("Reward", Option?.Reward ?? 0);
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

public sealed class AdvancedDefensiveMagic : FactorCard;
public sealed class BarrierMagic : FactorCard;
public sealed class BePrepared : FactorCard;
public sealed class BigFire : FactorCard;
public sealed class BloodSpell : FactorCard;
public sealed class BluePrint : FactorCard;
public sealed class BuildUpBody : FactorCard;
public sealed class Factor001 : FactorCard;
public sealed class Factor010 : FactorCard;
public sealed class Factor100 : FactorCard;
public sealed class GreatMageComposure : FactorCard;
public sealed class GrossedOutLook : FactorCard;
public sealed class HighSpeedMovement : FactorCard;
public sealed class Hypnosis : FactorCard;
public sealed class IceArrows : FactorCard;
public sealed class ImpeccableIntangible : FactorCard;
public sealed class MeleeMage : FactorCard;
public sealed class MetallicizFlower : FactorCard;
public sealed class NatureBeauty : FactorCard;
public sealed class PassOnTheTorch : FactorCard;
public sealed class PeacefulDays : FactorCard;
public sealed class PinchAndScrape : FactorCard;
public sealed class Sleeeep : FactorCard;
public sealed class SpellBoost : FactorCard;
public sealed class Strategizing : FactorCard;
public sealed class TheLivingLegend : FactorCard;
public sealed class Transcendence : FactorCard;
public sealed class TransferToAlchemist : FactorCard;

namespace FrierenMod.Core;

// 槽位顺序与一代 currentSlot 一致，不能把三个牌堆的 Mana 合并支付。
public enum ManaSlot { Draw, Hand, Discard }

public sealed record SpellDefinition(string Id, int CostMultiplier = 1, int CostOffset = 0,
    int RewardMultiplier = 1, int RewardOffset = 0);

public readonly record struct ChantOption(ManaSlot Slot, string SpellId, int Intensity, int Cost, int Reward);

public static class ChantRules
{
    public static int Cost(int intensity, SpellDefinition spell, int concentration = 0,
        int weakened = 0, bool free = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(intensity);
        Validate(spell);
        if (spell.Id == "BloodSpell") return 0;
        return free ? 0 : Math.Max(0, checked(intensity * spell.CostMultiplier + spell.CostOffset - concentration + weakened));
    }

    // 一代 PRECISE/FINAL 先按原始系数求 X，再检查减费后的实际费用。
    public static int PreciseIntensity(int mana, SpellDefinition spell)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mana);
        Validate(spell);
        // 驭血魔法只接受普通咏唱指定的 X，自动咏唱不能以生命换魔力。
        if (spell.Id == "BloodSpell") return 0;
        return Math.Max(0, (mana - spell.CostOffset) / spell.CostMultiplier);
    }

    public static ChantOption? Option(ManaSlot slot, int mana, SpellDefinition spell,
        int intensity, int concentration = 0, int weakened = 0, bool free = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mana);
        var preview = Preview(slot, spell, intensity, concentration, weakened, free);
        return intensity > 0 && mana >= preview.Cost ? preview : null;
    }

    // 预览仍显示不可支付的费用与效果；可用性单独判断，不隐藏对应槽位。
    public static ChantOption Preview(ManaSlot slot, SpellDefinition spell, int intensity,
        int concentration = 0, int weakened = 0, bool free = false)
    {
        if (!Enum.IsDefined(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        int cost = Cost(intensity, spell, concentration, weakened, free);
        return new(slot, spell.Id, intensity, cost, checked(intensity * spell.RewardMultiplier + spell.RewardOffset));
    }

    private static void Validate(SpellDefinition spell)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spell.CostMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(spell.RewardMultiplier);
        ArgumentOutOfRangeException.ThrowIfNegative(spell.RewardOffset);
    }
}

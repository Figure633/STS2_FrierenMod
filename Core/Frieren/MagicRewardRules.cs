namespace FrierenMod.Core;

public enum MagicRewardRarity { Common, Uncommon, Rare, Prop }
public sealed record MagicRewardOffer(string[] Choices, int RarityOffset);

public static class MagicRewardRules
{
    public const int StartingOffset = 5;
    public const int MinimumOffset = -40;
    public const int CommonStep = 20;

    public static bool ShouldDrop(int roll, int pity, bool elite, bool escaped)
    {
        if (roll is < 0 or >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
        return (elite || !escaped) && roll < (elite ? 70 : 40) + pity;
    }

    public static MagicRewardRarity Rarity(int roll, int offset)
    {
        if (roll is < 0 or >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
        return (roll + offset) switch {
            < 3 => MagicRewardRarity.Rare,
            < 20 => MagicRewardRarity.Uncommon,
            < 70 => MagicRewardRarity.Prop,
            _ => MagicRewardRarity.Common
        };
    }

    public static int AfterOffer(int offset, MagicRewardRarity rarity) => rarity switch {
        MagicRewardRarity.Common => Math.Max(MinimumOffset, offset - CommonStep),
        MagicRewardRarity.Rare => StartingOffset,
        _ => offset
    };

    public static MagicRewardOffer Offer(int offset, Func<int, int> next)
    {
        var factors = SpellCatalog.All.GroupBy(f => f.Rarity).ToDictionary(g => g.Key, g => g.Select(f => f.Id).ToList());
        var props = MagicPropRules.All.Select(p => p.Id).ToList();
        var choices = new List<string>();
        for (int i = 0; i < 3; i++) {
            var rarity = Rarity(next(100), offset);
            var pool = rarity switch {
                MagicRewardRarity.Rare => factors[FactorRarity.Rare],
                MagicRewardRarity.Uncommon => factors[FactorRarity.Uncommon],
                MagicRewardRarity.Common => factors[FactorRarity.Common],
                _ => props
            };
            int index = next(pool.Count);
            choices.Add(pool[index]); pool.RemoveAt(index);
            offset = AfterOffer(offset, rarity);
        }
        return new([.. choices], offset);
    }
}

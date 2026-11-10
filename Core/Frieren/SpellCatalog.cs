namespace FrierenMod.Core;

public enum FactorRarity { Common, Uncommon, Rare }

public sealed record FactorDefinition(SpellDefinition Rules, FactorRarity Rarity, bool Puzzle = false)
{
    public string Id => Rules.Id;
}

// 系数逐项来自一代 factors/*.java；血魔法的费用偏移允许为负。
public static class SpellCatalog
{
    public static IReadOnlyList<FactorDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        F("AdvancedDefensiveMagic"), F("BePrepared", rewardOffset: 3), F("BuildUpBody"),
        F("GrossedOutLook", rewardOffset: 1), F("IceArrows", reward: 4),
        F("MeleeMage", rewardOffset: 1), F("MetalPetalSpell"), F("Sleeeep"),
        F("TransferToAlchemist", reward: 2), F("Factor100", offset: 1, puzzle: true),
        F("BarrierMagic", FactorRarity.Uncommon, offset: 1),
        F("BigFire", FactorRarity.Uncommon, cost: 2),
        F("BlackGoldWings", FactorRarity.Uncommon, cost: 2, reward: 6),
        F("CopyMagic", FactorRarity.Uncommon, cost: 2),
        F("BloodSpell", FactorRarity.Rare),
        F("BluePrint", FactorRarity.Uncommon), F("HighSpeedMovement", FactorRarity.Uncommon),
        F("Hypnosis", FactorRarity.Uncommon, cost: 2),
        F("NatureBeauty", FactorRarity.Uncommon, offset: 1),
        F("PassOnTheTorch", FactorRarity.Uncommon),
        F("PinchAndScrape", FactorRarity.Uncommon, reward: 2),
        F("SpellBoost", FactorRarity.Uncommon, cost: 4),
        F("Factor010", FactorRarity.Uncommon, cost: 2, offset: 2, puzzle: true),
        F("GreatMageComposure", FactorRarity.Rare, cost: 4),
        F("ImpeccableIntangible", FactorRarity.Rare, cost: 6),
        F("PeacefulDays", FactorRarity.Rare, offset: 1),
        F("Strategizing", FactorRarity.Rare, cost: 2),
        F("TheLivingLegend", FactorRarity.Rare), F("Transcendence", FactorRarity.Rare),
        F("Factor001", FactorRarity.Rare, cost: 3, offset: 3, puzzle: true)
    });

    private static FactorDefinition F(string id, FactorRarity rarity = FactorRarity.Common,
        int cost = 1, int offset = 0, int reward = 1, int rewardOffset = 0, bool puzzle = false) =>
        new(new(id, cost, offset, reward, rewardOffset), rarity, puzzle);

    public static FactorDefinition Get(string id) => All.FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException($"未知魔法因子：{id}", nameof(id));

    // 旧档仍可解析弃用因子，新的抽选与变换统一使用可获取目录。
    public static IEnumerable<FactorDefinition> Available => All.Where(x => x.Id != "TransferToAlchemist");

    public static string[] Offer(FactorRarity rarity, Func<int, int> next, bool starting = false)
    {
        var pool = Available.Where(x => x.Rarity == rarity && (!starting || !x.Puzzle)).ToList();
        var result = new List<string>();
        while (result.Count < 3 && pool.Count > 0)
        {
            int index = next(pool.Count);
            result.Add(pool[index].Id);
            pool.RemoveAt(index);
        }
        return [.. result];
    }
}

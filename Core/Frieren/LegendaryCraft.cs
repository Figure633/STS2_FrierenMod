namespace FrierenMod.Core;

public readonly record struct LegendaryEffect(string Kind, int Amount);

// 一代 CustomCardLayer2/3/4Action 的候选表；每层随机展示两个，升级后三个。
public static class LegendaryCraft
{
    public static IReadOnlyList<LegendaryEffect> Options(int cost, int layer) => (cost, layer) switch
    {
        (0, 0) => [new("Chant",2), new("ManaDiscard",2), new("ManaDraw",2), new("Scry",2)],
        (1, 0) => [new("Chant",3), new("ManaDiscard",4), new("ManaHand",3), new("ManaDraw",4), new("Scry",3)],
        (2, 0) => [new("Chant",4), new("ManaDiscard",6), new("RandomCard",1), new("Retain",1), new("ManaHand",4), new("Scry",4), new("ZeroCost",1)],
        (3, 0) => [new("ManaDiscard",8), new("RandomCard",2), new("Retain",2), new("ManaHand",5), new("ZeroCost",2)],
        (0, 1) => [new("Block",4), new("Damage",6), new("DamageAll",4)],
        (1, 1) => [new("Block",8), new("Damage",10), new("Energy",1), new("DamageAll",8)],
        (2, 1) => [new("Block",12), new("Energy",2), new("DamageTriple",5), new("DamageAll",12), new("Execute",30)],
        (3, 1) => [new("Block",16), new("Energy",3), new("DamageTriple",7), new("DamageAll",16), new("Execute",40)],
        (0, 2) => [new("Draw",1), new("Dexterity",2), new("Vulnerable",1)],
        (1, 2) => [new("Draw",2), new("Dexterity",3), new("Vulnerable",2)],
        (2, 2) => [new("Draw",3), new("Dexterity",4), new("Vulnerable",3), new("Intangible",1), new("UpgradeHand",1)],
        (3, 2) => [new("Draw",5), new("Intangible",2), new("DoubleStrength",1), new("RemoveDebuffs",1), new("UpgradeAll",1)],
        _ => throw new ArgumentOutOfRangeException()
    };
}

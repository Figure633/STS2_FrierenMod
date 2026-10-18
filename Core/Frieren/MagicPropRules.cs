namespace FrierenMod.Core;

public sealed record PropDefinition(string Id, int Targets, bool CombatOnly = false);
public sealed record PropResult(MagicDeckState Deck, int Heal = 0, int Energy = 0);

// 一代八种可获得魔具。Tutorial 标记 NEVER_DROP，不进入营地选择池。
public static class MagicPropRules
{
    public static IReadOnlyList<PropDefinition> All { get; } = Array.AsReadOnly(new[] {
        new PropDefinition("CrystalBall", 2), new("MysteryBox", 0), new("MysticBox", 3),
        new("SeinNote", 2), new("ShadowDragonHorn", 2, true), new("TransmutationCircle", 2),
        new("UnbelievableTool", 2), new("UnbelievableToolBox", 2)
    });
    public static PropDefinition Get(string id) => All.SingleOrDefault(p => p.Id == id)
        ?? throw new ArgumentException("未知魔具：" + id);

    public static int[] Eligible(MagicDeckState deck, MagicProp prop)
    {
        var factors = deck.Items.Where(i => prop.PropId switch {
            "CrystalBall" => i.Slot >= 0,
            "MysticBox" or "SeinNote" or "ShadowDragonHorn" or "UnbelievableToolBox" => i.Slot < 0,
            "MysteryBox" => false,
            _ => true
        }).Select(i => i.InstanceId);
        return prop.PropId is "SeinNote" or "ShadowDragonHorn" or "UnbelievableToolBox"
            ? factors.Concat(deck.Props.Where(p => p.InstanceId != prop.InstanceId).Select(p => p.InstanceId)).ToArray()
            : factors.ToArray();
    }

    public static bool CanUse(MagicDeckState deck, MagicProp prop, bool inCombat)
    {
        var rule = Get(prop.PropId);
        if (rule.CombatOnly && !inCombat) return false;
        if (prop.PropId == "UnbelievableTool")
            return deck.Items.Any(i => i.Slot >= 0) && deck.Items.Any(i => i.Slot < 0);
        return Eligible(deck, prop).Length >= rule.Targets;
    }

    public static PropResult? Apply(MagicDeckState original, int propId, IReadOnlyList<int> targets,
        bool inCombat, Func<int, int> next)
    {
        var prop = original.Props.SingleOrDefault(p => p.InstanceId == propId);
        if (prop == null || !CanUse(original, prop, inCombat) || targets.Count != Get(prop.PropId).Targets ||
            targets.Distinct().Count() != targets.Count || targets.Except(Eligible(original, prop)).Any()) return null;
        var chosen = original.Items.Where(i => targets.Contains(i.InstanceId)).ToArray();
        if (prop.PropId == "UnbelievableTool" && chosen.Count(i => i.Slot >= 0) != 1) return null;
        // 在副本上结算；取消或不合法选择不会消耗魔具、目标或随机数。
        var deck = MagicDeckState.FromJson(original.ToJson());
        string RandomFactor(FactorRarity? rarity = null)
        {
            var pool = SpellCatalog.All.Where(f => !f.Puzzle && (rarity == null || f.Rarity == rarity)).ToArray();
            return pool[next(pool.Length)].Id;
        }
        int heal = 0, energy = 0;
        switch (prop.PropId)
        {
            case "CrystalBall":
                deck.Equip(chosen[0].InstanceId, (ManaSlot)chosen[1].Slot);
                deck.Equip(chosen[1].InstanceId, (ManaSlot)chosen[0].Slot);
                break;
            case "UnbelievableTool":
                var equipped = chosen.Single(i => i.Slot >= 0);
                deck.Equip(chosen.Single(i => i.Slot < 0).InstanceId, (ManaSlot)equipped.Slot);
                break;
            case "MysteryBox":
                for (int i = 0; i < 2; i++) deck.Add(RandomFactor());
                break;
            case "MysticBox":
                var slots = new List<ManaSlot> { ManaSlot.Draw, ManaSlot.Hand, ManaSlot.Discard };
                foreach (var item in chosen) {
                    int index = next(slots.Count);
                    deck.Equip(item.InstanceId, slots[index]); slots.RemoveAt(index);
                }
                break;
            case "TransmutationCircle":
                foreach (var item in chosen) {
                    deck.Remove(item.InstanceId);
                    var replacement = deck.Add(RandomFactor(SpellCatalog.Get(item.SpellId).Rarity));
                    if (item.Slot >= 0) deck.Equip(replacement.InstanceId, (ManaSlot)item.Slot);
                }
                break;
            default:
                foreach (int id in targets) deck.Remove(id);
                if (prop.PropId == "SeinNote") heal = 10;
                if (prop.PropId == "ShadowDragonHorn") energy = 2;
                if (prop.PropId == "UnbelievableToolBox") {
                    deck.AddProp("UnbelievableTool"); deck.AddProp("UnbelievableTool");
                }
                break;
        }
        deck.Remove(prop.InstanceId);
        return new(deck, heal, energy);
    }
}

namespace FrierenMod.Core;

public sealed record PropDefinition(string Id, int Targets, bool CombatOnly = false, bool Reusable = false, int GoldCost = 0);
public sealed record PropResult(MagicDeckState Deck, int Heal = 0, int Energy = 0, int Draw = 0, bool BirdSpell = false, bool Escape = false);

// 一代八种可获得魔具。Tutorial 标记 NEVER_DROP，不进入营地选择池。
public static class MagicPropRules
{
    public static IReadOnlyList<PropDefinition> All { get; } = Array.AsReadOnly(new[] {
        new PropDefinition("CrystalBall", 2), new("MysteryBox", 0),
        new("SeinNote", 2), new("ShadowDragonHorn", 2, true), new("TransmutationCircle", 2),
        new("UnbelievableTool", 2), new("UnbelievableToolBox", 2)
    });
    public static IReadOnlyList<PropDefinition> CommissionOnly { get; } = Array.AsReadOnly(new[] {
        new PropDefinition("RoyalGrimoire",0), new("MagicExamVial",0,true), new("MagicPot",0,Reusable:true,GoldCost:30),new("MagicBread",0,true)
    });
    public static IReadOnlyList<PropDefinition> EventOnly { get; } = Array.AsReadOnly(new[] {
        new PropDefinition("SealingOre", 0), new("BirdCapturingGrimoire", 0, true),
        new("DragonBall1",0),new("DragonBall2",0),new("DragonBall3",0),new("DragonBall4",0),
        new("DragonBall5",0),new("DragonBall6",0),new("DragonBall7",0)
    });
    // 兼容旧存档，已弃用的魔具不再进入任何新获取池。
    public static IReadOnlyList<PropDefinition> Legacy { get; } = [new("MysticBox",3),new("ElvenHourglass",0,true),new("PhoenixQuill",1)];
    public static PropDefinition Get(string id) => All.Concat(CommissionOnly).Concat(EventOnly).Concat(Legacy).SingleOrDefault(p => p.Id == id)
        ?? throw new ArgumentException("未知魔具：" + id);

    public static int[] Eligible(MagicDeckState deck, MagicProp prop)
    {
        var factors = deck.Items.Where(i => prop.PropId switch {
            "CrystalBall" => i.Slot >= 0,
            "MysticBox" or "SeinNote" or "ShadowDragonHorn" or "UnbelievableToolBox" => i.Slot < 0,
            "MysteryBox" or "RoyalGrimoire" or "ElvenHourglass" or "BirdCapturingGrimoire" or "MagicExamVial" or "MagicPot" or "MagicBread" => false,
            _ => true
        }).Select(i => i.InstanceId);
        return prop.PropId is "SeinNote" or "ShadowDragonHorn" or "UnbelievableToolBox"
            ? factors.Concat(deck.Props.Where(p => p.InstanceId != prop.InstanceId).Select(p => p.InstanceId)).ToArray()
            : factors.ToArray();
    }

    public static bool CanUse(MagicDeckState deck, MagicProp prop, bool inCombat)
    {
        var rule = Get(prop.PropId);
        if(prop.PropId=="MysticBox" || prop.PropId.StartsWith("DragonBall",StringComparison.Ordinal))return false;
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
            var pool = SpellCatalog.Available.Where(f => !f.Puzzle && (rarity == null || f.Rarity == rarity)).ToArray();
            return pool[next(pool.Length)].Id;
        }
        int heal = 0, energy = 0, draw = 0;
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
            case "RoyalGrimoire":
                // 具体因子由原生同步选牌流程选择，纯规则层只负责消费魔具。
                break;
            case "BirdCapturingGrimoire":
                // 战斗卡牌通过运行时原生生成命令添加，纯规则层只消费魔具并返回效果。
                break;
            case "MagicExamVial": break;
            case "MagicPot": deck.AddProp(All[next(All.Count)].Id);break;
            case "MagicBread": draw=2;energy=2;break;
            case "ElvenHourglass": energy=3;draw=3;break;
            case "PhoenixQuill":
                var source=chosen.Single();deck.Remove(source.InstanceId);
                var reborn=deck.Add(RandomFactor(FactorRarity.Rare));
                if(source.Slot>=0)deck.Equip(reborn.InstanceId,(ManaSlot)source.Slot);
                deck.AddProp("UnbelievableTool");deck.AddProp("UnbelievableTool");
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
        if(!Get(prop.PropId).Reusable)deck.Remove(prop.InstanceId);
        return new(deck, heal, energy, draw, prop.PropId == "BirdCapturingGrimoire",prop.PropId=="MagicExamVial");
    }
}

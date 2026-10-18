using FrierenMod.Core;
using System.Text.Json;

static class MagicPropTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool value) { if (!value) throw new Exception("魔具规则回归失败"); }
        void Test(string name, Action action) { action(); count++; Console.WriteLine("PASS " + name); }
        MagicDeckState Deck() {
            var deck = MagicDeckState.CreatePrototypeLoadout();
            deck.Add("BePrepared"); deck.Add("IceArrows"); deck.Add("BluePrint"); return deck;
        }
        PropResult Use(MagicDeckState deck, string name, int[] ids, bool combat = false) =>
            MagicPropRules.Apply(deck, deck.AddProp(name).InstanceId, ids, combat, _ => 0)!;
        Test("水晶球交换两个已装载槽位，消耗自身", () => {
            var result = Use(Deck(), "CrystalBall", [1, 2]);
            Check(result.Deck.Items.Single(i => i.InstanceId == 1).Slot == 1 && result.Deck.Items.Single(i => i.InstanceId == 2).Slot == 0 && result.Deck.Props.Count == 0);
        });
        Test("妙妙工具用库存替换装载，原因子保留", () => {
            var result = Use(Deck(), "UnbelievableTool", [1, 4]);
            Check(result.Deck.Items.Single(i => i.InstanceId == 1).Slot == -1 && result.Deck.Items.Single(i => i.InstanceId == 4).Slot == 0 && result.Deck.Items.Count == 6);
        });
        Test("盲盒生成两张非谜题因子", () => {
            var result = Use(Deck(), "MysteryBox", []);
            Check(result.Deck.Items.Count == 8 && result.Deck.Items.TakeLast(2).All(i => i.Slot == -1 && !SpellCatalog.Get(i.SpellId).Puzzle));
        });
        Test("神秘箱随机装入三个新因子并保留旧库存", () => {
            var result = Use(Deck(), "MysticBox", [4, 5, 6]);
            Check(result.Deck.Items.Take(3).All(i => i.Slot == -1) && result.Deck.Items.Skip(3).Select(i => i.Slot).Order().SequenceEqual(new[] {0, 1, 2}));
        });
        Test("炼成阵保留稀有度与槽位并分配新编号", () => {
            var deck = Deck(); var result = Use(deck, "TransmutationCircle", [1, 3]);
            Check(result.Deck.Items.All(i => i.InstanceId != 1 && i.InstanceId != 3));
            Check(SpellCatalog.Get(result.Deck.InSlot(ManaSlot.Draw)!.Id).Rarity == FactorRarity.Common && SpellCatalog.Get(result.Deck.InSlot(ManaSlot.Discard)!.Id).Rarity == FactorRarity.Uncommon);
        });
        Test("赞因笔记可消耗库存因子和其他魔具以治疗", () => {
            var deck = Deck(); int material = deck.AddProp("MysteryBox").InstanceId;
            var result = Use(deck, "SeinNote", [4, material]);
            Check(result.Heal == 10 && result.Deck.Items.Count == 5 && result.Deck.Props.Count == 0);
        });
        Test("龙角战斗内换能量，非战斗拒绝", () => {
            var deck = Deck(); int id = deck.AddProp("ShadowDragonHorn").InstanceId;
            Check(MagicPropRules.Apply(deck, id, [4, 5], false, _ => throw new Exception()) == null);
            var result = MagicPropRules.Apply(deck, id, [4, 5], true, _ => 0)!;
            Check(result.Energy == 2 && result.Deck.Items.Count == 4 && result.Deck.Props.Count == 0);
        });
        Test("工具箱消耗两件材料并获得两个独立工具实例", () => {
            var result = Use(Deck(), "UnbelievableToolBox", [4, 5]);
            Check(result.Deck.Props.Count == 2 && result.Deck.Props.All(p => p.PropId == "UnbelievableTool") && result.Deck.Props.Select(p => p.InstanceId).Distinct().Count() == 2);
        });
        Test("非法重复、自身、装载材料选择不消耗也不推进随机数", () => {
            var deck = Deck(); int id = deck.AddProp("SeinNote").InstanceId; string before = deck.ToJson();
            foreach (int[] targets in new[] { new[] {4, 4}, new[] {4, id}, new[] {1, 4}, Array.Empty<int>() })
                Check(MagicPropRules.Apply(deck, id, targets, false, _ => throw new Exception()) == null);
            Check(before == deck.ToJson());
        });
        Test("妙妙工具拒绝选择两个已装载因子", () => {
            var deck = Deck(); int id = deck.AddProp("UnbelievableTool").InstanceId;
            Check(MagicPropRules.Apply(deck, id, [1, 2], false, _ => throw new Exception()) == null);
        });
        Test("魔具 v3 存档往返与编号不复用", () => {
            var deck = Deck(); int used = deck.AddProp("MysteryBox").InstanceId; deck.Remove(used);
            var restored = MagicDeckState.FromJson(deck.ToJson());
            Check(restored.AddProp("MysteryBox").InstanceId > used);
            Check(MagicDeckState.FromJson(restored.ToJson()).ToJson() == restored.ToJson());
        });
        Test("v2 旧存档载入后保留因子并可新增魔具", () => {
            var old = new MagicDeckSave(2, [new(12, "BuildUpBody", 1)]);
            var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(old));
            Check(deck.AddProp("MysteryBox").InstanceId == 13 && deck.InSlot(ManaSlot.Hand)!.Id == "BuildUpBody");
        });
        Test("因子与魔具跨类型重复编号拒绝", () => {
            try { MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3, [new(1, "BuildUpBody")], Props: [new(1, "MysteryBox")]))); }
            catch (JsonException) { return; }
            throw new Exception("重复编号未拒绝");
        });
        return count;
    }
}

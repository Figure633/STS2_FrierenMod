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
        Test("妙妙工具交换已装载和未装载因子的槽位", () => {
            var result = Use(Deck(), "UnbelievableTool", [1, 4]);
            Check(result.Deck.Items.Single(i => i.InstanceId == 1).Slot == -1 && result.Deck.Items.Single(i => i.InstanceId == 4).Slot == 0 && result.Deck.Items.Count == 6);
        });
        Test("妙妙工具交换覆盖三个槽位和两种选择顺序，保留原实例", () => {
            foreach (var slot in Enum.GetValues<ManaSlot>()) foreach (bool reverse in new[] { false, true }) {
                var deck = Deck(); var loaded = deck.Items.Single(i => i.Slot == (int)slot);
                var held = deck.Items.Single(i => i.InstanceId == 4);
                int tool = deck.AddProp("UnbelievableTool").InstanceId; string before = deck.ToJson();
                var result = MagicPropRules.Apply(deck, tool,
                    reverse ? [held.InstanceId, loaded.InstanceId] : [loaded.InstanceId, held.InstanceId], false,
                    _ => throw new Exception("槽位交换不应使用随机数"))!;
                Check(result.Deck.Items.Single(i => i.InstanceId == loaded.InstanceId).Slot == -1 &&
                    result.Deck.Items.Single(i => i.InstanceId == held.InstanceId).Slot == (int)slot &&
                    result.Deck.Items.Select(i => i.InstanceId).SequenceEqual(deck.Items.Select(i => i.InstanceId)) &&
                    result.Deck.Props.Count == 0 && deck.ToJson() == before);
            }
        });
        Test("盲盒生成两张非谜题因子", () => {
            var result = Use(Deck(), "MysteryBox", []);
            Check(result.Deck.Items.Count == 8 && result.Deck.Items.TakeLast(2).All(i => i.Slot == -1 && !SpellCatalog.Get(i.SpellId).Puzzle));
        });
        Test("魔术箱弃用，旧存档仍可读取但不能使用", () => {
            var deck=Deck();int id=deck.AddProp("MysticBox").InstanceId;
            Check(MagicDeckState.FromJson(deck.ToJson()).Props.Any(p=>p.PropId=="MysticBox")&&MagicPropRules.Apply(deck,id,[4,5,6],false,_=>throw new Exception())==null);
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
        Test("委托独占魔具与弃用魔具不进入七种普通魔具池",()=>{
            Check(MagicPropRules.All.Count==7&&MagicPropRules.CommissionOnly.Count==3&&MagicPropRules.CommissionOnly.All(p=>!MagicPropRules.All.Contains(p))&&!MagicPropRules.All.Any(p=>p.Id=="MysticBox"));
        });
        Test("魔法陶罐保留自身，每次生成一个普通魔具，不能生成陶罐或弃用魔具",()=>{
            var deck=Deck();int id=deck.AddProp("MagicPot").InstanceId;
            for(int i=0;i<MagicPropRules.All.Count;i++){
                var result=MagicPropRules.Apply(deck,id,[],false,_=>i)!;
                Check(result.Deck.Props.Count==2&&result.Deck.Props.Any(p=>p.InstanceId==id)&&result.Deck.Props.Any(p=>p.PropId==MagicPropRules.All[i].Id));
            }
        });
        Test("考试小瓶仅战斗可用并消费自身，返回逃跑标记",()=>{
            var deck=Deck();int id=deck.AddProp("MagicExamVial").InstanceId;
            Check(MagicPropRules.Apply(deck,id,[],false,_=>0)==null);
            var result=MagicPropRules.Apply(deck,id,[],true,_=>0)!;
            Check(result.Escape&&result.Deck.Props.Count==0);
        });
        Test("王室秘典不随机生成或覆盖槽位，留给同步选牌流程",()=>{
            var deck=Deck();int old=deck.Items.Count;var result=Use(deck,"RoyalGrimoire",[]);
            Check(result.Deck.Items.Count==old&&result.Deck.Items.SequenceEqual(deck.Items)&&result.Deck.Props.All(p=>p.PropId!="RoyalGrimoire"));
        });
        Test("时砂漏只在战斗中给予3能量与3抽牌",()=>{
            var deck=Deck();int id=deck.AddProp("ElvenHourglass").InstanceId;
            Check(MagicPropRules.Apply(deck,id,[],false,_=>0)==null);
            var result=MagicPropRules.Apply(deck,id,[],true,_=>0)!;Check(result.Energy==3&&result.Draw==3&&result.Deck.Props.Count==0);
        });
        Test("羽笔保持槽位换成稀有因子并给两个工具",()=>{
            var deck=Deck();var result=Use(deck,"PhoenixQuill",[1]);
            Check(result.Deck.InSlot(ManaSlot.Draw)!=null&&SpellCatalog.Get(result.Deck.InSlot(ManaSlot.Draw)!.Id).Rarity==FactorRarity.Rare&&result.Deck.Props.Count(p=>p.PropId=="UnbelievableTool")==2);
            Check(MagicDeckState.FromJson(result.Deck.ToJson()).ToJson()==result.Deck.ToJson());
        });
        return count;
    }
}

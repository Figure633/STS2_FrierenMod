using System.Text.Json;
using FrierenMod.Core;

int passed = 0;
var basic = new SpellDefinition("test");
void Test(string name, Action test)
{
    test(); passed++; Console.WriteLine($"PASS {name}");
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}");
}
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

Test("基础费用", () => Equal(3, ChantRules.Cost(3, basic)));
Test("费用系数与偏移", () => Equal(8, ChantRules.Cost(3, new("test", 2, 2))));
Test("专注减费与削弱", () => Equal(2, ChantRules.Cost(3, basic, 2, 1)));
Test("费用下限为零", () => Equal(0, ChantRules.Cost(1, basic, 5)));
Test("免 Mana", () => Equal(0, ChantRules.Cost(4, basic, free: true)));
Test("精确咏唱向下取整", () => Equal(3, ChantRules.PreciseIntensity(9, new("test", 2, 2))));
Test("偏移大于 Mana", () => Equal(0, ChantRules.PreciseIntensity(1, new("test", 2, 3))));
Test("资源不足无选项", () => Equal<ChantOption?>(null, ChantRules.Option(ManaSlot.Hand, 1, basic, 2)));
Test("零强度不生成精确选项", () => Equal<ChantOption?>(null, ChantRules.Option(ManaSlot.Hand, 5, basic, 0)));
Test("独立牌堆费用", () => {
    Equal<ChantOption?>(null, ChantRules.Option(ManaSlot.Hand, 0, basic, 1));
    Equal(1, ChantRules.Option(ManaSlot.Draw, 1, basic, 1)!.Value.Cost);
});
Test("效果系数独立于费用", () => Equal(11, ChantRules.Option(ManaSlot.Draw, 8, new("test", 2, 0, 3, 2), 3)!.Value.Reward));
Test("负强度被拒绝", () => Throws<ArgumentOutOfRangeException>(() => ChantRules.Cost(-1, basic)));
Test("零除数被拒绝", () => Throws<ArgumentOutOfRangeException>(() => ChantRules.PreciseIntensity(5, new("test", 0))));
Test("负 Mana 被拒绝", () => Throws<ArgumentOutOfRangeException>(() => ChantRules.Option(ManaSlot.Draw, -1, basic, 1)));
Test("整数溢出被拒绝", () => Throws<OverflowException>(() => ChantRules.Cost(int.MaxValue, new("test", 2))));
Test("非法槽位被拒绝", () => Throws<ArgumentOutOfRangeException>(() => ChantRules.Option((ManaSlot)3, 1, basic, 1)));
Test("空 Magic Deck", () => Equal<SpellDefinition?>(null, new MagicDeckState().InSlot(ManaSlot.Draw)));
Test("默认三槽与来源因子", () => {
    var deck = MagicDeckState.CreatePrototypeLoadout();
    Equal("AdvancedDefensiveMagic", deck.InSlot(ManaSlot.Draw)!.Id);
    Equal("BuildUpBody", deck.InSlot(ManaSlot.Hand)!.Id);
    Equal("HighSpeedMovement", deck.InSlot(ManaSlot.Discard)!.Id);
});
Test("同名因子保留独立实例", () => {
    var deck = new MagicDeckState();
    Equal(1, deck.Add("BuildUpBody").InstanceId);
    Equal(2, deck.Add("BuildUpBody").InstanceId);
});
Test("装载解除原占位且移动不复制", () => {
    var deck = MagicDeckState.CreatePrototypeLoadout();
    deck.Equip(1, ManaSlot.Hand);
    Equal<SpellDefinition?>(null, deck.InSlot(ManaSlot.Draw));
    Equal("AdvancedDefensiveMagic", deck.InSlot(ManaSlot.Hand)!.Id);
    Equal(-1, deck.Items.Single(x => x.InstanceId == 2).Slot);
    Equal(3, deck.Items.Count);
});
Test("装载不存在实例无副作用", () => {
    var deck = MagicDeckState.CreatePrototypeLoadout(); var before = deck.ToJson();
    Throws<ArgumentException>(() => deck.Equip(999, ManaSlot.Draw)); Equal(before, deck.ToJson());
});
Test("JSON 往返保持 ID 与槽位", () => {
    var deck = MagicDeckState.CreatePrototypeLoadout(); deck.Add("BuildUpBody");
    Equal(deck.ToJson(), MagicDeckState.FromJson(deck.ToJson()).ToJson());
});
Test("玩家和存档快照隔离", () => {
    var a = MagicDeckState.CreatePrototypeLoadout(); var saved = a.ToJson(); var b = MagicDeckState.FromJson(saved);
    b.Equip(1, ManaSlot.Discard); b.Add("BuildUpBody"); Equal(saved, a.ToJson()); Equal(4, b.Items.Count);
});
Test("未来存档版本拒绝", () => Throws<JsonException>(() => MagicDeckState.FromJson("{\"Version\":4,\"Items\":[]}")));
Test("重复实例拒绝", () => Throws<JsonException>(() => MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(1, [new(1, "BuildUpBody"), new(1, "BuildUpBody")])))));
Test("重复槽位拒绝", () => Throws<JsonException>(() => MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(1, [new(1, "BuildUpBody", 0), new(2, "BuildUpBody", 0)])))));
Test("未知因子拒绝", () => Throws<ArgumentException>(() => new MagicDeckState().Add("unknown")));
Test("空条目拒绝", () => Throws<JsonException>(() => MagicDeckState.FromJson("{\"Version\":1,\"Items\":[null]}")));
Test("因子目录完整且无重名", () => { Equal(28, SpellCatalog.All.Count); Equal(28, SpellCatalog.All.Select(x => x.Id).Distinct().Count()); });
Test("初始三次三选一完整装载", () => {
    var deck = new MagicDeckState();
    for (int i = 0; i < 3; i++) {
        deck.PrepareDraft(_ => 0); Equal(3, deck.Draft.Length); Equal(3, deck.Draft.Distinct().Count());
        Equal(false, deck.Draft.Any(x => SpellCatalog.Get(x).Puzzle));
        string selected = deck.Draft[i]; deck.SelectInitial(selected); Equal(selected, deck.InSlot((ManaSlot)i)!.Id);
    }
    Equal(true, deck.SetupCompleted); Equal(3, deck.Items.Count);
    Throws<InvalidOperationException>(() => deck.SelectInitial("BuildUpBody"));
});
Test("中断初始选择后恢复相同选项和进度", () => {
    var deck = new MagicDeckState(); deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); deck.PrepareDraft(n => n - 1);
    var restored = MagicDeckState.FromJson(deck.ToJson()); restored.PrepareDraft(_ => throw new Exception("不应重掷"));
    Equal(deck.ToJson(), restored.ToJson()); Equal(1, restored.DraftSlot);
});
Test("旧版本存档保留库存且补初始选择", () => {
    var old = new MagicDeckSave(1, [new(1, "BuildUpBody", 1)]);
    var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(old));
    Equal(false, deck.SetupCompleted); Equal("BuildUpBody", deck.InSlot(ManaSlot.Hand)!.Id);
});
Test("蓝图连续与循环解析", () => {
    var deck = new MagicDeckState(); deck.Equip(deck.Add("BluePrint").InstanceId, ManaSlot.Draw);
    deck.Equip(deck.Add("BluePrint").InstanceId, ManaSlot.Hand);
    deck.Equip(deck.Add("BuildUpBody").InstanceId, ManaSlot.Discard);
    Equal("BuildUpBody", deck.ResolveSlot(ManaSlot.Draw)!.Id);
    deck.Equip(deck.Add("BluePrint").InstanceId, ManaSlot.Discard); Equal<SpellDefinition?>(null, deck.ResolveSlot(ManaSlot.Draw));
    deck.Unequip(2); Equal<SpellDefinition?>(null, deck.ResolveSlot(ManaSlot.Draw));
});
Test("血魔法负偏移与精准强度", () => {
    var spell = MagicDeckState.Definition("BloodSpell"); Equal(0, ChantRules.Cost(1, spell));
    Equal(2, ChantRules.PreciseIntensity(0, spell)); Equal(4, ChantRules.Option(ManaSlot.Draw, 0, spell, 2)!.Value.Reward);
});
Test("正常咏唱免耗允许零魔力", () => Equal(0, ChantRules.Option(ManaSlot.Draw, 0, basic, 3, free: true)!.Value.Cost));
Test("精准咏唱先按原系数算强度再减费", () => {
    var spell = MagicDeckState.Definition("GreatMageComposure"); int intensity = ChantRules.PreciseIntensity(8, spell);
    Equal(2, intensity); Equal(5, ChantRules.Option(ManaSlot.Draw, 8, spell, intensity, concentration: 3)!.Value.Cost);
});
passed += MagicPropTests.Run();
Console.WriteLine($"{passed} tests passed.");

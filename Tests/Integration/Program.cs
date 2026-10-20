using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using BaseLib.Extensions;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;

// 仅独立测试进程使用实际游戏的模型/存档 API，不读写玩家存档，不启动游戏。
int passed = 0;
MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn = true;
foreach (var type in typeof(AbstractModel).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null))
    ModelDb.Inject(type);
foreach (var type in typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t)))
    ModelDb.Inject(type);
void Test(string name, Action action) { action(); passed++; Console.WriteLine($"PASS {name}"); }
void Equal<T>(T a, T b) { if (!Equals(a, b)) throw new Exception($"Expected {a}; actual {b}"); }

Test("真实程序集可实例化全部卡模型", () => {
    var types = typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)).ToArray();
    Equal(true, types.Length > 50);
    foreach (var type in types) _ = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
});
Test("圣徽可变副本不共享 Magic Deck", () => {
    var a = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); var b = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable();
    var deck = a.ReadMagicDeck(); deck.Equip(deck.Add("AdvancedDefensiveMagic").InstanceId, ManaSlot.Hand); a.SaveMagicDeck(deck);
    Equal(0, b.ReadMagicDeck().Items.Count);
    Equal<SpellDefinition?>(null, a.ReadMagicDeck().InSlot(ManaSlot.Draw));
});
Test("原生 SavedProperties JSON 属性载入", () => {
    var a = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); var deck = a.ReadMagicDeck(); deck.Add("BuildUpBody"); a.SaveMagicDeck(deck);
    SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HolyEmblem));
    var props = SavedProperties.FromInternal(a, null)!;
    Equal(a.MagicDeckJson, props.strings!.Single(x => x.name == nameof(HolyEmblem.MagicDeckJson)).value);
    var json = JsonSerializer.Serialize(props, JsonSerializationUtility.Options);
    var restored = JsonSerializer.Deserialize<SavedProperties>(json, JsonSerializationUtility.Options)!;
    var b = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); restored.Fill(b); Equal(a.MagicDeckJson, b.MagicDeckJson);
});
Test("原生网络包往返保持 Magic Deck", () => {
    var a = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable();
    var props = SavedProperties.FromInternal(a, null)!;
    var writer = new PacketWriter(); props.Serialize(writer);
    var reader = new PacketReader(); reader.Reset(writer.Buffer);
    var restored = new SavedProperties(); restored.Deserialize(reader);
    var b = (HolyEmblem)ModelDb.Relic<HolyEmblem>().ToMutable(); restored.Fill(b); Equal(a.MagicDeckJson, b.MagicDeckJson);
    Equal(writer.BitPosition, reader.BitPosition);
});
Test("起始牌组与一代一致", () => {
    var character = ModelDb.Character<FrierenMod.Scripts.Characters.Frieren>();
    Equal(75, character.StartingHp); Equal(99, character.StartingGold);
    var deck = character.StartingDeck.ToArray(); Equal(10, deck.Length);
    Equal(4, deck.Count(x => x is StrikeFrieren)); Equal(3, deck.Count(x => x is DefendFrieren));
    Equal(1, deck.Count(x => x is Flow)); Equal(1, deck.Count(x => x is DefensiveMagic)); Equal(1, deck.Count(x => x is RapidChant));
});
Test("打击与防御升级数值", () => {
    var strike = ModelDb.Card<StrikeFrieren>().ToMutable(); Equal(6m, strike.DynamicVars.Damage.BaseValue);
    strike.UpgradeInternal(); Equal(9m, strike.DynamicVars.Damage.BaseValue);
    var defend = ModelDb.Card<DefendFrieren>().ToMutable(); defend.UpgradeInternal(); Equal(8m, defend.DynamicVars.Block.BaseValue);
});
Test("快速咏唱保留与精准咏唱消耗升级", () => {
    var rapid = ModelDb.Card<RapidChant>().ToMutable(); rapid.UpgradeInternal(); Equal(true, rapid.Keywords.Contains(CardKeyword.Retain));
    var precise = ModelDb.Card<PreciseChant>().ToMutable(); Equal(true, precise.Keywords.Contains(CardKeyword.Exhaust));
    precise.UpgradeInternal(); Equal(false, precise.Keywords.Contains(CardKeyword.Exhaust));
});
Test("终极咏唱和魔力膨胀费用升级", () => {
    var final = ModelDb.Card<FinalChant>().ToMutable(); final.UpgradeInternal(); Equal(1, final.EnergyCost.GetWithModifiers(CostModifiers.Local));
    var expansion = ModelDb.Card<ManaExpansion>().ToMutable(); expansion.UpgradeInternal(); Equal(0, expansion.EnergyCost.GetWithModifiers(CostModifiers.Local));
});
Test("魔力不升级且同调不重复列出消耗关键字", () => {
    var mana = ModelDb.Card<Mana>(); Equal(0, mana.MaxUpgradeLevel); Equal(CardType.Status, mana.Type);
    Equal(false, mana.Keywords.Contains(CardKeyword.Exhaust)); Equal(0, mana.EnergyCost.Canonical);
});
Test("真实模型 ID 与中英本地化一致", () => {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    foreach (var lang in new[] { "eng", "zhs" })
    {
        var text = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(root, "FrierenMod/localization", lang, "cards.json")))!;
        foreach (var type in typeof(Mana).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CardModel).IsAssignableFrom(t)))
        {
            var key = type.GetPrefix() + ModelDb.GetId(type).Entry;
            Equal(true, text.ContainsKey(key + ".title")); Equal(true, text.ContainsKey(key + ".description"));
        }
    }
});
Test("重构保持全部已注册模型身份", () => {
    // 保留重构前模型身份，并显式记录本次新增模型；覆盖卡牌、能力、角色、遗物及内容池。
    var expected = JsonSerializer.Deserialize<Dictionary<string, string>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "model-identities.json")))!;
    var actual = typeof(Mana).Assembly.GetTypes()
        .Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t))
        .ToDictionary(t => t.FullName!, t => ModelDb.GetId(t).ToString());
    Equal(expected.Count, actual.Count);
    foreach (var (name, id) in expected) Equal(id, actual[name]);
});
Test("一代77张有效芙莉莲卡牌均有可获得模型", () => {
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    using var audit = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "docs/inventory/frieren_card_parity.json")));
    int count = 0;
    foreach (var entry in audit.RootElement.EnumerateArray())
    {
        if (!entry.GetProperty("active").GetBoolean()) continue;
        var name = entry.GetProperty("model").GetString();
        var type = typeof(Mana).Assembly.GetType("FrierenMod.Scripts.Cards." + name, throwOnError: true)!;
        var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
        Equal(false, card.Rarity == CardRarity.Token);
        count++;
    }
    Equal(77, count);
    Equal(CardRarity.Token, ModelDb.Card<PaperPlaneSpell>().Rarity);
});
Test("公共卡牌基类不绑定角色卡池", () => {
    var shared = typeof(FrierenMod.Scripts.Shared.Cards.ModCard);
    Equal(true, shared.IsAbstract);
    Equal(false, shared.GetCustomAttributes(typeof(BaseLib.Utils.PoolAttribute), true).Any());
    Equal(true, typeof(FrierenCard).GetCustomAttributes(typeof(BaseLib.Utils.PoolAttribute), true).Any());
    Equal("res://FrierenMod/images/cards/StrikeFrieren.png", ModelDb.Card<StrikeFrieren>().PortraitPath);
    Equal("res://FrierenMod/images/cards/DefensiveMagic.png", ModelDb.Card<SecondDefensiveMagic>().PortraitPath);
});
Console.WriteLine($"{passed} integration tests passed.");

using System.Text.Json;

namespace FrierenMod.Core;

public sealed record MagicItem(int InstanceId, string SpellId, int Slot = -1);
public sealed record MagicProp(int InstanceId, string PropId);
public sealed record MagicDeckSave(int Version, MagicItem[] Items, bool SetupCompleted = false,
    int DraftSlot = 0, string[]? Draft = null, int RewardPity = 0, MagicProp[]? Props = null, int NextId = 0);

// 使用实例 ID 区分同名因子；对象只属于单个玩家，存档采用值快照。
public sealed class MagicDeckState
{
    private readonly List<MagicItem> _items;
    private readonly List<MagicProp> _props = [];
    private int _nextId = 1;
    public IReadOnlyList<MagicItem> Items => _items.AsReadOnly();
    public IReadOnlyList<MagicProp> Props => _props.AsReadOnly();
    public bool SetupCompleted { get; private set; }
    public int DraftSlot { get; private set; }
    public string[] Draft { get; private set; } = [];
    public int RewardPity { get; set; }

    public MagicDeckState() : this([]) { }
    private MagicDeckState(IEnumerable<MagicItem> items) => _items = [.. items];

    public static SpellDefinition Definition(string id) => SpellCatalog.Get(id).Rules;

    public void PrepareDraft(Func<int, int> next)
    {
        if (!SetupCompleted && Draft.Length == 0)
            Draft = SpellCatalog.Offer(FactorRarity.Common, next, starting: true);
    }

    public void SelectInitial(string spellId)
    {
        if (SetupCompleted || !Draft.Contains(spellId)) throw new InvalidOperationException("初始因子选项已失效。");
        Equip(Add(spellId).InstanceId, (ManaSlot)DraftSlot);
        Draft = [];
        DraftSlot++;
        SetupCompleted = DraftSlot == 3;
    }

    public void Unequip(int instanceId)
    {
        int index = _items.FindIndex(x => x.InstanceId == instanceId);
        if (index < 0) throw new ArgumentException("魔法因子不存在。", nameof(instanceId));
        _items[index] = _items[index] with { Slot = -1 };
    }

    public MagicItem Add(string spellId)
    {
        _ = Definition(spellId);
        var item = new MagicItem(AllocateId(), spellId);
        _items.Add(item);
        return item;
    }

    private int AllocateId() { int id = _nextId; _nextId = checked(id + 1); return id; }

    public MagicProp AddProp(string id)
    {
        _ = MagicPropRules.Get(id);
        var prop = new MagicProp(AllocateId(), id);
        _props.Add(prop);
        return prop;
    }

    public void Remove(int instanceId)
    {
        if (_items.RemoveAll(x => x.InstanceId == instanceId) + _props.RemoveAll(x => x.InstanceId == instanceId) == 0)
            throw new ArgumentException("魔法牌组条目不存在。", nameof(instanceId));
    }

    public void Equip(int instanceId, ManaSlot slot)
    {
        if (!Enum.IsDefined(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        if (!_items.Any(x => x.InstanceId == instanceId)) throw new ArgumentException("魔法因子不存在。", nameof(instanceId));
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.InstanceId == instanceId) _items[i] = item with { Slot = (int)slot };
            else if (item.Slot == (int)slot) _items[i] = item with { Slot = -1 };
        }
    }

    public SpellDefinition? InSlot(ManaSlot slot)
    {
        if (!Enum.IsDefined(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        var item = _items.SingleOrDefault(x => x.Slot == (int)slot);
        return item == null ? null : Definition(item.SpellId);
    }

    public SpellDefinition? ResolveSlot(ManaSlot slot)
    {
        // 蓝图沿槽位循环寻找下一个非蓝图；空槽和全蓝图都不可发动。
        for (int offset = 0; offset < 3; offset++)
        {
            var spell = InSlot((ManaSlot)(((int)slot + offset) % 3));
            if (spell == null || spell.Id != "BluePrint") return spell;
        }
        return null;
    }

    public string ToJson() => JsonSerializer.Serialize(new MagicDeckSave(3, [.. _items], SetupCompleted, DraftSlot, Draft, RewardPity, [.. _props], _nextId));

    public static MagicDeckState FromJson(string json)
    {
        var save = JsonSerializer.Deserialize<MagicDeckSave>(json) ?? throw new JsonException("Magic Deck 存档为空。");
        if (save.Version is not (1 or 2 or 3)) throw new JsonException($"不支持的 Magic Deck 版本：{save.Version}");
        if (save.Items == null || save.Items.Any(x => x == null || x.InstanceId < 1 || x.Slot < -1 || x.Slot > 2))
            throw new JsonException("Magic Deck 存档包含无效条目。");
        if (save.Items.Select(x => x.InstanceId).Distinct().Count() != save.Items.Length ||
            save.Items.Where(x => x.Slot >= 0).GroupBy(x => x.Slot).Any(x => x.Count() > 1))
            throw new JsonException("Magic Deck 存档包含重复实例或槽位。");
        foreach (var item in save.Items) _ = Definition(item.SpellId);
        if (save.DraftSlot < 0 || save.DraftSlot > 3 || save.SetupCompleted != (save.DraftSlot == 3))
            throw new JsonException("初始因子选择进度无效。");
        if (save.Draft is { Length: > 0 } && (save.Draft.Length != 3 || save.Draft.Distinct().Count() != 3 ||
            save.Draft.Any(x => SpellCatalog.Get(x).Rarity != FactorRarity.Common || SpellCatalog.Get(x).Puzzle)))
            throw new JsonException("初始因子选项无效。");
        var props = save.Props ?? [];
        if (props.Any(x => x == null || x.InstanceId < 1)) throw new JsonException("魔具条目无效。");
        foreach (var prop in props) _ = MagicPropRules.Get(prop.PropId);
        var ids = save.Items.Select(x => x.InstanceId).Concat(props.Select(x => x.InstanceId)).ToArray();
        if (ids.Distinct().Count() != ids.Length) throw new JsonException("因子与魔具实例编号重复。");
        int nextId = checked(ids.DefaultIfEmpty(0).Max() + 1);
        if (save.NextId < 0 || (save.NextId > 0 && save.NextId < nextId)) throw new JsonException("魔法牌组编号计数无效。");
        var result = new MagicDeckState(save.Items) { SetupCompleted = save.SetupCompleted, DraftSlot = save.DraftSlot,
            Draft = save.Draft ?? [], RewardPity = save.RewardPity, _nextId = Math.Max(nextId, save.NextId) };
        result._props.AddRange(props);
        return result;
    }

    // 仅供旧版存档与回归用例使用；新角色必须通过三次初始选择取得因子。
    public static MagicDeckState CreatePrototypeLoadout()
    {
        var deck = new MagicDeckState();
        deck.Equip(deck.Add("AdvancedDefensiveMagic").InstanceId, ManaSlot.Draw);
        deck.Equip(deck.Add("BuildUpBody").InstanceId, ManaSlot.Hand);
        deck.Equip(deck.Add("HighSpeedMovement").InstanceId, ManaSlot.Discard);
        return deck;
    }
}

using System.Text.Json;

namespace FrierenMod.Core;

public enum CommissionGoal { Monsters, Elites, Spell }
public enum CommissionRewardKind { Gold, Prop, Potion, Relic }
public enum CommissionStatus { Available, Accepted, Claimed, Abandoned }
public enum CommissionOperation { Accept, Claim, Abandon }
public sealed record CommissionReward(CommissionRewardKind Kind, string Item, int Amount);
public sealed class Commission
{
    public int Id { get; init; }
    public int Act { get; init; }
    public string Template { get; init; } = "";
    public int OpensAt { get; init; }
    public CommissionGoal Goal { get; init; }
    public int Target { get; init; }
    public string Spell { get; init; } = "";
    public CommissionReward Reward { get; init; } = new(CommissionRewardKind.Gold, "", 0);
    public int Progress { get; set; }
    public CommissionStatus Status { get; set; }
}

// 每名玩家独立保存整张委托板；界面不掷随机数，所有奖励在生成时确定。
public sealed class CommissionState
{
    public const int ActiveLimit = 3;
    public int Version { get; set; } = 1;
    public int Revision { get; set; }
    public List<int> GeneratedActs { get; set; } = [];
    public List<Commission> Quests { get; set; } = [];
    public HashSet<string> CountedEvents { get; set; } = [];
    public int ActiveCount => Quests.Count(q => q.Status == CommissionStatus.Accepted);

    public bool EnsureAct(int act, Func<int, int> next, IReadOnlyList<string> spells,
        IReadOnlyList<string> props, IReadOnlyList<string> potions, IReadOnlyList<string> relics)
    {
        if (act < 0 || GeneratedActs.Contains(act)) return false;
        if (new[] { spells.Count, props.Count, potions.Count, relics.Count }.Any(n => n == 0))
            throw new ArgumentException("委托内容池不能为空。");
        string Pick(IReadOnlyList<string> pool) => pool[next(pool.Count)];
        var magic = spells.ToList();
        string FirstSpell() { int i = next(magic.Count); string id = magic[i]; magic.RemoveAt(i); return id; }
        void Add(int slot, string template, int floor, CommissionGoal goal, int target, CommissionReward reward, string spell = "") =>
            Quests.Add(new() { Id = checked(act * 100 + slot), Act = act, Template = template, OpensAt = floor,
                Goal = goal, Target = target, Reward = reward, Spell = spell });
        Add(1, "PATROL", 1, CommissionGoal.Monsters, 3 + act, new(CommissionRewardKind.Gold, "", 40 + act * 10));
        Add(2, "RESEARCH", 1, CommissionGoal.Spell, 1, new(CommissionRewardKind.Potion, Pick(potions), 1), FirstSpell());
        Add(3, "SUPPLIES", 3, CommissionGoal.Monsters, 6 + act, new(CommissionRewardKind.Prop, Pick(props), 1));
        Add(4, "ELITE", 5, CommissionGoal.Elites, 1, new(CommissionRewardKind.Relic, Pick(relics), 100 + act * 25));
        Add(5, "TEACHING", 7, CommissionGoal.Spell, 1, new(CommissionRewardKind.Prop, Pick(props), 1), FirstSpell());
        Add(6, "ROAD", 9, CommissionGoal.Monsters, 8 + act * 2, new(CommissionRewardKind.Gold, "", 80 + act * 20));
        GeneratedActs.Add(act); Revision++; return true;
    }

    public bool Accept(int id, int act, int floor)
    {
        var q = Quests.SingleOrDefault(q => q.Id == id);
        if (q == null || q.Status != CommissionStatus.Available || q.Act != act || q.OpensAt > floor || ActiveCount >= ActiveLimit) return false;
        q.Status = CommissionStatus.Accepted; q.Progress = 0; Revision++; return true;
    }
    public bool Abandon(int id)
    {
        var q = Quests.SingleOrDefault(q => q.Id == id);
        if (q?.Status != CommissionStatus.Accepted) return false;
        q.Status = CommissionStatus.Abandoned; Revision++; return true;
    }
    public bool Record(string eventId, CommissionGoal goal, int count = 1)
    {
        if (goal == CommissionGoal.Spell || count <= 0) return false;
        var active = Quests.Where(q => q.Status == CommissionStatus.Accepted && q.Goal == goal && q.Progress < q.Target).ToArray();
        if (!CountedEvents.Add(eventId)) return false;
        foreach (var q in active) q.Progress = Math.Min(q.Target, q.Progress + count);
        Revision++; return true;
    }
    public static int ProgressOf(Commission quest, Func<string, bool> ownsSpell) => quest.Goal == CommissionGoal.Spell
        ? (ownsSpell(quest.Spell) ? 1 : 0) : quest.Progress;
    public static bool Ready(Commission q, Func<string, bool> ownsSpell) => q.Status == CommissionStatus.Accepted && ProgressOf(q, ownsSpell) >= q.Target;
    public bool Claim(int id, Func<string, bool> ownsSpell)
    {
        var q = Quests.SingleOrDefault(q => q.Id == id);
        if (q == null || !Ready(q, ownsSpell)) return false;
        q.Status = CommissionStatus.Claimed; Revision++; return true;
    }
    public string ToJson() => JsonSerializer.Serialize(this);
    public static CommissionState FromJson(string json)
    {
        var state = JsonSerializer.Deserialize<CommissionState>(json) ?? throw new ArgumentException("委托存档为空。");
        if (state.Version != 1 || state.Revision < 0 || state.Quests == null || state.GeneratedActs == null || state.CountedEvents == null ||
            state.GeneratedActs.Distinct().Count() != state.GeneratedActs.Count || state.Quests.Select(q => q.Id).Distinct().Count() != state.Quests.Count ||
            state.Quests.Any(q => q.Act < 0 || !state.GeneratedActs.Contains(q.Act) || q.Target <= 0 || q.Progress < 0 || q.Progress > q.Target ||
                !Enum.IsDefined(q.Goal) || !Enum.IsDefined(q.Status) || !Enum.IsDefined(q.Reward.Kind) || q.Reward.Amount <= 0))
            throw new ArgumentException("委托存档格式无效。");
        return state;
    }
}

using System.Text.Json;

namespace FrierenMod.Core;

public sealed class SlotBackProgress
{
    public HashSet<string> Unlocked { get; set; } = ["0001", "0002", "0003"];
    public string[] Selected { get; set; } = ["0001", "0002", "0003"];
    public HashSet<string> RewardedRuns { get; set; } = [];

    public bool Unlock(string id) => SlotBackCatalog.Contains(id) && Unlocked.Add(id);

    public bool Select(int slot, string id)
    {
        if (slot is < 0 or > 2 || !SlotBackCatalog.Contains(id) || !Unlocked.Contains(id)) return false;
        Selected[slot] = id;
        return true;
    }

    public string? Roll(char group, Func<int, int> roll)
    {
        var candidates = SlotBackCatalog.Ids.Where(id => id[0] == group && !Unlocked.Contains(id)).ToArray();
        return candidates.Length == 0 ? null : candidates[roll(candidates.Length)];
    }

    public IReadOnlyList<string> GrantVictory(string runKey, int ascension, Func<int, int> roll)
    {
        if (!RewardedRuns.Add(runKey)) return [];
        var added = new List<string>();
        foreach (var (group, minimum) in new[] { ('1', 0), ('2', 5), ('3', 10) }) {
            if (ascension < minimum) continue;
            if (Roll(group, roll) is { } id && Unlock(id)) added.Add(id);
        }
        return added;
    }

    public IEnumerable<string> CollectionRewards()
    {
        // 一代收藏库打开时赠送纪念背景，再按当前收藏数与勇者小队成员补发收藏成就。
        foreach (var id in new[] { "9999", "9997", "9998" }) if (Unlock(id)) yield return id;
        if (Unlocked.Count >= 50 && Unlock("9001")) yield return "9001";
        if (Unlocked.Count >= 100 && Unlock("9002")) yield return "9002";
        if (new[] { "2007", "2008", "3002", "4012", "9003" }.Any(Unlocked.Contains) &&
            Unlocked.Contains("4013") && new[] { "1008", "5015" }.Any(Unlocked.Contains) && Unlock("9003"))
            yield return "9003";
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static SlotBackProgress FromJson(string json)
    {
        var state = JsonSerializer.Deserialize<SlotBackProgress>(json) ?? new();
        state.Unlocked ??= [];
        state.RewardedRuns ??= [];
        state.Unlocked.RemoveWhere(id => !SlotBackCatalog.Contains(id));
        state.Unlocked.UnionWith(["0001", "0002", "0003"]);
        if (state.Selected is not { Length: 3 }) state.Selected = ["0001", "0002", "0003"];
        for (int i = 0; i < 3; i++)
            if (!state.Unlocked.Contains(state.Selected[i])) state.Selected[i] = $"000{i + 1}";
        return state;
    }
}

using FrierenMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Presentation;

public static class SlotBackStore
{
    private static SaveManager? _manager;
    private static int _profile = int.MinValue;
    private static SlotBackProgress _progress = new();
    private static bool _writable = true;
    private static ISaveStore Store => (ISaveStore)AccessTools.Field(typeof(SaveManager), "_saveStore").GetValue(SaveManager.Instance)!;
    private static string Folder => Path.Combine(UserDataPathProvider.GetProfileDir(SaveManager.Instance.CurrentProfileId), "FrierenMod");
    private static string FilePath => Path.Combine(Folder, "slot_backs.json");
    public static event Action? Changed;
    public static event Action<string>? Unlocked;

    public static SlotBackProgress Read()
    {
        var manager = SaveManager.Instance;
        int profile = manager.IsProfileInitialized ? manager.CurrentProfileId : -1;
        if (_manager == manager && _profile == profile) return _progress;
        _manager = manager; _profile = profile; _progress = new(); _writable = true;
        if (profile >= 0) {
            try {
                if (Store.FileExists(FilePath) && Store.ReadFile(FilePath) is { Length: > 0 } json) _progress = SlotBackProgress.FromJson(json);
            } catch (Exception ex) {
                // 不以默认值覆盖损坏的收藏存档，保留原文件供恢复。
                _writable = false; Log.Warn("FrierenMod 卡背收藏读取失败，保留原存档：" + ex.Message);
            }
        }
        return _progress;
    }

    private static void Save()
    {
        if (_profile < 0 || !_writable) return;
        try {
            Store.CreateDirectory(Folder);
            Store.WriteFile(FilePath, _progress.ToJson());
        } catch (Exception ex) { Log.Warn("FrierenMod 卡背收藏保存失败：" + ex.Message); }
    }

    public static bool Unlock(string id)
    {
        if (!Read().Unlock(id)) return false;
        Save(); Changed?.Invoke(); Unlocked?.Invoke(id);
        return true;
    }

    public static void CollectLibraryRewards()
    {
        var added = Read().CollectionRewards().ToArray();
        if (added.Length == 0) return;
        Save(); Changed?.Invoke();
        foreach (var id in added) Unlocked?.Invoke(id);
    }

    public static bool Select(int slot, string id)
    {
        if (!Read().Select(slot, id)) return false;
        Save(); Changed?.Invoke(); return true;
    }

    public static bool SelectAll(IReadOnlyList<string> ids)
    {
        var progress = Read();
        if (ids.Count != 3 || ids.Any(id => !progress.Unlocked.Contains(id))) return false;
        for (int i = 0; i < 3; i++) progress.Select(i, ids[i]);
        Save(); Changed?.Invoke(); return true;
    }

    public static void GrantVictory(string runKey, int ascension)
    {
        if (Read().RewardedRuns.Contains(runKey)) return;
        var added = Read().GrantVictory(runKey, ascension, Random.Shared.Next);
        // 即使当前组已集齐，也保存本局奖励标记，避免读档后重复结算。
        Save(); Changed?.Invoke();
        foreach (var id in added) Unlocked?.Invoke(id);
    }

    public static string Selected(int slot) => Read().Selected[slot];
}

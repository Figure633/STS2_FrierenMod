using FrierenMod.Core;
using FrierenMod.Scripts.UI;
using MegaCrit.Sts2.Core.Localization;

public static class Sts2SlotBackTests
{
    public static int Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Godot.GD.Print("PASS 二代卡槽背景：" + name); }
        Check(SlotBackCatalog.LegacyIds.Count == 104 && SlotBackCatalog.NewIds.Count == 23 && SlotBackCatalog.Ids.Distinct().Count() == 127,
            "保留104张旧背景，追加23张且ID不冲突");
        foreach (string id in SlotBackCatalog.NewIds) {
            var texture = SlotBackLibrary.Texture(id);
            Check(texture is { } && texture.GetWidth() == 301 && texture.GetHeight() == 461, "打包后的新增背景可加载：" + id);
        }
        foreach (string language in new[] { "eng", "zhs" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (string id in SlotBackCatalog.NewIds) {
                Check(!SlotBackLibrary.Text($"BACK.{id}.title").Contains($"BACK.{id}") &&
                    SlotBackLibrary.Text($"BACK.{id}.description") == (language == "zhs" ? "解锁条件待定。" : "Unlock condition to be determined."),
                    "名称与待定条件本地化完整：" + language + "/" + id);
            }
        }
        var pending = new SlotBackProgress();
        foreach (int ascension in new[] { 0, 5, 10 }) pending.GrantVictory("pending-conditions-" + ascension, ascension, _ => 0);
        pending.CollectionRewards().ToArray();
        Check(!pending.Unlocked.Overlaps(SlotBackCatalog.NewIds), "未指定条件的新增背景不进入通关或收藏赠送奖励");
        Check(!pending.Select(2, "8023"), "锁定背景不能直接选择");
        Check(pending.Unlock("8023") && pending.Select(2, "8023"), "新背景复用原有解锁与选择接口");
        Check(SlotBackProgress.FromJson(pending.ToJson()).Selected[2] == "8023", "新增背景的解锁和选择可保存恢复");
        Check(SlotBackProgress.FromJson("{\"Unlocked\":[\"0001\",\"0002\",\"0003\",\"5015\"],\"Selected\":[\"5015\",\"0002\",\"0003\"]}").Selected[0] == "5015",
            "旧收藏存档与已选背景保持兼容");
        return count;
    }
}

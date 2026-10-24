using BaseLib.Abstracts;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Test;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class AchievementSystemTests
{
    public static async Task<int> Run(Node root)
    {
        SaveManager.Instance.InitPrefsDataForTest();
        int count = 0;
        void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            count++; GD.Print("PASS 成就系统：" + name);
        }
        async Task Tick() {
            for (int i = 0; i < 4; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        async Task Snapshot(string name) {
            await Tick();
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + name + ".png"));
        }
        foreach (var (ascension, expected) in new[] { (0, 1), (4, 1), (5, 2), (9, 2), (10, 3) }) {
            var progress = new SlotBackProgress();
            var added = progress.GrantVictory("one-run", ascension, _ => 0);
            Check(added.Count == expected, "进阶0/5/10奖励边界：" + ascension);
            Check(!SlotBackProgress.FromJson(progress.ToJson()).GrantVictory("one-run", ascension, _ => 0).Any(), "读档后同一跑局不重复奖励");
        }
        var collection = new SlotBackProgress(); collection.Unlocked.UnionWith(SlotBackCatalog.Ids.Take(49));
        Check(collection.CollectionRewards().Contains("9001"), "50张收藏奖励可达");
        collection.Unlocked.UnionWith(SlotBackCatalog.Ids);
        collection.Unlocked.Remove("9002");
        Check(collection.CollectionRewards().Contains("9002"), "100张收藏奖励可达");

        // 两个本地档案都使用原生内存 ISaveStore，测试不会读写玩家文件。
        var originalManager = SaveManager.Instance;
        var memory = new MockGodotFileIo("user://achievement-tests");
        var manager = new SaveManager(memory, memory, true);
        SaveManager.MockInstanceForTesting(manager); manager.InitProfileId(0);
        SlotBackStore.Unlock("0004"); SlotBackStore.Select(1, "0004");
        SlotBackStore.GrantVictory("persisted-run", 10);
        manager.InitProfileId(1);
        Check(SlotBackStore.Read().Unlocked.Count == 3 && SlotBackStore.Selected(1) == "0002", "不同档案的解锁与三槽选择隔离");
        manager.InitProfileId(0);
        Check(SlotBackStore.Read().Unlocked.Contains("0004") && SlotBackStore.Selected(1) == "0004", "切回档案从原生存储恢复进度和选择");
        int savedCount = SlotBackStore.Read().Unlocked.Count;
        SlotBackStore.GrantVictory("persisted-run", 10);
        Check(SlotBackStore.Read().Unlocked.Count == savedCount, "持久化通关标记阻止跨读档重复奖励");
        Check(memory.Calls.Any(c => c.Method == MockGodotFileIo.Methods.writeFile), "确实通过原生ISaveStore保存，而非仅内存状态");
        SaveManager.MockInstanceForTesting(originalManager);
        var progressNow = SlotBackStore.Read(); progressNow.Unlocked.Clear(); progressNow.Unlocked.UnionWith(["0001", "0002", "0003"]);
        progressNow.Selected = ["0001", "0002", "0003"]; progressNow.RewardedRuns.Clear();

        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var other = Player.CreateForNewRun<Frieren>(UnlockState.all, 2);
        var run = RunState.CreateForTest([player, other], ascensionLevel: 10, seed: "ACHIEVEMENTS-080");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        RunManager.Instance.WinTime = 7200;
        var combat = new CombatState(runState: run); combat.AddPlayer(player); combat.AddPlayer(other);
        var enemy = combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null); combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new ThrowingPlayerChoiceContext();
        void Equip(params string[] ids) {
            var deck = new MagicDeckState();
            for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
            foreach (var item in deck.Items.ToArray()) deck.Remove(item.InstanceId);
            for (int i = 0; i < ids.Length; i++) deck.Equip(deck.Add(ids[i]).InstanceId, (ManaSlot)i);
            MagicDeckStore.Write(player, deck);
        }
        Equip("BluePrint", "BluePrint", "BluePrint");
        Check(!FrierenAchievements.Unlock(other, "0004"), "其他玩家的事件不会解锁本地成就");
        PresentationSettings.Achievements = false;
        Check(!FrierenAchievements.Unlock(player, "0004"), "关闭成就选项时不再解锁");
        PresentationSettings.Achievements = true;
        for (int i = 0; i < 100; i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        for (int i = 0; i < 4; i++) await CardPileCmd.Add(combat.CreateCard<Laziness>(player), PileType.Discard);
        for (int i = 0; i < 8; i++) await CardPileCmd.Add(combat.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>(player), PileType.Draw);
        player.Creature.GainBlockInternal(999);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 30, player.Creature, null);
        await PowerCmd.Apply<DexterityPower>(context, player.Creature, 1, player.Creature, null);
        await PowerCmd.Apply<VigorPower>(context, player.Creature, 1, player.Creature, null);
        await PowerCmd.Apply<ArtifactPower>(context, player.Creature, 1, player.Creature, null);
        await PowerCmd.Apply<MetallicizePower>(context, player.Creature, 1, player.Creature, null);
        await Hook.AfterPlayerTurnStart(combat, context, player);
        foreach (string id in new[] { "5001", "5002", "5003", "5005", "5006", "5015", "7001" })
            Check(SlotBackStore.Read().Unlocked.Contains(id), "原生回合钩子实际解锁 " + id);
        CardPlay Play(CardModel card) => new() { Card = card, Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, ResultPile = PileType.Discard,
            Target = enemy, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
        await Hook.AfterCardPlayed(combat, context, Play(combat.CreateCard<RapidChant>(player)));
        await Hook.AfterCardPlayed(combat, context, Play(combat.CreateCard<LightningMagic>(player)));
        Check(SlotBackStore.Read().Unlocked.Contains("0004") && SlotBackStore.Read().Unlocked.Contains("4001"), "原生出牌钩子解锁咏唱和破灭之雷魔法");
        foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        var damageCard = combat.CreateCard<OrdinaryOffensiveMagic>(player); await CardPileCmd.Add(damageCard, PileType.Hand);
        FrierenAchievements.CheckDamage(damageCard, enemy, 99, 100, "4008");
        Check(!SlotBackStore.Read().Unlocked.Contains("4008"), "伤害未达到阈值不解锁");
        FrierenAchievements.CheckDamage(damageCard, enemy, 100, 100, "4008");
        Check(SlotBackStore.Read().Unlocked.Contains("4008"), "伤害等于100即可解锁");
        Equip("BarrierMagic", "Hypnosis", "NatureBeauty");
        RunManager.Instance.OnEnded(true);
        Check(SlotBackStore.Read().Unlocked.Contains("5010") && SlotBackStore.Read().Unlocked.Contains("5012"), "原生跑局结算按罕见因子和两小时判定");
        Check(new[] { '1', '2', '3' }.All(g => SlotBackStore.Read().Unlocked.Any(id => id[0] == g)), "进阶10通关实际发放三档卡背");
        int winCount = SlotBackStore.Read().Unlocked.Count;
        RunManager.Instance.OnEnded(true);
        Check(SlotBackStore.Read().Unlocked.Count == winCount, "重复原生结算不重复发奖");
        foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        var puzzlePlay = Play(combat.CreateCard<RapidChant>(player));
        await PuzzleCmd.Chant(context, puzzlePlay, ChantRules.Preview(ManaSlot.Hand, SpellCatalog.Get("Factor010").Rules, 1));
        Check(!SlotBackStore.Read().Unlocked.Contains("7002"), "错序谜题不解锁");
        foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        foreach (var (slot, id) in new[] { (ManaSlot.Draw, "Factor100"), (ManaSlot.Hand, "Factor010"), (ManaSlot.Discard, "Factor001") })
            await PuzzleCmd.Chant(context, puzzlePlay, ChantRules.Preview(slot, SpellCatalog.Get(id).Rules, 1));
        Check(SlotBackStore.Read().Unlocked.Contains("7002"), "二代真实谜题成功后解锁");
        var defeat = new CombatRoom(ModelDb.Encounter<HunterKillerNormal>().ToMutable(), run); run.PushRoom(defeat);
        defeat.Encounter.GenerateMonstersWithSlots(run);
        run.AppendToMapPointHistory(MapPointType.Monster, RoomType.Monster, defeat.Encounter.Id);
        player.Creature.SetCurrentHpInternal(0);
        RunManager.Instance.OnEnded(false);
        Check(SlotBackStore.Read().Unlocked.Contains("0008") && SlotBackStore.Read().Unlocked.Contains("0009"), "猎人杀手战败对接原生结算");
        player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, false);

        // 检查 BaseLib 已缓存 canonical 材质后，槽位卡背仍能正确切换。
        _ = ModelDb.Card<BarrierMagic>().CustomFrameMaterial;
        var factor = FactorCard.Create(player, "BarrierMagic", ManaSlot.Hand);
        SlotBackStore.Select(1, "0004");
        Check(((AtlasTexture)factor.CustomFrame!).Atlas.ResourcePath.EndsWith("0004.png") &&
            ((ShaderMaterial)factor.CustomFrameMaterial!).GetShaderParameter("source_region").AsVector4() == new Vector4(0, 0, 1, 1), "实际因子卡面及缓存材质使用所选卡背");
        SlotBackStore.Select(1, "0002");
        Check(((AtlasTexture)factor.CustomFrame!).Atlas.ResourcePath.EndsWith("0002.png"), "同一槽位切换卡背立即生效");

        LocManager.Instance.SetLanguage("zhs");
        var ui = new Control { Name = "AchievementTestUi", Size = new(1920, 1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new Color("192333"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var library = SlotBackLibrary.Open(ui, 0); await Tick();
        var grid = library.View.GetNode<GridContainer>("LibraryCanvas/BackScroll/BackGrid");
        Check(grid.GetChildCount() == 104, "收藏界面完整显示104张卡背");
        var image = grid.GetNode<TextureButton>("Back_0004/SelectBack_0004"); image.EmitSignal(BaseButton.SignalName.Pressed);
        Check(SlotBackStore.Selected(0) == "0001", "选中预览不会提前写入保存选择");
        library.Close(); await Tick();
        Check(SlotBackStore.Selected(0) == "0001", "取消不会保存未确认的卡背选择");
        library = SlotBackLibrary.Open(ui, 0); await Tick();
        grid = library.View.GetNode<GridContainer>("LibraryCanvas/BackScroll/BackGrid");
        grid.GetNode<TextureButton>("Back_0004/SelectBack_0004").EmitSignal(BaseButton.SignalName.Pressed);
        library.SaveAndClose(); await Tick();
        Check(SlotBackStore.Selected(0) == "0004", "点击保存后写入三槽选择");
        library = SlotBackLibrary.Open(ui, 0); await Tick();
        var search = library.View.GetNode<LineEdit>("LibraryCanvas/BackSearch"); search.Text = "0004";
        search.EmitSignal(LineEdit.SignalName.TextChanged, search.Text); await Tick();
        grid = library.View.GetNode<GridContainer>("LibraryCanvas/BackScroll/BackGrid");
        Check(grid.GetChildCount() == 1, "按ID搜索准确过滤");
        search.Text = ""; search.EmitSignal(LineEdit.SignalName.TextChanged, search.Text); await Tick();
        var ownedOnly = library.View.GetNode<CheckButton>("LibraryCanvas/OwnedOnly"); ownedOnly.ButtonPressed = true; await Tick();
        Check(grid.GetChildCount() == SlotBackStore.Read().Unlocked.Count, "只看已解锁筛选准确");
        ownedOnly.ButtonPressed = false; await Tick();
        await Snapshot("achievement-library");
        library.Close(); await Tick();
        var book = new MagicBookScreen(ui, player); book.Open(); await Tick();
        Check(book.View.FindChild("OpenSlotBackLibrary", true, false) != null, "魔法书提供实际收藏入口");
        await Snapshot("achievement-slots");
        book.Close(); book.View.QueueFree(); await Tick();
        SlotBackSelectionEntry.Attach(ui, true);
        Check(ui.GetNode<Control>("FrierenSlotBackSelection").Visible, "选人页面可显示三个卡背入口");
        SlotBackSelectionEntry.Attach(ui, false);
        Check(!ui.GetNode<Control>("FrierenSlotBackSelection").Visible, "切换其他角色时隐藏入口");
        var session = AchievementNotifications.Attach(ui);
        SlotBackStore.Unlock("4002"); SlotBackStore.Unlock("4007");
        Check(session.PendingCount == 2, "同时解锁时提示排队，不互相覆盖");
        await root.ToSignal(root.GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        await Snapshot("achievement-unlock");
        Check(ui.GetNode<Control>("FrierenAchievementPopup").MouseFilter == Control.MouseFilterEnum.Ignore, "成就提示不拦截输入");
        ui.QueueFree(); await Tick();
        SlotBackStore.Unlock("4010");
        Check(session.PendingCount <= 1, "离开场景后清除队列并解绑解锁监听");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp();
        return count;
    }
}

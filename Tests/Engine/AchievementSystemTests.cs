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
    private static bool CombatEnding(ref bool __result) { __result = true; return false; }
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

        // 槽位背景不再替换因子本身的紫色卡框，包括装载、普通预览和咏唱模型。
        var factor = FactorCard.Create(player, "BarrierMagic", ManaSlot.Hand);
        string frame = factor.CustomFrame!.ResourcePath;
        SlotBackStore.Select(1, "0004");
        Check(frame.EndsWith("magic.tres") && factor.CustomFrame.ResourcePath == frame,
            "更换槽位背景不改变装载因子的紫色卡框");
        var chantFactor = FactorCard.Create(player, "BarrierMagic", ManaSlot.Hand, new(ManaSlot.Hand, "BarrierMagic", 1, 1, 1));
        Check(chantFactor.CustomFrame!.ResourcePath == frame && ModelDb.Card<BarrierMagic>().CustomFrame!.ResourcePath == frame,
            "咏唱因子与百科因子都保留原卡框，槽位背景由选择界面独立绘制");
        SlotBackStore.Select(1, "0002");

        LocManager.Instance.SetLanguage("zhs");
        var ui = new Control { Name = "AchievementTestUi", Size = new(1920, 1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new Color("192333"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var library = SlotBackLibrary.Open(ui, 0); await Tick();
        var grid = library.View.GetNode<Control>("LibraryCanvas/BackScroll/BackGrid");
        Check(grid.GetChildCount() == 104, "收藏界面完整显示104张卡背");
        var image = grid.GetNode<TextureButton>("Back_0004/SelectBack_0004"); image.EmitSignal(BaseButton.SignalName.Pressed);
        Check(SlotBackStore.Selected(0) == "0001", "选中预览不会提前写入保存选择");
        library.Close(); await Tick();
        Check(SlotBackStore.Selected(0) == "0001", "取消不会保存未确认的卡背选择");
        library = SlotBackLibrary.Open(ui, 0); await Tick();
        grid = library.View.GetNode<Control>("LibraryCanvas/BackScroll/BackGrid");
        grid.GetNode<TextureButton>("Back_0004/SelectBack_0004").EmitSignal(BaseButton.SignalName.Pressed);
        library.SaveAndClose(); await Tick();
        Check(SlotBackStore.Selected(0) == "0004", "点击保存后写入当前槽位选择");
        library = SlotBackLibrary.Open(ui, 0); await Tick();
        grid = library.View.GetNode<Control>("LibraryCanvas/BackScroll/BackGrid");
        var saveButton = library.View.GetNode<TextureButton>("LibraryCanvas/SaveBack");
        var tip = library.View.GetNode<Control>("LibraryCanvas/BackTooltip");
        Check(saveButton.Disabled && !tip.Visible, "未改变选择时保存灰置且不显示常驻说明");
        Check(library.View.FindChild("BackSearch", true, false) == null && library.View.FindChild("OwnedOnly", true, false) == null,
            "收藏界面沿用一代图册布局，不添加搜索和筛选控件");
        string lockedId = SlotBackCatalog.Ids.First(id => !SlotBackStore.Read().Unlocked.Contains(id));
        var locked = grid.GetNode<TextureButton>($"Back_{lockedId}/SelectBack_{lockedId}");
        Check(locked.TextureNormal.ResourcePath.EndsWith(lockedId + ".png") && Mathf.IsEqualApprox(locked.SelfModulate.A, .4f) && !locked.Disabled,
            "未解锁项保留原图40%透明度并允许悬停");
        locked.EmitSignal(BaseButton.SignalName.Pressed);
        Check(saveButton.Disabled, "未解锁卡背不能被选中");
        var backScroll = library.View.GetNode<ScrollContainer>("LibraryCanvas/BackScroll");
        backScroll.EnsureControlVisible(locked); await Tick();
        locked.EmitSignal(Control.SignalName.MouseEntered);
        Check(!tip.Visible, "提示等待一代0.2秒悬停延迟");
        await root.ToSignal(root.GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
        Check(tip.Visible && tip.GetNode<Label>("Contents/Status").Text == SlotBackLibrary.Text("BACK_LOCKED"), "悬停未解锁卡背显示名称、条件和红色状态");
        Check(Mathf.IsEqualApprox(grid.GetNode<Control>("Back_" + lockedId).Scale.X, .7f), "未解锁项悬停不放大");
        Check(tip.GetNode<Label>("Contents/Title").Position.Y + tip.GetNode<Label>("Contents/Title").Size.Y < tip.GetNode<Label>("Contents/Condition").Position.Y,
            "长名称撑高提示框，不覆盖解锁条件");
        await Snapshot("achievement-library-locked");
        locked.EmitSignal(Control.SignalName.MouseExited);
        Check(!tip.Visible, "移出卡背立即隐藏提示");
        var selected = grid.GetNode<TextureButton>("Back_0004/SelectBack_0004");
        backScroll.EnsureControlVisible(selected); await Tick();
        selected.EmitSignal(Control.SignalName.MouseEntered);
        await root.ToSignal(root.GetTree().CreateTimer(.3), SceneTreeTimer.SignalName.Timeout);
        Check(tip.Visible && tip.GetNode<Label>("Contents/Status").Text == SlotBackLibrary.Text("BACK_UNLOCKED") &&
            Mathf.IsEqualApprox(grid.GetNode<Control>("Back_0004").Scale.X, .75f), "已解锁项悬停放大至0.75并显示绿色状态");
        await Snapshot("achievement-library-hover");
        selected.EmitSignal(Control.SignalName.MouseExited);
        Check(grid.GetNode<Control>("Back_0004/SelectionGlow").Visible, "当前卡背持续显示选中光圈");
        var alternate = grid.GetNode<TextureButton>("Back_0002/SelectBack_0002");
        alternate.EmitSignal(BaseButton.SignalName.Pressed);
        Check(!saveButton.Disabled && grid.GetNode<Control>("Back_0002/SelectionGlow").Visible &&
            !grid.GetNode<Control>("Back_0004/SelectionGlow").Visible, "点击更换卡背后光圈转移并启用保存");
        Check(SlotBackStore.Selected(0) == "0004", "光圈预览不提前保存");
        selected.EmitSignal(BaseButton.SignalName.Pressed);
        Check(saveButton.Disabled, "重新选回原卡背时保存恢复灰置");
        grid.GetNode<TextureButton>("Back_0001/SelectBack_0001").EmitSignal(BaseButton.SignalName.Pressed);
        backScroll.ScrollVertical = 0; await Tick();
        Vector2 clickPoint = alternate.GetGlobalRect().GetCenter();
        root.GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = clickPoint, GlobalPosition = clickPoint }, true);
        root.GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = clickPoint, GlobalPosition = clickPoint }, true);
        await Tick();
        Check(grid.GetNode<Control>("Back_0002/SelectionGlow").Visible, "实际鼠标点击命中卡背并移动选中光圈");
        root.GetViewport().PushInput(new InputEventMouseButton { ButtonIndex = MouseButton.WheelDown, Pressed = true, Position = clickPoint, GlobalPosition = clickPoint }, true);
        await Tick();
        Check(backScroll.ScrollVertical > 0, "实际鼠标滚轮可以滚动图册");
        backScroll.ScrollVertical = 0;
        grid.GetNode<TextureButton>("Back_0001/SelectBack_0001").EmitSignal(BaseButton.SignalName.Pressed);
        await Tick();
        await Snapshot("achievement-library");
        library.Close(); await Tick();
        string drawBack = SlotBackStore.Selected(0), discardBack = SlotBackStore.Selected(2);
        library = SlotBackLibrary.Open(ui, 1); await Tick();
        library.View.GetNode<TextureButton>("LibraryCanvas/BackScroll/BackGrid/Back_0004/SelectBack_0004").EmitSignal(BaseButton.SignalName.Pressed);
        library.SaveAndClose(); await Tick();
        Check(SlotBackStore.Selected(1) == "0004" && SlotBackStore.Selected(0) == drawBack && SlotBackStore.Selected(2) == discardBack,
            "保存只改变打开的槽位，不影响另两个槽");
        // 普通入口先选择三个槽之一，保存后回到三槽预览，可连续配置全部槽位。
        library = SlotBackLibrary.Open(ui); await Tick();
        var selector = library.View.GetNode<Control>("SlotSelector");
        Check(selector.Visible && !library.View.GetNode<Control>("LibraryCanvas").Visible, "魔法书和设置入口先显示三槽选择");
        await Snapshot("achievement-three-slots");
        for (int slot = 0; slot < 3; slot++) {
            string target = new[] {"0002", "0003", "0001"}[slot];
            selector.GetNode<TextureButton>("SlotBack" + slot).EmitSignal(BaseButton.SignalName.Pressed); await Tick();
            Check(!selector.Visible, "可进入槽位收藏：" + slot);
            library.View.GetNode<TextureButton>($"LibraryCanvas/BackScroll/BackGrid/Back_{target}/SelectBack_{target}").EmitSignal(BaseButton.SignalName.Pressed);
            library.SaveAndClose(); await Tick();
            Check(selector.Visible && SlotBackStore.Selected(slot) == target, "保存对应槽位后返回三槽预览：" + slot);
        }
        library.Close(); await Tick();
        var book = new MagicBookScreen(ui, player); book.Open(); await Tick();
        Check(book.View.FindChild("OpenSlotBackLibrary", true, false) != null, "魔法书提供实际收藏入口");
        await Snapshot("achievement-slots");
        book.Close(); book.View.QueueFree(); await Tick();
        SlotBackSelectionEntry.Attach(ui, true);
        Check(ui.GetNode<Control>("FrierenSlotBackSelection").Visible, "选人页面可显示三个卡背入口");
        Check(ui.GetNode<Control>("FrierenSlotBackSelection").Position.Y < 100 && ui.GetNode<Control>("FrierenSlotBackSelection").Position.X > 1400,
            "选人页收藏入口位于右上角");
        await Snapshot("achievement-character-entry");
        var entryPanel = ui.GetNode<Control>("FrierenSlotBackSelection");
        Check(Mathf.IsEqualApprox(entryPanel.GetNode<ColorRect>("ReadabilityBackground").Color.A, .5f), "选人三槽入口保留一代半透明黑底");
        var slotPreview = entryPanel.GetNode<TextureButton>("SlotBack0");
        slotPreview.EmitSignal(Control.SignalName.MouseEntered);
        await root.ToSignal(root.GetTree().CreateTimer(.2), SceneTreeTimer.SignalName.Timeout);
        Check(slotPreview.Scale.X > 1.1f && slotPreview.GetNode<Control>("HoverGlow").Visible, "槽位悬停放大并显示绿色光圈");
        await Snapshot("achievement-character-hover");
        slotPreview.EmitSignal(Control.SignalName.MouseExited);
        SlotBackSelectionEntry.Attach(ui, false);
        Check(!ui.GetNode<Control>("FrierenSlotBackSelection").Visible, "切换其他角色时隐藏入口");
        var session = AchievementNotifications.Attach(ui);
        var endingPatch = new Harmony("frieren.tests.achievement_ending");
        endingPatch.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)),
            prefix: new HarmonyMethod(typeof(AchievementSystemTests), nameof(CombatEnding)));
        SlotBackStore.Unlock("4002"); SlotBackStore.Unlock("4007");
        Check(session.PendingCount == 2, "同时解锁时提示排队，不互相覆盖");
        await root.ToSignal(root.GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        await Snapshot("achievement-unlock");
        Check(ui.GetNode<Control>("FrierenAchievementLayer/FrierenAchievementPopup").MouseFilter == Control.MouseFilterEnum.Ignore, "成就提示不拦截输入");
        var sound = root.GetTree().Root.GetChildren().OfType<AudioStreamPlayer>()
            .FirstOrDefault(p => p.Stream?.ResourcePath.EndsWith("achievement.mp3") == true);
        Check(sound != null && sound.Playing && sound.GetPlaybackPosition() > 0, "战斗结束时成就原音效仍实际播放");
        Check(ui.GetNode<CanvasLayer>("FrierenAchievementLayer").Layer == 100, "成就弹窗使用全局前景层");
        endingPatch.UnpatchAll(endingPatch.Id);
        // 不把音频池留给测试进程退出；生产环境由 BaseLib 正常回收。
        if (sound != null) { sound.Stop(); sound.GetParent()?.RemoveChild(sound); sound.Free(); }
        ui.QueueFree(); await Tick();
        SlotBackStore.Unlock("4010");
        Check(session.PendingCount <= 1, "离开场景后清除队列并解绑解锁监听");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp();
        return count;
    }
}

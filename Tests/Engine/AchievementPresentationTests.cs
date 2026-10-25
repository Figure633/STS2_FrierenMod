using BaseLib.Config.UI;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class AchievementPresentationTests
{
    private static bool SkipStartup() => false;
    private static NOverlayStack? _stack;
    private static bool Stack(ref NOverlayStack? __result) { __result = _stack; return false; }

    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; GD.Print("PASS 成就界面集成：" + name); }
        async Task Tick() { for (int i = 0; i < 8; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Snapshot(string name) { await Tick(); if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + name + ".png")); }
        SaveManager.Instance.InitPrefsDataForTest();
        LocManager.Instance.SetLanguage("zhs");
        // 提前领取一次性纪念背景，后面单独验证指定成就的解锁提示。
        SlotBackStore.CollectLibraryRewards();
        SlotBackStore.Unlock("4015");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        RunManager.Instance.SetUpTest(RunState.CreateForTest([player], seed: "ACHIEVEMENT-PRESENTATION"), new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var patch = new Harmony("frieren.tests.achievement_presentation");
        foreach (string method in new[] { nameof(NGame._EnterTree), nameof(NGame._Ready) })
            patch.Patch(AccessTools.Method(typeof(NGame), method), prefix: new HarmonyMethod(typeof(AchievementPresentationTests), nameof(SkipStartup)));
        foreach (var type in new[] { typeof(NGlobalUi), typeof(NOverlayStack) })
            patch.Patch(AccessTools.Method(type, "_Ready"), prefix: new HarmonyMethod(typeof(AchievementPresentationTests), nameof(SkipStartup)));
        // 模拟 NGame 已就绪后才加载模组，验证主动注册而不是测试手动 Attach。
        patch.Patch(AccessTools.Method(typeof(AchievementNotifications), "Postfix"), prefix: new HarmonyMethod(typeof(AchievementPresentationTests), nameof(SkipStartup)));
        var game = new NGame { Size = new(1920, 1080), ProcessMode = Node.ProcessModeEnum.Disabled };
        var sceneContainer = new NSceneContainer(); game.AddChild(sceneContainer);
        AccessTools.Property(typeof(NGame), nameof(NGame.RootSceneContainer)).SetValue(game, sceneContainer);
        var input = new NInputManager(); var controller = new NControllerManager();
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        root.AddChild(game); AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        Check(!game.HasNode("FrierenAchievementLayer"), "宿主先于模组初始化时没有预先注册提示");
        AchievementNotifications.Initialize(); AchievementNotifications.Initialize();
        Check(game.GetChildren().Count(n => n.Name == "FrierenAchievementLayer") == 1, "加载时主动注册提示，重复初始化不重复订阅");
        var host = new NGlobalUi { Size = new(1920, 1080) };
        var map = new NMapScreen(); var runNode = new NRun(); var room = new NSceneContainer();
        AccessTools.Field(typeof(NRun), "_roomContainer").SetValue(runNode, room);
        AccessTools.Property(typeof(NRun), nameof(NRun.GlobalUi)).SetValue(runNode, host);
        AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(sceneContainer, runNode);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.MapScreen)).SetValue(host, map);
        _stack = new NOverlayStack { Size = host.Size };
        var backstop = new Control { Name = "OverlayBackstop", MouseFilter = Control.MouseFilterEnum.Ignore };
        _stack.AddChild(backstop); AccessTools.Field(typeof(NOverlayStack), "_backstop").SetValue(_stack, backstop); host.AddChild(_stack);
        var capstone = new NCapstoneContainer { Size = host.Size }; capstone.AddChild(new Control { Name = "CapstoneBackstop" }); host.AddChild(capstone);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.CapstoneContainer)).SetValue(host, capstone);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.Overlays)).SetValue(host, _stack);
        var topBar = new NTopBar { Size = host.Size };
        topBar.AddChild(new Control { Name = "BgImage", Size = new(1920, 100) });
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.TopBar)).SetValue(host, topBar);
        // 与原生 run.tscn 一样，顶栏绘制在 Capstone 后；背景高度为100，根节点铺满窗口。
        host.AddChild(new ColorRect { Name = "TopBarMask", Size = new(1920, 100), Color = new Color("36302a"), MouseFilter = Control.MouseFilterEnum.Ignore });
        root.AddChild(host);
        patch.Patch(AccessTools.PropertyGetter(typeof(NOverlayStack), nameof(NOverlayStack.Instance)), prefix: new HarmonyMethod(typeof(AchievementPresentationTests), nameof(Stack)));
        var book = new MagicBookScreen(host, player); book.Open(); await Tick();
        var entry = (NConfigButton)book.View.FindChild("OpenSlotBackLibrary", true, false);
        entry.EmitSignal(NClickableControl.SignalName.Released, entry); await Tick();
        var library = (MagicBookView)capstone.CurrentCapstoneScreen!;
        Check(library.Name == "FrierenSlotBackLibrary" && library.GetNode<Control>("SlotSelector").Visible, "实际魔法书按钮进入三槽选择并由原生 Capstone 管理");
        library.GetNode<TextureButton>("SlotSelector/SlotBack2").EmitSignal(BaseButton.SignalName.Pressed); await Tick();
        var progress = library.GetNode<Label>("LibraryCanvas/CollectionProgress");
        Check(progress.GetGlobalRect().Position.Y > 100, "收藏进度位于原生顶栏下面，不被后绘制的顶栏遮挡");
        await Snapshot("achievement-in-run");
        var tip = library.GetNode<Control>("LibraryCanvas/BackTooltip");
        var grid = library.GetNode<Control>("LibraryCanvas/BackScroll/BackGrid");
        foreach (string id in new[] { "1001", "4015" }) {
            var card = grid.GetNode<TextureButton>($"Back_{id}/SelectBack_{id}");
            library.GetNode<ScrollContainer>("LibraryCanvas/BackScroll").EnsureControlVisible(card); await Tick();
            card.EmitSignal(Control.SignalName.MouseEntered);
            library.GetNode<Godot.Timer>("LibraryCanvas/HoverDelay").EmitSignal(Godot.Timer.SignalName.Timeout); await Tick();
            Check(Mathf.IsEqualApprox(tip.Size.X, 320), "一代提示框320基准宽度：" + id);
            Check(tip.GetNode<Label>("Contents/Title").GetThemeFontSize("font_size") == 23 &&
                tip.GetNode<Label>("Contents/Condition").GetThemeFontSize("font_size") == 22, "一代标题与正文的23/22字号");
            await Snapshot("achievement-legacy-tip-" + id);
            card.EmitSignal(Control.SignalName.MouseExited);
        }
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            var samples = SlotBackCatalog.Ids.OrderByDescending(id => SlotBackLibrary.Text($"BACK.{id}.title").Length).Take(4)
                .Concat(SlotBackCatalog.Ids.OrderByDescending(id => SlotBackLibrary.Text($"BACK.{id}.description").Length).Take(4)).Distinct();
            foreach (string id in samples) {
                var card = grid.GetNode<TextureButton>($"Back_{id}/SelectBack_{id}");
                library.GetNode<ScrollContainer>("LibraryCanvas/BackScroll").EnsureControlVisible(card); await Tick();
                card.EmitSignal(Control.SignalName.MouseEntered);
                library.GetNode<Godot.Timer>("LibraryCanvas/HoverDelay").EmitSignal(Godot.Timer.SignalName.Timeout); await Tick();
                Check(tip.GetNode<VBoxContainer>("Contents").GetChildren().OfType<Label>().All(l => tip.GetGlobalRect().Grow(.5f).Encloses(l.GetGlobalRect())),
                    "最长标题/条件不出框：" + language + "/" + id);
                Check(tip.Size.Y < 600, "提示框按文字收缩，不保留初始窄行的过高布局：" + language + "/" + id);
                card.EmitSignal(Control.SignalName.MouseExited);
            }
        }
        LocManager.Instance.SetLanguage("zhs");
        library.GetNode<TextureButton>("LibraryCanvas/CancelBack").EmitSignal(BaseButton.SignalName.Pressed); await Tick();
        Check(library.GetNode<Control>("SlotSelector").Visible, "收藏退出回到三槽预览");
        library.GetNode<TextureButton>("SlotSelector/ExitSelector").EmitSignal(BaseButton.SignalName.Pressed); await Tick();
        Check(capstone.CurrentCapstoneScreen == book.View && book.View.Visible, "退出收藏后恢复原魔法书");
        SlotBackStore.Unlock("0006"); await Tick();
        await root.ToSignal(root.GetTree().CreateTimer(.8), SceneTreeTimer.SignalName.Timeout);
        var popup = game.GetNode<Control>("FrierenAchievementLayer/FrierenAchievementPopup");
        Check(popup.IsVisibleInTree() && popup.GetGlobalRect().Position.Y > 0, "通过加载时注册的队列真实弹出新成就");
        Check(popup.FindChildren("*", "Label", true, false).OfType<Label>().Any(l => l.Text == SlotBackLibrary.Text("BACK.0006.title")), "弹窗显示本次实际解锁的成就");
        Check(popup.FindChildren("*", "Label", true, false).OfType<Label>().All(l => popup.GetGlobalRect().Grow(.5f).Encloses(l.GetGlobalRect())), "成就弹窗标题与解锁条件都在框内");
        await Snapshot("achievement-popup-in-run");
        book.Close(); host.QueueFree(); await Tick();
        foreach (var sound in root.GetTree().Root.GetChildren().OfType<AudioStreamPlayer>().Where(p => p.Stream?.ResourcePath.EndsWith("achievement.mp3") == true).ToArray()) {
            sound.Stop(); sound.GetParent()?.RemoveChild(sound); sound.Free();
        }
        AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(sceneContainer, null);
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        _stack = null; topBar.Free(); map.Free(); room.Free(); runNode.Free(); controller.Free(); input.Free(); game.QueueFree();
        await Tick(); patch.UnpatchAll(patch.Id); RunManager.Instance.CleanUp(); return count;
    }
}

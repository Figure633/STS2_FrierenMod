using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class OpeningEventTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 开局事件：" + name); count++; }
        async Task Tick(int frames = 8) { for (int i = 0; i < frames; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        Player Copy(Player player) => Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(
            JsonSerializer.Serialize(player.ToSerializable(), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!);
        RunState Start(params Player[] players) {
            var run = RunState.CreateForTest(players, seed: "FRIEREN-OPENING-EVENT");
            RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
            ((RoomSet)AccessTools.Field(typeof(ActModel), "_rooms").GetValue(run.Act)!).Ancient = ModelDb.AncientEvent<Neow>();
            return run;
        }
        AbstractRoom Create() => (AbstractRoom)AccessTools.Method(typeof(RunManager), "CreateRoom").Invoke(RunManager.Instance,
            [RoomType.Event, MapPointType.Ancient, ModelDb.AncientEvent<Neow>()])!;
        async Task<ReadingGrimoireEvent> Enter(RunState run) {
            var room = Create(); run.AppendToMapPointHistory(MapPointType.Ancient, RoomType.Event, room.ModelId);
            await RunManager.Instance.EnterRoom(room);
            return (ReadingGrimoireEvent)RunManager.Instance.EventSynchronizer.GetLocalEvent();
        }
        async Task Choose() {
            RunManager.Instance.EventSynchronizer.ChooseLocalOption(0);
            await RunManager.Instance.EventSynchronizer.AwaitPendingOptionTasks(); await Tick();
        }
        var manager = RunManager.Instance;
        SaveManager.Instance.InitPrefsDataForTest();
        var canonical = ModelDb.Event<ReadingGrimoireEvent>();
        Check(canonical.Id.Entry == "FRIERENMOD-READING_GRIMOIRE_EVENT" && !canonical.IsAllowed(NullRunState.Instance) &&
            !ModelDb.AllSharedEvents.Contains(canonical), "新事件注册但不进入随机事件池");
        foreach (var player in new[] { Player.CreateForNewRun<Ironclad>(UnlockState.all, 1), Player.CreateForNewRun<Frieren>(UnlockState.all, 1) }) {
            var run = Start(player);
            Check((Create() is EventRoom { CanonicalEvent: ReadingGrimoireEvent }) == (player.Character is Frieren), "只替换芙莉莲的涅奥前置入口");
            manager.CleanUp(); await Tick();
        }
        foreach (var player in new[] { Player.CreateForNewRun<Ironclad>(UnlockState.none, 1), Player.CreateForNewRun<Frieren>(UnlockState.none, 1) }) {
            var fresh = Start(player);
            Check(fresh.ExtraFields.StartedWithNeow == (player.Character is Frieren), "未解锁涅奥的新档仅为芙莉莲保留魔导书开局");
            if (player.Character is Frieren) {
                var blessing = ModelDb.AncientEvent<Neow>().ToMutable(); await blessing.BeginEvent(player, false);
                Check(!blessing.IsFinished && blessing.CurrentOptions.Count > 0, "未解锁涅奥的新档也有可选的原版祝福");
            }
            manager.CleanUp(); await Tick();
        }
        var mixed = Start(Player.CreateForNewRun<Frieren>(UnlockState.all, 1), Player.CreateForNewRun<Ironclad>(UnlockState.all, 2));
        Check(Create() is EventRoom { CanonicalEvent: Neow }, "多人混合角色不替换全队的原版开局");
        manager.CleanUp(); await Tick();

        var selector = new TestCardSelector(); using var choices = CardSelectCmd.UseSelector(selector);
        var owner = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); var state = Start(owner); var story = await Enter(state);
        Check(((RoomSet)AccessTools.Field(typeof(ActModel), "_rooms").GetValue(state.Act)!).eventsVisited == 0,
            "前置事件不会提前消耗后续普通事件序列");
        Check(story.Description!.LocEntryKey.Contains("BOOK_0") && ReadingGrimoireEntrance.Stage.Get(owner) == 1, "初次进入第一本魔导书页面");
        var context = new BlockingPlayerChoiceContext();
        await owner.GetRelic<HolyEmblem>()!.AfterActEntered(); MagicDeckCmd.TryResumeStartingFactors(owner);
        Check(MagicDeckStore.Read(owner).Draft.Length == 0, "圣徽和界面兜底不抢先选择或跳过事件");

        var pending = selector.SetupForAsyncIndexSelection(); manager.EventSynchronizer.ChooseLocalOption(0); await Tick();
        var draft = MagicDeckStore.Read(owner); var restored = Copy(owner);
        Check(draft.Draft.Length == 3 && draft.DraftSlot == 0, "第一本选择等待时保留完整候选");
        manager.CleanUp(); await Tick(); pending.TrySetResult([2]); selector.Cleanup();
        state = Start(restored); story = await Enter(state);
        Check(MagicDeckStore.Read(restored).ToJson() == draft.ToJson() && ReadingGrimoireEntrance.Stage.Get(restored) == 1,
            "中途退出后真实玩家序列化恢复原候选与事件标记");
        selector.PrepareToSelect(new[] { 1 }); await Choose();
        Check(MagicDeckStore.Read(restored).Items.Single().SpellId == draft.Draft[1] && story.Description!.LocEntryKey.Contains("BOOK_1"),
            "第一本只选一张，接着显示第二本");
        selector.PrepareToSelect(new[] { 0 }); await Choose();
        Check(MagicDeckStore.Read(restored).Items.Count == 2 && story.Description!.LocEntryKey.Contains("BOOK_2"), "第二本后保留第三本按钮");
        selector.PrepareToSelect(new[] { 2 }); await Choose();
        Check(MagicDeckStore.Read(restored).SetupCompleted && MagicDeckStore.Read(restored).Props.Count == 0 &&
            story.Description!.LocEntryKey.Contains("TOOLS"), "第三本完成后先显示领取工具，没有提前赠送");
        MagicDeckCmd.TryResumeStartingFactors(restored); await restored.GetRelic<HolyEmblem>()!.AfterActEntered();
        Check(MagicDeckStore.Read(restored).Props.Count == 0, "三槽完成后兜底也不会越过领取工具步骤");
        restored = Copy(restored); manager.CleanUp(); await Tick(); state = Start(restored); story = await Enter(state);
        Check(story.Description!.LocEntryKey.Contains("TOOLS") && MagicDeckStore.Read(restored).Items.Count == 3, "第三本之后读档仍回到领取工具页");
        await Choose();
        var complete = MagicDeckStore.Read(restored); string completeJson = complete.ToJson();
        Check(complete.Props.Count == 2 && complete.Props.All(p => p.PropId == "UnbelievableTool") && story.CurrentOptions.Single().IsProceed,
            "只获得两个妙妙工具，出发使用原生 Proceed 路径");
        restored = Copy(restored); manager.CleanUp(); await Tick(); state = Start(restored); story = await Enter(state);
        Check(story.Description!.LocEntryKey.Contains("DONE") && MagicDeckStore.Read(restored).ToJson() == completeJson,
            "领取后读档保留出发页面，不重复赠品");

        // 真实原版房间与事件布局：检查图片、双语正文和原生按钮，不替换玩家存档。
        var game = new NGame(); var scenes = new NSceneContainer(); var runNode = new NRun(); var roomContainer = new NSceneContainer();
        var global = new NGlobalUi(); var map = new NMapScreen(); var overlays = new NOverlayStack(); var capstone = new NCapstoneContainer();
        var topBar = new NTopBar(); var roomIcon = new MegaCrit.sts2.Core.Nodes.TopBar.NTopBarRoomIcon();
        var hp = new MegaCrit.sts2.Core.Nodes.TopBar.NTopBarHp();
        var controller = new NControllerManager(); var input = new NInputManager(); var shake = new NScreenShake();
        AccessTools.Property(typeof(NGame), nameof(NGame.RootSceneContainer)).SetValue(game, scenes);
        AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(scenes, runNode);
        AccessTools.Field(typeof(NRun), "_roomContainer").SetValue(runNode, roomContainer);
        AccessTools.Property(typeof(NRun), nameof(NRun.GlobalUi)).SetValue(runNode, global);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.TopBar)).SetValue(global, topBar);
        AccessTools.Property(typeof(NTopBar), nameof(NTopBar.RoomIcon)).SetValue(topBar, roomIcon);
        AccessTools.Property(typeof(NTopBar), nameof(NTopBar.Hp)).SetValue(topBar, hp);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.MapScreen)).SetValue(global, map);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.Overlays)).SetValue(global, overlays);
        AccessTools.Property(typeof(NGlobalUi), nameof(NGlobalUi.CapstoneContainer)).SetValue(global, capstone);
        AccessTools.Property(typeof(NInputManager), nameof(NInputManager.ControllerManager)).SetValue(input, controller);
        AccessTools.Property(typeof(NGame), nameof(NGame.InputManager)).SetValue(game, input);
        AccessTools.Field(typeof(NGame), "_screenShake").SetValue(game, shake);
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, game);
        await Tick();
        var hoverHost = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.GetTree().Root.AddChild(hoverHost);
        AccessTools.Property(typeof(NGame), nameof(NGame.HoverTipsContainer)).SetValue(game, hoverHost);
        // 本夹具只复原事件房间，不启动全局浮窗布局；正文与按钮保留真实鼠标路径。
        bool previousHoverBlock = NHoverTipSet.shouldBlockHoverTips; NHoverTipSet.shouldBlockHoverTips = true;
        if (!InputMap.HasAction(DebugHotkey.hideEventUi)) InputMap.AddAction(DebugHotkey.hideEventUi);
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (int progress in language == "zhs" ? new[] { 0, 1, 2, 3, 4 } : new[] { 0, 4 }) {
            var previewDeck = new MagicDeckState();
            for (int i = 0; i < Math.Min(progress, 3); i++) { previewDeck.PrepareDraft(_ => 0); previewDeck.SelectInitial(previewDeck.Draft[0]); }
            if (progress == 4) previewDeck.GrantStartingProps();
            MagicDeckStore.Write(restored, previewDeck);
            AccessTools.Property(typeof(EventModel), nameof(EventModel.IsFinished)).SetValue(manager.EventSynchronizer.GetLocalEvent(), true);
            manager.EventSynchronizer.BeginEvent(canonical);
            var page = manager.EventSynchronizer.GetLocalEvent(); story = (ReadingGrimoireEvent)page;
            var host = new Control { Size = new(1920, 1080) }; root.GetTree().Root.AddChild(host);
            var view = GD.Load<PackedScene>(NEventRoom.AssetPaths.Single()).Instantiate<NEventRoom>();
            AccessTools.Field(typeof(NEventRoom), "_event").SetValue(view, page);
            AccessTools.Field(typeof(NEventRoom), "_runState").SetValue(view, state);
            AccessTools.Property(typeof(NSceneContainer), nameof(NSceneContainer.CurrentScene)).SetValue(roomContainer, view);
            host.AddChild(view); await Tick();
            await root.ToSignal(root.GetTree().CreateTimer(2.2), SceneTreeTimer.SignalName.Timeout);
            Check(view.Layout is NEventLayout && view.Layout.GetNode<VBoxContainer>("%OptionsContainer").GetChildCount() == 1,
                language + "实际原版事件布局显示阶段按钮：" + progress);
            Check(page.CreateInitialPortrait().GetWidth() == 600 && !page.Description!.GetFormattedText().Contains("LOC_ERR"),
                language + "一代事件原图与完整本地化可用");
            var description = view.Layout!.GetNode<Control>("%EventDescription");
            var button = view.Layout.GetNode<VBoxContainer>("%OptionsContainer").GetChild<Control>(0);
            Check(description.IsVisibleInTree() && description.Modulate.A > 0.99f && button.IsVisibleInTree() &&
                root.GetViewport().GetVisibleRect().Encloses(button.GetGlobalRect()), "原版入场动画结束后正文和按钮完整可见");
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/opening-event-" + language + "-" + progress + ".png"));
            if (language == "zhs" && progress == 0) {
                selector.PrepareToSelect(new[] { 1 });
                var point = button.GetGlobalRect().GetCenter();
                root.GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true); await Tick();
                root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true }, true); await Tick();
                root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
                await Tick(); await manager.EventSynchronizer.AwaitPendingOptionTasks();
                Check(MagicDeckStore.Read(restored).DraftSlot == 1 && page.Description!.LocEntryKey.Contains("BOOK_1"),
                    "实际鼠标点击原版事件按钮，经同步器完成第一本并刷新第二本");
            }
            if (language == "eng" && progress == 4) {
                MagicDeckStore.Write(restored, complete);
                var point = button.GetGlobalRect().GetCenter();
                root.GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true); await Tick();
                root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = true }, true); await Tick();
                root.GetViewport().PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point, ButtonIndex = MouseButton.Left, Pressed = false }, true);
                ulong deadline = Time.GetTicksMsec() + 5000;
                while (state.CurrentRoom is not EventRoom { CanonicalEvent: Neow } && Time.GetTicksMsec() < deadline) await Tick();
                Check(story.IsFinished && state.CurrentRoom is EventRoom { CanonicalEvent: Neow } && manager.EventSynchronizer.GetLocalEvent() is Neow,
                    "保留实际房间界面点击出发，结束页刷新及切房不报错、不死锁");
            }
            host.QueueFree(); await Tick();
            }
        }
        MagicDeckStore.Write(restored, complete);
        hoverHost.QueueFree(); await Tick();
        NHoverTipSet.shouldBlockHoverTips = previousHoverBlock;
        AccessTools.Property(typeof(NGame), nameof(NGame.Instance)).SetValue(null, null);
        scenes.Free(); runNode.Free(); roomContainer.Free(); global.Free(); map.Free(); overlays.Free(); capstone.Free();
        topBar.Free(); roomIcon.Free(); hp.Free();
        input.Free(); controller.Free(); shake.Free(); game.Free();

        Check(manager.EventSynchronizer.GetLocalEvent().CurrentOptions.Count > 0 && ReadingGrimoireEntrance.Stage.Get(restored) == 2 &&
            MagicDeckStore.Read(restored).ToJson() == completeJson, "原版祝福选项正常生成且魔法书不改变");
        Check(((RoomSet)AccessTools.Field(typeof(ActModel), "_rooms").GetValue(state.Act)!).eventsVisited == 1,
            "进入涅奥仅保留原版的一次事件访问计数");
        Check(Create() is EventRoom { CanonicalEvent: Neow }, "再次创建开局房间不重复前置事件");
        ReadingGrimoireEntrance.Stage.Set(restored, 0);
        Check(Create() is EventRoom { CanonicalEvent: Neow }, "已经完成初始因子的旧版本存档不会重插前置事件");
        manager.CleanUp(); await Tick(); selector.Cleanup(); LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

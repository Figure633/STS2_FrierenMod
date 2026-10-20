using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Localization;
using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using HarmonyLib;

public partial class Runner : Node
{
    private static readonly MegaCrit.Sts2.Core.Nodes.CommonUi.NHotkeyManager TestHotkeys = new();
    private static bool GetTestHotkeys(ref MegaCrit.Sts2.Core.Nodes.CommonUi.NHotkeyManager __result)
    { __result = TestHotkeys; return false; }

    private static bool SkipGlobalHoverWindow() => false;

    private async Task Finish()
    {
        for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // 卡牌悬停仍可能持有补间；先停止并让引擎移除，再释放对象池。
        foreach (var tween in GetTree().GetProcessedTweens()) tween.Kill();
        for (int i = 0; i < 2; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // 游戏的静态对象池不随测试场景销毁；在引擎关闭前显式释放独立测试缓存。
        var pools = (System.Collections.IDictionary)AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Pooling.NodePool), "_pools").GetValue(null);
        foreach (var pool in pools.Values) {
            var nodes = (System.Collections.IEnumerable)pool.GetType().GetProperty("DebugFreeObjects")!.GetValue(pool)!;
            foreach (Node node in nodes) {
                if (!GodotObject.IsInstanceValid(node)) continue;
                if (node is MegaCrit.Sts2.Core.Nodes.Cards.NCard card && card.IsNodeReady()) card.CardHighlight.AnimHideInstantly();
                node.Free();
            }
        }
        pools.Clear();
        TestHotkeys.Free();
        for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // 在 Godot 原生对象仍可用时清理托管资源，避免引擎退出后才终结悬停材质与纹理。
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GetTree().Quit();
    }

    public override async void _Ready()
    {
        try
        {
            bool reviewedOnly = OS.GetCmdlineUserArgs().Contains("--reviewed-only");
            if (reviewedOnly) GD.Print("REVIEWED: 初始化测试运行环境");
            // 原生 TestMode 将 SaveManager 重定向至内存 MockGodotFileIo，不读写玩家文件。
            TestMode.IsOn = true;
            // 构建脚本从 local.props 读取位置；不依赖开发者电脑的固定安装目录。
            var gameDir = System.Environment.GetEnvironmentVariable("FRIEREN_STS2_DIR")
                ?? throw new Exception("请通过 tools/build.ps1 -Pack -EngineTests 启动测试。");
            var baseLibDir = System.Environment.GetEnvironmentVariable("FRIEREN_BASELIB_DIR")
                ?? throw new Exception("缺少 FRIEREN_BASELIB_DIR，请通过构建脚本启动测试。");
            foreach (var pack in new[] { Path.Combine(gameDir, "SlayTheSpire2.pck"), Path.Combine(baseLibDir, "BaseLib.pck") })
                if (!ProjectSettings.LoadResourcePack(pack))
                    throw new Exception("无法加载测试依赖资源包：" + pack);
            if (!ProjectSettings.LoadResourcePack(ProjectSettings.GlobalizePath("res://../../artifacts/FrierenMod/FrierenMod.pck")))
                throw new Exception("先打包 FrierenMod.pck 再运行引擎测试。");
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(typeof(ModelDb).Assembly);
            var mods = (List<Mod>)ModManager.Mods;
            mods.Add(new Mod { path = "test", assembly = typeof(BaseLib.BaseLibMain).Assembly, state = ModLoadState.Loaded,
                manifest = new ModManifest { id = "BaseLib" } });
            mods.Add(new Mod { path = "test", assembly = typeof(Mana).Assembly, state = ModLoadState.Loaded,
                manifest = new ModManifest { id = "FrierenMod" } });
            AccessTools.Property(typeof(ModManager), nameof(ModManager.State)).SetValue(null, ModManagerState.Initialized);
            BaseLib.BaseLibMain.Initialize();
            FrierenMod.Scripts.Entry.Init();
            ModelDb.Init();
            ModelIdSerializationCache.Init();
            ModelDb.InitIds();
            if (reviewedOnly) GD.Print("REVIEWED: 模型已加载");
            SaveManager.Instance.InitSettingsData();
            SaveManager.Instance.SettingsSave.Language = "eng";
            LocManager.Initialize();
            if (reviewedOnly) GD.Print("REVIEWED: 本地化已加载");
            MegaCrit.Sts2.Core.Nodes.Pooling.NodePool.Init<MegaCrit.Sts2.Core.Nodes.Cards.NCard>("res://scenes/cards/card.tscn", 0);
            MegaCrit.Sts2.Core.Nodes.Pooling.NodePool.Init<MegaCrit.Sts2.Core.Nodes.Cards.Holders.NGridCardHolder>("res://scenes/cards/holders/grid_card_holder.tscn", 0);
            new Harmony("frieren.tests.hotkeys").Patch(
                AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Nodes.CommonUi.NHotkeyManager), "Instance"),
                prefix: new HarmonyMethod(typeof(Runner), nameof(GetTestHotkeys)));
            // 独立场景没有完整 NGame 的全局浮窗宿主；仍执行真实卡牌焦点和鼠标路径。
            // 悬停规则正文由 CardUiAudit 逐卡检查；不把浮窗视觉算作此测试覆盖。
            new Harmony("frieren.tests.hover_host").Patch(
                AccessTools.Method(typeof(MegaCrit.Sts2.Core.Nodes.Cards.Holders.NCardHolder), "CreateHoverTips"),
                prefix: new HarmonyMethod(typeof(Runner), nameof(SkipGlobalHoverWindow)));
            if (OS.GetCmdlineUserArgs().Contains("--ui-todos-only")) {
                GD.Print("UI_TODOS_OK: " + await UiTodoTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--frames-only")) {
                GD.Print("CARD_FRAMES_OK: " + await CardFrameTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--prop-cancel-only")) {
                GD.Print("PROP_CANCEL_OK: " + await MagicBookSelectionTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--synchro-display-only")) {
                GD.Print("SYNCHRO_DISPLAY_OK: " + await SynchroDisplayTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--chant-selection-only")) {
                GD.Print("CHANT_SELECTION_OK: " + await ChantSelectionTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--generated-cards-only")) {
                GD.Print("GENERATED_CARDS_OK: " + await GeneratedCardTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--play-feedback-only")) {
                GD.Print("PLAY_FEEDBACK_OK: " + await PlayFeedbackTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--card-ids-only")) {
                GD.Print("CARD_IDS_OK: " + await CardIdTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--reviewed-only")) {
                GD.Print("REVIEWED_TESTS_OK: " + await ReviewedCardTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--missing-only")) {
                GD.Print("MISSING_TESTS_OK: " + await MissingCardTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magicbook-only")) {
                GD.Print("MAGICBOOK_TESTS_OK: " + await MagicShopTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--ending-only")) {
                GD.Print("ENDING_TESTS_OK: " + await EndingFlowTests.Run(this));
                await Finish(); return;
            }
            int count = await CombatTests.Run();
            var player = MegaCrit.Sts2.Core.Runs.RunManager.Instance.DebugOnlyGetState()!.Players[0];
            count += PuzzlePresentationTests.Run(player);
            count += await CardUiAudit.Run(this, player);
            count += await PresentationTests.Run(this, player);
            var ui = new Control { Size = new Vector2(1920, 1080) };
            AddChild(ui);
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                ui.AddChild(new ColorRect { Size = ui.Size, Color = new Color("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
                var character = ((FrierenMod.Scripts.Characters.Frieren)player.Character).CreateVisuals();
                character.Position = new Vector2(430, 750);
                ui.AddChild(character);
            }
            FrierenMod.Scripts.UI.MagicBookUi.Attach(ui, player);
            ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
            var hud = ui.GetNode<Control>("FrierenManaCounter");
            if (!hud.Visible) throw new Exception("战斗魔力面板未显示。");
            if (hud.Size.X > 120 || hud.Size.Y > 280 || hud.FindChildren("*", "Label", true, false).Count != 6)
                throw new Exception("魔力计数器出现常驻长说明或占用过多画面。");
            foreach (var slot in Enum.GetValues<FrierenMod.Core.ManaSlot>()) {
                var area = hud.GetNode<Control>("Slot" + slot);
                int actual = FrierenMod.Scripts.Mechanics.ManaCmd.InPile(player, FrierenMod.Scripts.Mechanics.ManaCmd.Pile(slot)).Count();
                if (area.GetNode<Label>("Count").Text != actual.ToString())
                    throw new Exception("槽位魔力计数或悬停说明缺失：" + slot);
            }
            ((FrierenMod.Scripts.UI.MagicFactorButton)ui.FindChild("FrierenMagicBookButton", true, false)).EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Released, ui.FindChild("FrierenMagicBookButton", true, false));
            var book = ui.GetNode<Control>("FrierenMagicBook");
            if (!book.Visible) throw new Exception("魔法书按钮未打开面板。");
            if (hud.Visible || !ui.GetNode<Control>("FrierenBookBackdrop").Visible ||
                ui.GetNode<Control>("FrierenBookBackdrop").MouseFilter != Control.MouseFilterEnum.Stop)
                throw new Exception("魔法书打开后未隐藏计数器或未阻止点击穿透。");
            if (!new Rect2(Vector2.Zero, ui.Size).Encloses(book.GetRect()) ||
                !new Rect2(Vector2.Zero, ui.Size).Encloses(((Control)ui.FindChild("FrierenMagicBookButton", true, false)).GetGlobalRect()))
                throw new Exception("魔法书或按钮超出界面边界：" + book.GetRect() + ", " + ((Control)ui.FindChild("FrierenMagicBookButton", true, false)).GetGlobalRect());
            if (book.FindChildren("*", "OptionButton", true, false).Count != 0)
                throw new Exception("临时免费换槽入口未移除。");
            foreach (var size in new[] { new Vector2(1280, 720), new Vector2(2560, 1440), new Vector2(1920, 1080) }) {
                ui.Size = size;
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!new Rect2(Vector2.Zero, size).Encloses(book.GetRect()) || !new Rect2(Vector2.Zero, size).Encloses(hud.GetRect()))
                    throw new Exception("魔法书或魔力计数器超出界面：" + size);
            }
            AccessTools.Property(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "IsInProgress").SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance, false);
            ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
            GD.Print("PASS 原生 Godot UI：紧凑计数与悬停说明、魔法书遮罩、槽位只读"); count++;
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            {
                // 独立测试场景截图，仅包含本模组新 UI。
                for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/magic-book.png"));
                book.Hide();
                AccessTools.Property(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "IsInProgress").SetValue(MegaCrit.Sts2.Core.Combat.CombatManager.Instance, true);
                ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
                for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/mana-counter.png"));
            }
            ui.GetNode<Control>("FrierenBookBackdrop").EmitSignal(Control.SignalName.GuiInput,
                new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true });
            if (book.Visible || ui.GetNode<Control>("FrierenBookBackdrop").Visible)
                throw new Exception("点击魔法书外部未关闭面板和遮罩。");
            ((FrierenMod.Scripts.UI.MagicFactorButton)ui.FindChild("FrierenMagicBookButton", true, false)).EmitSignal(MegaCrit.Sts2.Core.Nodes.GodotExtensions.NClickableControl.SignalName.Released, ui.FindChild("FrierenMagicBookButton", true, false));
            GetViewport().PushInput(new InputEventKey { Keycode = Key.Escape, Pressed = true });
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (book.Visible) throw new Exception("Esc 未关闭魔法书。");
            ui.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            count += await RestSiteTests.Run();
            count += await MagicShopTests.Run(this);
            count += await EndingFlowTests.Run(this);
            count += await MissingCardTests.Run(this);
            count += await ReviewedCardTests.Run(this);
            count += await UiTodoTests.Run(this);
            GD.Print($"ENGINE_TESTS_OK: {count}");
            await Finish();
        }
        catch (Exception ex) { GD.PrintErr(ex.ToString()); GetTree().Quit(1); }
    }
}

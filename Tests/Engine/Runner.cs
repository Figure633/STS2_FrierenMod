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
    private static bool ReleasePaths(ref string[] __result)
    {
        __result=[System.Environment.GetEnvironmentVariable("FRIEREN_RELEASE_INFO") ??
            Path.Combine(System.Environment.GetEnvironmentVariable("FRIEREN_STS2_DIR")!,"release_info.json")];
        return false;
    }
    private static Mod TestMod(string id, System.Reflection.Assembly assembly, ModManifest manifest)
    {
        var mod=new Mod {path="test",state=ModLoadState.Loaded,manifest=manifest};
        if(AccessTools.Field(typeof(Mod),"assemblies") is {} many) many.SetValue(mod,new List<System.Reflection.Assembly>{assembly});
        else AccessTools.Field(typeof(Mod),"assembly").SetValue(mod,assembly);
        return mod;
    }

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
            mods.Add(TestMod("BaseLib",typeof(BaseLib.BaseLibMain).Assembly,new ModManifest{id="BaseLib"}));
            mods.Add(TestMod("FrierenMod",typeof(Mana).Assembly,new ModManifest {id="FrierenMod",dependencies=[new ModDependency("BaseLib","3.4.7")]}));
            AccessTools.Property(typeof(ModManager), nameof(ModManager.State)).SetValue(null, ModManagerState.Initialized);
            AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.AssemblyInfo")?.GetMethod("Init")?.Invoke(null,null);
            new Harmony("frieren.tests.release_info").Patch(
                AccessTools.Method(typeof(MegaCrit.Sts2.Core.Debug.ReleaseInfoManager),"GetPossibleReleaseInfoPaths"),
                prefix:new HarmonyMethod(typeof(Runner),nameof(ReleasePaths)));
            BaseLib.BaseLibMain.Initialize();
            FrierenMod.Scripts.Entry.Init();
            var initModels=AccessTools.Method(typeof(ModelDb),"Init");
            initModels.Invoke(null,initModels.GetParameters().Length==0?null:[null]);
            ModelIdSerializationCache.Init();
            ModelDb.InitIds();
            GD.Print("NATIVE_SAVE_OK: " + NativeSaveTests.Run());
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
            if (OS.GetCmdlineUserArgs().Contains("--jojo-only")) {
                GD.Print("JOJO_OK: " + await JojoTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--serie-only")) {
                GD.Print("SERIE_OK: " + await SerieTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-01215-only")) {
                GD.Print("AUTHOR01215_OK: " + await Author01215Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--ready-keywords-only")) {
                GD.Print("READY_KEYWORDS_OK: " + await Author01214Tests.Run(this, readyAndKeywordsOnly: true));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-01214-only")) {
                GD.Print("AUTHOR01214_OK: " + await Author01214Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--slot-back-unlocks-only")) {
                GD.Print("SLOT_BACK_UNLOCKS_OK: " + await SlotBackUnlockTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-todos-only")) {
                GD.Print("AUTHOR_TODOS_OK: " + await AuthorTodoTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-review-only")) {
                GD.Print("AUTHOR_REVIEW_OK: " + await AuthorReviewTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--sts2-slot-backs-only")) {
                GD.Print("STS2_SLOT_BACKS_OK: " + Sts2SlotBackTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-text-01210-only")) {
                GD.Print("AUTHOR_TEXT_01210_OK: " + await AuthorText01210Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--bird-grimoire-only")) {
                GD.Print("BIRD_GRIMOIRE_OK: " + await BirdGrimoireTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--hollow-knight-only")) {
                GD.Print("HOLLOW_KNIGHT_OK: " + await HollowKnightTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--event-art-preview-only")) {
                GD.Print("EVENT_ART_PREVIEW_OK: " + await EventArtPreviewTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--card-art-audit-only")) {
                GD.Print("CARD_ART_AUDIT_OK: " + await CardArtAudit.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--honest-living-only")) {
                GD.Print("HONEST_LIVING_OK: " + await HonestLivingTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-0124-only")) {
                GD.Print("AUTHOR0124_OK: " + await Author0124Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--feedback0123-only")) {
                GD.Print("FEEDBACK0123_OK: " + await Feedback0123Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--achievement101-only")) {
                GD.Print("ACHIEVEMENT101_OK: " + await Achievement101Tests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--demon-army-event-only")) {
                GD.Print("DEMON_ARMY_EVENT_OK: " + await DemonArmyAttackTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--author-0120-only")) {
                GD.Print("AUTHOR0120_OK: " + await Author0120Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo103-only")) {
                GD.Print("TODO103_OK: " + await Todo103Tests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo104-only")) {
                GD.Print("TODO104_OK: " + await Todo104Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--power-icons-only")) {
                GD.Print("POWER_ICONS_OK: " + await PowerIconTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--ancient-cards-only")) {
                GD.Print("ANCIENT_CARDS_OK: " + await CardPresentationFixTests.Run(this, true));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--card-fixes-only")) {
                GD.Print("CARD_FIXES_OK: " + await CardPresentationFixTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--cards105-only")) {
                GD.Print("CARDS105_OK: " + await Cards105Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo110-only")) {
                GD.Print("TODO110_OK: " + await Todo110Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--commissions-only")) {
                GD.Print("COMMISSIONS_OK: " + await CommissionTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--commission-feedback-ui")) {
                GD.Print("COMMISSION_FEEDBACK_UI_OK: " + await CommissionFeedbackUiTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magic-shop-only")) {
                GD.Print("MAGIC_SHOP_OK: " + await MagicShopTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Any(a=>a is "--mp-host" or "--mp-client")) {
                GD.Print("MULTIPLAYER_OK: " + await MultiplayerTests.Run(this, OS.GetCmdlineUserArgs().Contains("--mp-host")));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--cards099-only")) {
                GD.Print("CARDS099_OK: " + await Cards099Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo098-ui")) {
                GD.Print("TODO098_UI_OK: " + await Todo098UiTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo098-only")) {
                GD.Print("TODO098_OK: " + await Todo098Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--excel097-only")) {
                GD.Print("EXCEL097_OK: " + await Excel097Tests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo096-only")) {
                GD.Print("TODO096_OK: " + await Todo096Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo093-only")) {
                GD.Print("TODO093_OK: " + await Todo093Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--combat-api-only")) {
                GD.Print("COMBAT_API_OK: " + await CombatTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--version-compat-only")) {
                GD.Print("VERSION_COMPAT_OK: " + await VersionCompatibilityTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--pet-feasibility-only")) {
                GD.Print("PET_FEASIBILITY_OK: " + await PetFeasibilityTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--music092-only")) {
                GD.Print("MUSIC092_OK: " + await MusicTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo091-only")) {
                GD.Print("TODO091_OK: " + await Todo091Tests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--aura-boss-only")) {
                GD.Print("AURA_BOSS_OK: " + await AuraBossTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--export-card-catalog")) {
                GD.Print("CARD_CATALOG_OK: " + CardCatalogAudit.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--current-todo-fixes-only")) {
                GD.Print("CURRENT_TODO_FIXES_OK: " + await CurrentTodoFixTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--achievement-presentation-only")) {
                GD.Print("ACHIEVEMENT_PRESENTATION_OK: " + await AchievementPresentationTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magic-deck-count-only")) {
                GD.Print("MAGIC_DECK_COUNT_OK: " + await UiTodoTests.Run(this, topBarOnly: true));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--achievements-only")) {
                GD.Print("ACHIEVEMENTS_OK: " + await AchievementSystemTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--portrait-layout-only")) {
                GD.Print("PORTRAIT_LAYOUT_OK: " + await PortraitLayoutTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--affinity-and-backs-only")) {
                GD.Print("AFFINITY_AND_BACKS_OK: " + await AffinityAndBackTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--preview-and-flow-only")) {
                GD.Print("PREVIEW_AND_FLOW_OK: " + await PreviewAndFlowTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--library-ui-only")) {
                GD.Print("LIBRARY_UI_OK: " + await LibraryUiTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--latest-todos-only")) {
                GD.Print("LATEST_TODOS_OK: " + await LatestTodoTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--event-layout-fixes-only")) {
                GD.Print("EVENT_LAYOUT_FIXES_OK: " + await EventLayoutFixTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--todo-combat-fixes-only")) {
                GD.Print("TODO_COMBAT_FIXES_OK: " + await TodoCombatFixTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magic-prop-appearance-only")) {
                GD.Print("MAGIC_PROP_APPEARANCE_OK: " + await MagicPropAppearanceTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--divinity-presentation-only")) {
                GD.Print("DIVINITY_PRESENTATION_OK: " + await DivinityPresentationTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--updated-factors-only")) {
                GD.Print("UPDATED_FACTORS_OK: " + await UpdatedFactorTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--updated-todos-only")) {
                GD.Print("UPDATED_TODOS_OK: " + await UpdatedTodoTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--spiegel-boss-only")) {
                GD.Print("SPIEGEL_BOSS_OK: " + await SpiegelBossTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--story-events-only")) {
                GD.Print("STORY_EVENTS_OK: " + await StoryEventTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--upgraded-emblem-only")) {
                GD.Print("UPGRADED_EMBLEM_OK: " + await UpgradedHolyEmblemTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--factor-feedback-only")) {
                GD.Print("FACTOR_FEEDBACK_OK: " + await UiTodoTests.RunFactorFeedback(this));
                await Finish(); return;
            }
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
            if (OS.GetCmdlineUserArgs().Contains("--card-pool-isolation-only")) {
                GD.Print("CARD_POOL_ISOLATION_OK: " + await CardPoolIsolationTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--book-scrolling-only")) {
                GD.Print("BOOK_SCROLLING_OK: " + await MagicBookScrollingTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magicbook-animations-only")) {
                GD.Print("MAGICBOOK_ANIMATIONS_OK: " + await MagicDeckAnimationTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--reward-props-only")) {
                GD.Print("REWARD_PROPS_OK: " + await RewardMagicPropTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--magic-reward-drops-only")) {
                GD.Print("MAGIC_REWARD_DROPS_OK: " + await MagicRewardDropTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--conditional-preview-only")) {
                GD.Print("CONDITIONAL_PREVIEW_OK: " + await ConditionalPreviewTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--factor-block-preview-only")) {
                GD.Print("FACTOR_BLOCK_PREVIEW_OK: " + await FactorBlockPreviewTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--custom-spell-display-only")) {
                GD.Print("CUSTOM_SPELL_DISPLAY_OK: " + await CustomSpellDisplayTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--chant-animation-speed-only")) {
                GD.Print("CHANT_ANIMATION_SPEED_OK: " + await ChantAnimationSpeedTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--tutorial-only")) {
                GD.Print("TUTORIAL_OK: " + await TutorialTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--starting-setup-only")) {
                GD.Print("STARTING_SETUP_OK: " + await StartingSetupTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--opening-event-only")) {
                GD.Print("OPENING_EVENT_OK: " + await OpeningEventTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--synchro-display-only")) {
                GD.Print("SYNCHRO_DISPLAY_OK: " + await SynchroDisplayTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--puzzle-resolution-only")) {
                GD.Print("PUZZLE_RESOLUTION_OK: " + await PuzzleResolutionTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--puzzle-display-only")) {
                var puzzlePlayer = MegaCrit.Sts2.Core.Entities.Players.Player.CreateForNewRun<FrierenMod.Scripts.Characters.Frieren>(MegaCrit.Sts2.Core.Unlocks.UnlockState.all, 1);
                var puzzleRun = MegaCrit.Sts2.Core.Runs.RunState.CreateForTest([puzzlePlayer], seed: "PUZZLE-DISPLAY");
                MegaCrit.Sts2.Core.Runs.RunManager.Instance.SetUpTest(puzzleRun, new MegaCrit.Sts2.Core.Multiplayer.NetSingleplayerGameService());
                GD.Print("PUZZLE_DISPLAY_OK: " + PuzzlePresentationTests.Run(puzzlePlayer));
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
            if (OS.GetCmdlineUserArgs().Contains("--magicdeck-playability-only")) {
                GD.Print("MAGICDECK_PLAYABILITY_OK: " + await MagicDeckPlayabilityTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--card-ids-only")) {
                GD.Print("CARD_IDS_OK: " + await CardIdTests.Run());
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--library-only")) {
                GD.Print("LIBRARY_OK: " + await LibraryTests.Run(this));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scale-only")) {
                GD.Print("SCALE_OK: " + await RelicsPotionsTests.Run(scaleOnly: true));
                await Finish(); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--relics-potions-only")) {
                GD.Print("RELICS_POTIONS_OK: " + await RelicsPotionsTests.Run());
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
            CombatTestState.SetInProgress(false);
            ui.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
            GD.Print("PASS 原生 Godot UI：紧凑计数与悬停说明、魔法书遮罩、槽位只读"); count++;
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            {
                // 独立测试场景截图，仅包含本模组新 UI。
                for (int i = 0; i < 5; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/magic-book.png"));
                book.Hide();
                CombatTestState.SetInProgress(true);
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

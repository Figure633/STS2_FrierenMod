using FrierenMod.Scripts.Ancients;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class SerieTests
{
    private static NControllerManager? Controller;
    private static NInputManager? Input;
    private static bool GetController(ref NControllerManager? __result) { __result = Controller; return false; }
    private static bool GetInput(ref NInputManager? __result) { __result = Input; return false; }
    private static bool SkipHotkey() => false;
    // TestMode将这两个纯视觉工厂置空；仅在此UI夹具中恢复原生场景实例化。
    private static void MakeRelic(RelicModel relic, NRelic.IconSize iconSize, ref NRelic? __result) {
        if (__result != null) return;
        __result = GD.Load<PackedScene>("res://scenes/relics/relic.tscn").Instantiate<NRelic>();
        __result.Model = relic; AccessTools.Field(typeof(NRelic), "_iconSize").SetValue(__result, iconSize);
    }
    private static void MakeBanner(AncientEventModel ancient, ref NAncientNameBanner? __result) {
        if (__result != null) return;
        __result = GD.Load<PackedScene>("res://scenes/ui/ancient_name_banner.tscn").Instantiate<NAncientNameBanner>();
        AccessTools.Field(typeof(NAncientNameBanner), "_ancient").SetValue(__result, ancient);
    }
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS 赛莉耶：" + text); }
        void Snapshot(string file) { using var image = root.GetViewport().GetTexture().GetImage(); image.SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/" + file)); }
        SaveManager.Instance.InitPrefsDataForTest(); LocalContext.NetId = 1;
        var selector=new MegaCrit.Sts2.Core.TestSupport.TestCardSelector();
        using var selections=CombatTestState.UseSelector(selector);
        var canonical = ModelDb.AncientEvent<Serie>();
        Check(ModelDb.AllSharedAncients.Contains(canonical), "BaseLib先古注册及百科入口");
        var gifts = Serie.Gifts;
        Check(gifts.Count == 6 && gifts.All(r => r.Rarity == RelicRarity.Ancient) &&
            canonical.AllPossibleOptions.Select(o => o.Relic!.Id).ToHashSet().SetEquals(gifts.Select(r => r.Id)), "六件遗物共用先古池且各登记一次");
        bool running = false;
        RunState Start(Player p, string seed = "SERIE-TEST") {
            if (running) RunManager.Instance.CleanUp(); running = true;
            var run = RunState.CreateForTest([p], acts: [ModelDb.Act<Overgrowth>(), ModelDb.Act<Hive>(), ModelDb.Act<Glory>()], seed: seed);
            RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = p.NetId;
            CombatTestState.SetInProgress(false); return run;
        }
        async Task<Serie> Enter(Player p) {
            var ancient = (Serie)canonical.ToMutable(); await CombatTestState.BeginEvent(ancient, p); return ancient;
        }
        var iron = Player.CreateForNewRun<Ironclad>(UnlockState.all, 1); Start(iron);
        Check(!canonical.IsValidForAct(ModelDb.Act<Glory>()), "原版角色跑局不加入赛莉耶");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); var state = Start(player);
        Check(canonical.IsValidForAct(state.Acts[2]) && !canonical.IsValidForAct(state.Acts[0]) && !canonical.IsValidForAct(state.Acts[1]), "仅第三幕加入随机池，不强制替换原先古");
        player.Creature.SetCurrentHpInternal(1); var encounter = await Enter(player);
        Check(player.Creature.CurrentHp == player.Creature.MaxHp && encounter.CurrentOptions.Count == 3 &&
            encounter.CurrentOptions.Select(o => o.Relic!.Id).Distinct().Count() == 3, "原生入场治疗及四抽三互不重复");
        var first = encounter.CurrentOptions.Select(o => o.Relic!.Id).ToArray();
        var repeatPlayer = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); Start(repeatPlayer);
        Check((await Enter(repeatPlayer)).CurrentOptions.Select(o => o.Relic!.Id).SequenceEqual(first), "同种子候选一致，使用事件RNG");
        foreach (var chosenGift in gifts) {
            player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); Start(player);
            // 只剩指定礼物，覆盖持有三件时的少选项分支与每件礼物的实际领取。
            foreach (var already in gifts.Where(r => r.Id != chosenGift.Id)) {
                if(already is RottingSageSkull) selector.PrepareToSelect([0,1,2]);
                await RelicCmd.Obtain(already.ToMutable(), player);
            }
            encounter = await Enter(player);
            Check(encounter.CurrentOptions.Count == 1 && encounter.CurrentOptions[0].Relic!.Id == chosenGift.Id, "排除已持有，只展示剩余礼物：" + chosenGift.GetType().Name);
            var option = encounter.CurrentOptions[0];
            if(chosenGift is RottingSageSkull) selector.PrepareToSelect([0,1,2]);
            await option.Chosen(); await option.Chosen();
            Check(encounter.IsFinished && player.Relics.Count(r => r.Id == chosenGift.Id) == 1, "原生领取完成且重复点击不重复获得：" + chosenGift.GetType().Name);
            var restored = Player.FromSerializable(player.ToSerializable());
            Check(restored.Relics.Any(r => r.Id == chosenGift.Id) && player.Deck.Cards.OfType<LongevityPain>().Count() == 1, "遗物真实存档可恢复，墓碑诅咒只获得一张");
            Check((await Enter(player)).IsFinished, "四件全部持有时可正常离开");
        }
        player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1); Start(player);
        foreach (string lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            AccessTools.Field(typeof(AncientEventModel), "_dialogueSet").SetValue(canonical, null);
            foreach (int visits in new[] { 0, 1, 2, 3, 4, 5, 20, 21 }) {
                var dialogues = canonical.DialogueSet.GetValidDialogues(player.Character.Id, visits, visits,
                    !canonical.AnyCharacterDialogueBlacklist.Contains(player.Character)).ToArray();
                bool wave = visits >= 3 && visits % 2 == 1;
                string expected = wave ? "FRIERENMOD-FRIEREN.3-0.ancient" : $"FRIERENMOD-FRIEREN.{Math.Min(visits, 2)}-0.{(visits >= 2 ? "char" : "ancient")}";
                Check(dialogues.Length == 1 && dialogues[0].Lines[0].LineText!.LocEntryKey == "FRIERENMOD-SERIE.talk." + expected &&
                    dialogues.All(d => !d.IsRepeating && d.Lines.Count == (wave ? 1 : visits == 0 ? 4 : 5) && d.Lines.All(l =>
                    l.LineText != null && LocString.Exists("ancients", l.LineText.LocEntryKey)) &&
                    d.Lines.Take(d.Lines.Count - 1).All(l => l.NextButtonText != null && LocString.Exists("ancients", l.NextButtonText.LocEntryKey))), $"按0、1、2、3、2、3顺序且无回退台词：{lang}/{visits}");
            }
            foreach (int visits in new[] { 0, 1, 20 }) {
                var fallback = canonical.DialogueSet.GetValidDialogues(ModelDb.Character<Ironclad>().Id, visits, visits, true).Single();
                Check(fallback.Lines.Count == 1 && fallback.Lines[0].Speaker == MegaCrit.Sts2.Core.Entities.Ancients.AncientDialogueSpeaker.Ancient &&
                    fallback.Lines[0].LineText!.LocEntryKey == "FRIERENMOD-SERIE.talk.ANY.0-0r.ancient", $"其他角色始终有赛莉耶通用对话兜底：{lang}/{visits}");
            }
        }
        LocManager.Instance.SetLanguage("zhs");
        var replacement = ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(ModelDb.Relic<HolyEmblem>());
        Check(ReferenceEquals(replacement, ModelDb.Relic<UpgradedHolyEmblem>()) && UnlockState.all.Relics.Contains(replacement), "升级接口返回百科可识别的规范模型");
        var uiHarmony = new Harmony("frieren.tests.serie_ui");
        uiHarmony.Patch(AccessTools.Method(typeof(NRelic), nameof(NRelic.Create)), postfix: new HarmonyMethod(typeof(SerieTests), nameof(MakeRelic)));
        // 独立离屏夹具没有全局输入映射和悬停宿主，避免桌面输入和默认聚焦触发这些路径。
        uiHarmony.Patch(AccessTools.Method(typeof(NEventLayout), "_Input"), prefix: new HarmonyMethod(typeof(SerieTests), nameof(SkipHotkey)));
        uiHarmony.Patch(AccessTools.Method(typeof(NEventOptionButton), "OnFocus"), prefix: new HarmonyMethod(typeof(SerieTests), nameof(SkipHotkey)));
        // 执行百科原生的SeenByDefault前置补丁，再使用真实初始遗物分类生成节点。
        AccessTools.Method(AccessTools.TypeByName("BaseLib.Abstracts.CustomRelicPoolMarkAsSeenPatch"), "MarkAllAsSeen").Invoke(null, null);
        var collection = new NRelicCollection();
        var category = GD.Load<PackedScene>("res://scenes/screens/relic_collection/relic_collection_category.tscn").Instantiate<NRelicCollectionCategory>();
        root.AddChild(category);
        category.LoadRelics(RelicRarity.Starter, collection, new LocString("relic_collection", "STARTER"),
            SaveManager.Instance.Progress.DiscoveredRelics.Select(ModelDb.GetByIdOrNull<RelicModel>).OfType<RelicModel>().ToHashSet(), UnlockState.all, UnlockState.all.Relics.ToHashSet());
        var entries = category.GetGridItems().SelectMany(row => row).OfType<NRelicCollectionEntry>().Where(e => e.relic.Id == replacement.Id).ToArray();
        Check(entries.Length == 1 && entries[0].ModelVisibility == ModelVisibility.Visible && collection.Relics.Contains(replacement), "真实百科节点可见、可检索，升级圣徽不再显示锁");
        Check(ModelDb.Relic<HolyEmblem>().PackedIconPath != replacement.PackedIconPath && replacement.Icon.GetWidth() == 128,
            "普通/升级圣徽图标分离，完整保留金色光线");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            category.Position = new(350, 200);
            for (int i = 0; i < 4; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Snapshot("serie-starter-collection.png");
        }
        category.QueueFree(); collection.Free();
        using var background = GD.Load<PackedScene>(canonical.CustomScenePath).Instantiate<Control>();
        root.AddChild(background);
        Check(background.GetNode<TextureRect>("Library").Texture.GetSize() == new Vector2(928, 524) &&
            background.GetNode<TextureRect>("Library").StretchMode == TextureRect.StretchModeEnum.KeepAspectCovered &&
            canonical.MapIcon.GetWidth() > 0 && canonical.RunHistoryIcon.GetWidth() > 0, "原图原尺寸加载，事件等比铺满，地图与历史图标可加载");
        using (var iconImage = canonical.MapIcon.GetImage())
            Check(iconImage.GetSize() == new Vector2I(128,128) && iconImage.GetPixel(0,0).A == 0 &&
                iconImage.GetPixel(64,64).A < .1f && iconImage.GetPixel(64,20).A > .5f && canonical.RunHistoryIcon.GetWidth() == 64,
                "简笔图标完整加载，脸部和边缘透明，头发轮廓保留");
        var point = new MegaCrit.Sts2.Core.Nodes.Screens.Map.NAncientMapPoint();
        Check((Color)AccessTools.PropertyGetter(point.GetType(),"UntravelableColor").Invoke(point,null)! == MegaCrit.Sts2.Core.Helpers.StsColors.bossNodeUntraveled,
            "简笔图标恢复原生先古地图状态配色");
        point.Free();
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            for (int i = 0; i < 4; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Snapshot("serie-background.png");
        }
        background.GetParent().RemoveChild(background);
        background.Free();
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
            Controller = new(); Input = new();
            uiHarmony.Patch(AccessTools.PropertyGetter(typeof(NControllerManager), nameof(NControllerManager.Instance)), prefix: new HarmonyMethod(typeof(SerieTests), nameof(GetController)));
            uiHarmony.Patch(AccessTools.PropertyGetter(typeof(NInputManager), nameof(NInputManager.Instance)), prefix: new HarmonyMethod(typeof(SerieTests), nameof(GetInput)));
            uiHarmony.Patch(AccessTools.Method(typeof(NAncientEventLayout), "UpdateHotkeyDisplay"), prefix: new HarmonyMethod(typeof(SerieTests), nameof(SkipHotkey)));
            uiHarmony.Patch(AccessTools.Method(typeof(NAncientNameBanner), nameof(NAncientNameBanner.Create)), postfix: new HarmonyMethod(typeof(SerieTests), nameof(MakeBanner)));
            encounter = await Enter(player);
            var layout = GD.Load<PackedScene>("res://scenes/events/ancient_event_layout.tscn").Instantiate<NAncientEventLayout>();
            root.AddChild(layout); layout.SetEvent(encounter);
            var lines = canonical.DialogueSet.GetValidDialogues(player.Character.Id, 0, 0, false).First().Lines;
            layout.SetDialogue(lines); layout.AddOptions(encounter.CurrentOptions); layout.OnSetupComplete();
            // 模拟完成对话后显示三项礼物，核对原生选项与人物位置。
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            AccessTools.Method(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate").Invoke(layout, [lines.Count - 1]);
            await root.ToSignal(root.GetTree().CreateTimer(5.5), SceneTreeTimer.SignalName.Timeout);
            Snapshot("serie-native-options.png");
            Check(layout.OptionButtons.Count() == 3 && layout.OptionButtons.All(b => b.GetGlobalRect().End.Y <= 1080), "原生先古版式展示三件礼物且选项不溢出");
            layout.QueueFree();
            for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            layout = GD.Load<PackedScene>("res://scenes/events/ancient_event_layout.tscn").Instantiate<NAncientEventLayout>();
            root.AddChild(layout); layout.SetEvent(encounter);
            lines = canonical.DialogueSet.GetValidDialogues(player.Character.Id, 3, 3, false).Single().Lines;
            layout.SetDialogue(lines); layout.AddOptions(encounter.CurrentOptions); layout.OnSetupComplete();
            await root.ToSignal(root.GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
            Check(layout.GetNode<Control>("%FakeNextButtonContainer").Visible &&
                layout.GetNode<Control>("%DialogueHitbox").Visible && lines.Count == 1 &&
                lines[0].NextButtonText?.GetFormattedText() == "继续" &&
                lines[0].LineText!.GetRawText() == "[i]赛莉耶摆摆手，示意你随便挑一件然后出去。[/i]", "摆手斜体独句显示原生继续按钮");
            Snapshot("serie-wave-continue.png");
            AccessTools.Method(typeof(NAncientEventLayout), "OnDialogueHitboxClicked").Invoke(layout, [null]);
            await root.ToSignal(root.GetTree().CreateTimer(1.2), SceneTreeTimer.SignalName.Timeout);
            Check(!layout.GetNode<Control>("%FakeNextButtonContainer").Visible &&
                !layout.GetNode<Control>("%DialogueHitbox").Visible && layout.OptionButtons.Count() == 3 &&
                (int)AccessTools.Field(typeof(NAncientEventLayout), "_currentDialogueLine").GetValue(layout)! == 0 &&
                layout.GetNode<Control>("%DialogueContainer").GetChildCount() == 1, "点击继续后展开礼物，保留有效行索引且无芙莉莲回应");
            // 鼠标回看动作说明时仍使用有效的原生对话节点。
            var lineNode = layout.GetNode<Control>("%DialogueContainer").GetChild<NAncientDialogueLine>(0);
            AccessTools.Method(typeof(NAncientEventLayout), "OnDialogueLineFocused").Invoke(layout, [lineNode]);
            AccessTools.Method(typeof(NAncientEventLayout), "OnDialogueLineUnfocused").Invoke(layout, [lineNode]);
            Snapshot("serie-wave-options.png");
            layout.QueueFree();
            for (int i = 0; i < 3; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Controller.Free(); Input.Free(); Controller = null; Input = null;
        }
        uiHarmony.UnpatchAll(uiHarmony.Id);
        RunManager.Instance.CleanUp(); LocManager.Instance.SetLanguage("eng");
        return count;
    }
}

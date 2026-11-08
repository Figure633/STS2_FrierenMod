using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class EventArtPreviewTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
        async Task Tick(int count = 3) { for (int i = 0; i < count; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "EVENT-ART-0126");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        SaveManager.Instance.InitPrefsDataForTest();
        if (!InputMap.HasAction(DebugHotkey.hideEventUi)) InputMap.AddAction(DebugHotkey.hideEventUi);
        var models = ModelDb.AllEvents.OfType<EventArtPreview>().OrderBy(model => model.ArtIndex).ToArray();
        Check(models.Length == 12 && models.Select(m => m.ArtIndex).Distinct().Count() == 12, "12张图均有原生事件控制台入口");
        Check(models.All(m => !m.IsAllowed(run)), "预览不进入普通随机事件抽取");
        var directory = ProjectSettings.GlobalizePath("res://../../artifacts/event-layout-review");
        Directory.CreateDirectory(directory);
        foreach (var language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (var canonical in models) {
                var model = (EventArtPreview)canonical.ToMutable(); await CombatTestState.BeginEvent(model, player);
                foreach (var option in model.CurrentOptions) model.DynamicVars.AddTo(option.Description);
                Check(model.CreateInitialPortrait().GetWidth() > 0, "插画可加载：" + model.ArtLabel);
                Check(model.Title.GetFormattedText().Contains(EventArtPreview.Images[model.ArtIndex], StringComparison.OrdinalIgnoreCase), "标题与插画一致");
                Check(model.CurrentOptions.Count == 5 && model.CurrentOptions.All(option => !option.Title.GetFormattedText().Contains("LOC_ERR") && !option.Description.GetFormattedText().Contains("LOC_ERR")), "原生选项及预览导航完整");
                var host = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(host);
                var layout = model.CreateScene().Instantiate<NEventLayout>(); host.AddChild(layout); model.SetNode(layout);
                layout.SetEvent(model); layout.SetTitle(model.Title.GetFormattedText());
                var description = model.Description!; model.DynamicVars.AddTo(description); layout.SetDescription(description.GetFormattedText());
                layout.AddOptions(model.CurrentOptions); layout.OnSetupComplete(); await Tick();
                // 使用原版顶栏场景的背景及几何位置，覆盖在事件上方，复现实际跑局的遮挡。
                var nativeTopBar = GD.Load<PackedScene>("res://scenes/ui/top_bar.tscn").Instantiate<Control>();
                var topBar = (TextureRect)nativeTopBar.GetNode<TextureRect>("BgImage").Duplicate();
                nativeTopBar.Free(); host.AddChild(topBar); topBar.ZIndex = 20;
                foreach (var tween in root.GetTree().GetProcessedTweens()) tween.CustomStep(10);
                await Tick();
                var portrait = layout.GetNode<TextureRect>("%Portrait");
                var backdrop = layout.GetNode<TextureRect>("FrierenEventBackdrop");
                foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1920, 1200), new Vector2(2560, 1080) }) {
                    host.Size = size; await Tick();
                    Check(portrait.GetGlobalRect().Position.Y > topBar.GetGlobalRect().End.Y &&
                        Mathf.IsEqualApprox(portrait.GetGlobalRect().End.Y, host.GetGlobalRect().End.Y), "前景避开原版顶栏并延伸到底部：" + size);
                }
                host.Size = new Vector2(1920, 1080); await Tick();
                Check(backdrop.SelfModulate == new Color(0.16f, 0.16f, 0.16f) &&
                    layout.GetNodeOrNull("FrierenEventTextShade") == null, "背景左右统一中间亮度，不额外遮暗右侧");
                Check(portrait.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered && portrait.Texture.GetSize() == model.CreateInitialPortrait().GetSize(), "使用完整原图等比显示");
                Check(!portrait.GetGlobalRect().Intersects(layout.GetNode<Control>("%EventDescription").GetGlobalRect()) && layout.OptionButtons.All(button => !portrait.GetGlobalRect().Intersects(button.GetGlobalRect())), "插画与正文/选项分离");
                Check(layout.OptionButtons.All(button => host.GetGlobalRect().Encloses(button.GetGlobalRect())), "五个原生按钮均在视口内");
                if (language == "zhs" && OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                    root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory, EventArtPreview.Images[model.ArtIndex] + "-native.png"));
                int start = model.ArtIndex, gold = player.Gold; var hp = player.Creature.CurrentHp; int deck = player.Deck.Cards.Count;
                await model.CurrentOptions[1].Chosen();
                Check(model.ArtIndex == (start + 1) % 12 && portrait.Texture == GD.Load<Texture2D>(model.CustomInitialPortraitPath), "下一张同步更新原生图片");
                await model.CurrentOptions[0].Chosen();
                Check(model.ArtIndex == start && !model.IsFinished && player.Gold == gold && player.Creature.CurrentHp == hp && player.Deck.Cards.Count == deck, "翻页无奖励/扣血并可恢复初始图");
                await model.CurrentOptions[4].Chosen();
                Check(model.IsFinished && player.Gold == gold, "离开预览正常结束且没有奖励");
                layout.ClearOptions(); host.QueueFree(); await Tick();
            }
        }
        // 正式剧情事件与开局事件也使用相同安全区域，不只检查十二张预览图。
        foreach (var canonical in ModelDb.AllEvents.Where(m => m is FrierenStoryEvent).Append(ModelDb.Event<ReadingGrimoireEvent>())) {
            var model = canonical.ToMutable();
            var host = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(host);
            var layout = model.CreateScene().Instantiate<NEventLayout>(); host.AddChild(layout); model.SetNode(layout); layout.SetEvent(model);
            var portrait = layout.GetNode<TextureRect>("%Portrait");
            foreach (var path in model.GetAssetPaths(run).Where(path => path.StartsWith("res://FrierenMod/images/events/")).Distinct()) {
                layout.SetPortrait(GD.Load<Texture2D>(path)); await Tick(1);
                Check(portrait.GetGlobalRect().Position.Y >= 112 &&
                    Mathf.IsEqualApprox(portrait.GetGlobalRect().End.Y, 1080) &&
                    layout.GetNode<TextureRect>("FrierenEventBackdrop").SelfModulate == new Color(0.16f, 0.16f, 0.16f), "剧情事件换图后保持安全区域与统一背景：" + path);
            }
            host.QueueFree(); await Tick();
        }
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return checks;
    }
}

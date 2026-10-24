using FrierenMod.Core;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.addons.mega_text;

public static class EventLayoutFixTests
{
    public static async Task<int> Run(Node root)
    {
        int checks = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; GD.Print("PASS 0.7.7：" + name); }
        async Task Tick(int n = 4) { for (int i = 0; i < n; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "EVENT-LAYOUT-077");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        SaveManager.Instance.InitPrefsDataForTest(); SaveManager.Instance.PrefsSave.FastMode = FastModeType.Instant;
        if (!InputMap.HasAction(DebugHotkey.hideEventUi)) InputMap.AddAction(DebugHotkey.hideEventUi);
        MagicDeckStore.Write(player, new MagicDeckState());
        var canonicals = new EventModel[] { ModelDb.Event<FoodEvent>(), ModelDb.Event<KraftGift>(), ModelDb.Event<AnimalWell>(),
            ModelDb.Event<MimicFight>(), ModelDb.Event<ReadingGrimoireEvent>() };
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (var canonical in canonicals) {
                var model = canonical.ToMutable(); await model.BeginEvent(player, false);
                var host = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(host);
                var layout = model.CreateScene().Instantiate<NEventLayout>(); host.AddChild(layout); model.SetNode(layout);
                layout.SetEvent(model); layout.SetTitle(model.Title.GetFormattedText());
                var description = model.Description!; model.DynamicVars.AddTo(description); layout.SetDescription(description.GetFormattedText());
                layout.AddOptions(model.CurrentOptions); layout.OnSetupComplete(); await Tick();
                foreach (var tween in root.GetTree().GetProcessedTweens()) tween.CustomStep(10);
                await Tick();
                var picture = layout.GetNode<TextureRect>("%Portrait");
                var backdrop = layout.GetNode<TextureRect>("FrierenEventBackdrop");
                var text = layout.GetNode<MegaRichTextLabel>("%EventDescription");
                var options = layout.GetNode<VBoxContainer>("%OptionsContainer");
                Check(picture.StretchMode == TextureRect.StretchModeEnum.KeepAspectCentered && picture.Scale == Vector2.One &&
                    picture.Texture == backdrop.Texture && backdrop.StretchMode == TextureRect.StretchModeEnum.KeepAspectCovered,
                    language + model.Id.Entry + "原图完整等比展示，同图背景铺满");
                foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1920, 1200), new Vector2(2560, 1080) }) {
                    host.Size = size; await Tick();
                    Check(!picture.GetGlobalRect().Intersects(text.GetGlobalRect()) &&
                        layout.OptionButtons.All(button => !picture.GetGlobalRect().Intersects(button.GetGlobalRect())),
                        model.Id.Entry + "正文与所有原生选项不遮挡图片，视口=" + size);
                    Check(backdrop.GetGlobalRect().Encloses(host.GetGlobalRect()) &&
                        layout.OptionButtons.All(button => host.GetGlobalRect().Encloses(button.GetGlobalRect())),
                        model.Id.Entry + "背景无边带且按钮完整处于视口，视口=" + size);
                }
                host.Size = new Vector2(1920, 1080); await Tick();
                var originalOptions = model.CurrentOptions.ToArray();
                foreach (string asset in model.GetAssetPaths(run).Where(p => p.StartsWith("res://FrierenMod/images/events/") && p.EndsWith(".png")).Distinct()) {
                    var texture = GD.Load<Texture2D>(asset); layout.SetPortrait(texture); await Tick(1);
                    Check(picture.Texture == texture && backdrop.Texture == texture && layout.GetChildren().Count(n => n.Name == "FrierenEventBackdrop") == 1,
                        "页面换图同步且不叠加背景：" + asset);
                }
                layout.SetPortrait(model.CreateInitialPortrait()); await Tick();
                Check(options.GetChildCount() == originalOptions.Length && layout.OptionButtons.Select(b => b.Option).SequenceEqual(originalOptions),
                    model.Id.Entry + "原生选项对象、数量和回调完整保留");
                Check(!text.GetParsedText().Contains('[') && !text.GetParsedText().Contains('@') && !text.GetParsedText().Contains('~') &&
                    text.CustomEffects.Any(e => e.AsGodotObject()?.GetType().Name == "RichTextSine") &&
                    text.CustomEffects.Any(e => e.AsGodotObject()?.GetType().Name == "RichTextJitter"),
                    model.Id.Entry + "原生富文本安装摇摆抖动效果且不泄漏标签");
                if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot") && model is AnimalWell or KraftGift)
                    root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/event-layout-" + model.GetType().Name + "-" + language + ".png"));
                layout.ClearOptions(); host.QueueFree(); await Tick();
            }
        }
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            foreach (string key in new[] { "FRIERENMOD-FOOD_EVENT.pages.INITIAL.description", "FRIERENMOD-KRAFT_GIFT.pages.INITIAL.description", "FRIERENMOD-ANIMAL_WELL.pages.INITIAL.description" }) {
                string text = new LocString("events", key).GetFormattedText();
                Check(text.Contains("[sine]") || text.Contains("[jitter]"), language + "一代叙事动态强调已还原：" + key);
            }
            string danger = new LocString("events", "FRIERENMOD-MIMIC_FIGHT.pages.INITIAL.description").GetFormattedText();
            Check(danger.Contains("[gold]") && danger.Contains("[blue]") && danger.Contains("[red]") && danger.Contains("[jitter]"),
                language + "宝箱怪事件保留一代原色与抖动");
        }
        var native = ModelDb.Event<WhisperingHollow>();
        var controlHost = new Control { Size = new Vector2(1920, 1080) }; root.AddChild(controlHost);
        var control = native.CreateScene().Instantiate<NEventLayout>(); controlHost.AddChild(control);
        AccessTools.Field(typeof(NEventLayout), "_event").SetValue(control, native);
        control.SetPortrait(native.CreateInitialPortrait());
        Check(control.GetNodeOrNull("FrierenEventBackdrop") == null && control.GetNode<TextureRect>("%Portrait").Scale == new Vector2(1.04f, 1.04f),
            "原版事件继续使用原来的场景与图片布局");
        controlHost.QueueFree(); await Tick(); LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return checks;
    }
}

using System.Collections;
using System.Text.Json;
using BaseLib.Config.UI;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Saves.Validation;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class TutorialTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 首次教程：" + name); count++; }
        async Task Tick() { for (int i = 0; i < 8; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame); }
        async Task Snapshot(string name) {
            await Tick();
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/tutorial-" + name + ".png"));
        }
        Player NewRun() {
            var p = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
            RunManager.Instance.SetUpTest(RunState.CreateForTest([p], seed: "FRIEREN-TUTORIAL"), new NetSingleplayerGameService());
            LocalContext.NetId = 1; return p;
        }
        Player Copy(Player p) => Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(
            JsonSerializer.Serialize(p.ToSerializable(), JsonSerializationUtility.Options), JsonSerializationUtility.Options)!);
        var save = SaveManager.Instance;
        save.InitProfileId(1);
        save.InitPrefsDataForTest();
        var originalProgress = save.Progress;
        save.Progress = ProgressState.CreateDefault();
        var player = NewRun();
        var ui = new Control { Size = new(1920, 1080) };
        // 等父场景结束 _Ready，再把全屏宿主挂到窗口，使用真实原版模态管理。
        await Tick(); root.GetTree().Root.AddChild(ui);
        ui.AddChild(new ColorRect { Size = ui.Size, Color = new("17202c"), MouseFilter = Control.MouseFilterEnum.Ignore });
        var modal = new NModalContainer { Size = ui.Size, ZIndex = 200, MouseFilter = Control.MouseFilterEnum.Ignore };
        modal.AddChild(new ColorRect { Name = "Backstop", Size = ui.Size, Color = new(0, 0, 0, 0), Visible = false });
        ui.AddChild(modal);
        var blockers = (IDictionary)AccessTools.Field(typeof(NHotkeyManager), "_blockingScreens").GetValue(NHotkeyManager.Instance)!;
        int initialBlockers = blockers.Count;
        FrierenTutorial Current() => modal.OpenModal as FrierenTutorial ?? throw new Exception("教程未显示");
        void Next() => Current().GetNode<NButton>("RightArrow").EmitSignal(NButton.SignalName.Released, Current().GetNode<NButton>("RightArrow"));
        try {
            save.Progress.EnableFtues = false;
            Check(await FrierenTutorial.Show(player) && modal.OpenModal == null, "尊重原版关闭教程设置");
            save.Progress.EnableFtues = true;
            string before = MagicDeckStore.Read(player).ToJson(); int deckCards = player.Deck.Cards.Count;
            var pending = FrierenTutorial.Show(player);
            await Tick();
            Check(!pending.IsCompleted && blockers.Count == initialBlockers + 1, "首次显示原版模态界面并阻止背后快捷键");
            Check(Current().GetNode<TextureRect>("Image").GetChildren().OfType<NCard>().Count() == 1, "第一页使用真实魔力卡面");
            Check(Current().GetNode<MegaRichTextLabel>("%Description").Text.Contains("Mana"), "英文教程正文从本地化读取");
            await Snapshot("eng-1"); Next(); await Tick();
            Check(Current().GetNode<TextureRect>("Image").GetChildren().OfType<NCard>().Count() == 2, "第二页显示咏唱牌和因子");
            Next(); await Tick();
            Check(Current().GetNode<MegaLabel>("PageCount").Text == "3 / 3", "翻页更新原版页码");
            Check(Current().GetNode<TextureRect>("Image").GetChildren().OfType<NCard>().Count() == 3, "第三页显示因子和魔具");
            await Snapshot("eng-3"); Next(); await Tick();
            Check(pending.IsCompletedSuccessfully && await pending && modal.OpenModal == null && blockers.Count == initialBlockers, "完成后关闭界面并释放原版输入锁");
            Check(MagicDeckStore.Read(player).ToJson() == before && player.Deck.Cards.Count == deckCards, "教程示例牌不进入魔法牌组或主牌组");
            Check(save.Progress.FtueCompleted.Contains(FrierenTutorial.TutorialId) && !save.Progress.FtueCompleted.Contains("combat_rules_ftue"), "只保存自定义教程标记");
            save.Progress = ProgressState.FromSerializable(save.Progress.ToSerializable(), new DeserializationContext());
            Check(await FrierenTutorial.Show(player) && modal.OpenModal == null, "保存重载后不重复自动弹出");

            save.SettingsSave.Language = "zhs"; MegaCrit.Sts2.Core.Localization.LocManager.Initialize();
            var book = new MagicBookScreen(ui, player, () => null); book.Open(); await Tick();
            var help = (NConfigButton)book.View.FindChild("ReplayMagicTutorial", true, false);
            help.EmitSignal(NButton.SignalName.Released, help); await Tick();
            Check(Current().GetNode<MegaRichTextLabel>("%Description").Text.Contains("魔力"), "已看过仍可从魔法书重看中文教程");
            await Snapshot("zhs-1"); Next(); await Snapshot("zhs-2"); Next(); await Snapshot("zhs-3");
            root.GetViewport().PushInput(new InputEventAction { Action = MegaInput.cancel, Pressed = true }, true); await Tick();
            Check(modal.OpenModal == null && book.View.Visible && blockers.Count == initialBlockers + 1, "Esc仅关闭教程，保留魔法书输入与界面");
            book.Close(); await Tick();
            Check(blockers.Count == initialBlockers, "关闭魔法书后无输入锁残留");
            book.View.QueueFree(); await Tick();

            save.ResetFtues();
            var selector = new TestCardSelector(); using var choices = CardSelectCmd.UseSelector(selector);
            var context = new BlockingPlayerChoiceContext();
            Task setup = MagicDeckCmd.ChooseStartingFactors(context, player); await Tick();
            Check(modal.OpenModal is FrierenTutorial && MagicDeckStore.Read(player).Draft.Length == 0, "重置原版教程后，在开局抽选项之前显示");
            var restored = Copy(player);
            RunManager.Instance.CleanUp(); await Tick();
            Check(setup.IsCompletedSuccessfully && modal.OpenModal == null && blockers.Count == initialBlockers, "教程中退出跑局释放等待与输入锁");
            Check(!save.Progress.FtueCompleted.Contains(FrierenTutorial.TutorialId), "中途退出不当作已完成教程");
            RunManager.Instance.SetUpTest(RunState.CreateForTest([restored], seed: "FRIEREN-TUTORIAL"), new NetSingleplayerGameService());
            LocalContext.NetId = 1;
            for (int i = 0; i < 3; i++) selector.PrepareToSelect(new[] { 0 });
            setup = MagicDeckCmd.ChooseStartingFactors(context, restored); await Tick();
            Current().GetNode<NConfigButton>("FinishTutorial").EmitSignal(NButton.SignalName.Released, Current().GetNode<NConfigButton>("FinishTutorial"));
            await Tick();
            Check(setup.IsCompletedSuccessfully && MagicDeckStore.Read(restored).SetupCompleted && MagicDeckStore.Read(restored).Props.Count == 2, "跳过后继续三槽选择，只赠送两个妙妙工具");
            Check(blockers.Count == initialBlockers && save.Progress.FtueCompleted.Contains(FrierenTutorial.TutorialId), "跳过也保存完成状态且选牌前已释放输入");
        }
        finally {
            RunManager.Instance.CleanUp(); modal.Clear(); await Tick();
            ui.QueueFree(); await Tick();
            AccessTools.Property(typeof(NModalContainer), nameof(NModalContainer.Instance)).SetValue(null, null);
            save.Progress = originalProgress;
        }
        return count;
    }
}

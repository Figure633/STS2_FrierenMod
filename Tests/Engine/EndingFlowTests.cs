using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class EndingFlowTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            GD.Print("PASS " + name); count++;
        }
        SaveManager.Instance.InitProfileId(1);
        SaveManager.Instance.InitPrefsDataForTest();
        string? replayPath = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--ending-save="))?["--ending-save=".Length..];
        // 原生 TestMode + shouldSave:false：只操作内存测试局，不提交统计或玩家存档。
        foreach (string language in new[] { "eng", "zhs" })
        {
            SaveManager.Instance.SettingsSave.Language = language;
            LocManager.Initialize();
            var canonical = ModelDb.Event<TheArchitect>();
            // 对话表会缓存；切换测试语言时清空以验证当前 PCK 内的真实文本。
            AccessTools.Field(typeof(TheArchitect), "_dialogueSet").SetValue(canonical, null);
            foreach (int wins in new[] { 0, 1, 2, 10 })
            {
                var dialogues = canonical.DialogueSet.GetValidDialogues(
                    ModelDb.Character<Frieren>().Id, wins, wins, allowAnyCharacterDialogues: false).ToList();
                Check(dialogues.Count > 0 && dialogues.All(d =>
                    d.Lines.Count >= 3 && (wins == 0 || d.IsRepeating) && d.EndAttackers == ArchitectAttackers.Both &&
                    d.Lines.All(l => l.LineText != null && LocString.Exists("ancients", l.LineText.LocEntryKey)) &&
                    d.Lines.Take(d.Lines.Count - 1).All(l => l.NextButtonText != null &&
                        LocString.Exists("ancients", l.NextButtonText!.LocEntryKey))),
                    $"建筑师结尾对话可用：{language}，此前通关 {wins} 次");
            }

            if (RunManager.Instance.DebugOnlyGetState() != null) RunManager.Instance.CleanUp();
            SerializableRun? checkpoint = replayPath == null ? null :
                SaveManager.FromJson<SerializableRun>(System.IO.File.ReadAllText(replayPath)).SaveData
                    ?? throw new Exception("无法读取指定的检查点副本");
            var run = checkpoint == null
                ? RunState.CreateForTest([Player.CreateForNewRun<Frieren>(UnlockState.all, 1)],
                    acts: [ModelDb.Act<Overgrowth>(), ModelDb.Act<Hive>(), ModelDb.Act<Glory>()], seed: "FRIERENENDING")
                : RunState.FromSerializable(checkpoint);
            var player = run.Players.Single();
            if (player.NetId != 1 || player.Character is not Frieren)
                throw new Exception("结算测试仅接受单人芙莉莲检查点副本");
            if (checkpoint == null) RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService());
            else {
                await RunManager.Instance.SetUpSavedSingleplayer(run, checkpoint);
                AccessTools.Property(typeof(RunManager), nameof(RunManager.ShouldSave))
                    .SetValue(RunManager.Instance, false);
            }
            LocalContext.NetId = 1;
            AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress))
                .SetValue(CombatManager.Instance, false);
            if (checkpoint == null) {
                run.CurrentActIndex = 2;
                run.AppendToMapPointHistory(MapPointType.Boss, RoomType.Event, canonical.Id);
                run.PushRoom(new RestSiteRoom());
            } else {
                Check(run.CurrentActIndex == 2 && checkpoint.PreFinishedRoom?.IsPreFinished == true,
                    "旧检查点仍位于已完成的第三幕首领房间");
                run.PushRoom(AbstractRoom.FromSerializable(checkpoint.PreFinishedRoom, run)!);
            }
            await RunManager.Instance.EnterNextAct();
            var sync = RunManager.Instance.EventSynchronizer;
            var ending = (TheArchitect)sync.GetLocalEvent();
            Check(ending.CurrentOptions.Count == 1 &&
                ending.CurrentOptions[0].TextKey != "PROCEED",
                $"建筑师先显示角色对话，而非缺失对话后的空继续按钮：{language}");
            for (int line = 0; line < 8 && ending.CurrentOptions.Single().TextKey != "PROCEED"; line++) {
                sync.ChooseLocalOption(0);
                await sync.AwaitPendingOptionTasks();
            }
            Check(ending.CurrentOptions.Single().TextKey == "PROCEED",
                $"回应后可以继续结算：{language}");
            RunManager.Instance.ActionExecutor.Unpause();
            sync.ChooseLocalOption(0);
            await sync.AwaitPendingOptionTasks();
            await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
            for (int i = 0; i < 120 && !run.IsGameOver; i++)
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(run.IsGameOver && run.CurrentRoom?.IsVictoryRoom == true &&
                AccessTools.Field(typeof(RunManager), "_runHistoryWasUploaded")
                    .GetValue(RunManager.Instance) is true,
                $"原生继续动作完成胜利结算，不停留在建筑师事件：{language}");

            var serialized = RunManager.Instance.ToSave(null);
            int previousWins = SaveManager.Instance.Progress.GetStatsForCharacter(player.Character.Id)?.TotalWins ?? 0;
            SaveManager.Instance.UpdateProgressWithRunData(serialized, victory: true);
            RunHistoryUtilities.CreateRunHistoryEntry(serialized, victory: true, isAbandoned: false,
                RunManager.Instance.NetService.Platform);
            Check(SaveManager.Instance.Progress.GetStatsForCharacter(player.Character.Id)?.TotalWins == previousWins + 1 &&
                RunManager.Instance.History?.Win == true &&
                SaveManager.Instance.LoadRunHistory($"{serialized.StartTime}.run").SaveData?.Win == true,
                $"原生内存进度记录胜利，并创建芙莉莲通关历史：{language}");
            // 测试结算按钮与分数页；新时间线奖励动画依赖完整 NGame，不属于此夹具范围。
            SaveManager.Instance.Progress.TotalUnlocks = 18;
            var screen = GD.Load<PackedScene>(NGameOverScreen.AssetPaths.Single())
                .Instantiate<NGameOverScreen>();
            AccessTools.Field(typeof(NGameOverScreen), "_runState").SetValue(screen, run);
            AccessTools.Field(typeof(NGameOverScreen), "_serializableRun").SetValue(screen, serialized);
            AccessTools.Field(typeof(NGameOverScreen), "_localPlayer").SetValue(screen, player);
            root.AddChild(screen);
            Check(!screen.GetNode<Control>("%Ui").HasNode("FrierenVictoryArt") &&
                !screen.FindChildren("*", "AudioStreamPlayer", true, false).OfType<AudioStreamPlayer>().Any() &&
                !string.IsNullOrWhiteSpace(screen.GetNode<RichTextLabel>("%VictoryDamageLabel").Text) &&
                screen.GetNodeOrNull<Control>("%ContinueButton") != null &&
                screen.GetNodeOrNull<Control>("%MainMenuButton") != null,
                $"保留原生阶段结果页与按钮，不添加一代终幕图或胜利语音：{language}");
            // 独立结果页没有战斗角色图层；仍执行原生入场动画和按钮启用流程。
            await (Task)AccessTools.Method(typeof(NGameOverScreen), "AnimateIn").Invoke(screen, null)!;
            var continueButton = screen.GetNode<NButton>("%ContinueButton");
            if (!continueButton.IsEnabled) throw new Exception("结果页入场动画未启用继续按钮");
            continueButton.EmitSignal(NClickableControl.SignalName.Released, continueButton);
            var menuButton = screen.GetNode<NButton>("%MainMenuButton");
            for (int i = 0; i < 900 && !menuButton.IsEnabled; i++)
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(menuButton.IsEnabled && menuButton.Visible &&
                screen.GetNode<Control>("%RunSummaryContainer").Visible &&
                screen.GetNode<Control>("%ScoreLineContainer").GetChildCount() >= 2,
                $"点击继续后分数页完成显示，返回主菜单按钮可用：{language}");
            if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                await root.ToSignal(root.GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
                root.GetViewport().GetTexture().GetImage().SavePng(
                    ProjectSettings.GlobalizePath($"res://../../artifacts/ending-{language}.png"));
            }
            screen.QueueFree();
            await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        RunManager.Instance.CleanUp();
        SaveManager.Instance.SettingsSave.Language = "eng";
        LocManager.Initialize();
        return count;
    }
}

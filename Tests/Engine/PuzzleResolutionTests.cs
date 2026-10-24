using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Helpers;

public static class PuzzleResolutionTests
{
    private static readonly List<string> Hints = [];
    private static bool CaptureHint(LocString line) { Hints.Add(line.GetFormattedText()); return false; }
    private static int BeamCount;
    private static NHyperbeamVfx? BeamStub;
    private static TaskCompletionSource? KillDelay;
    private static float DelaySeconds;
    private static readonly List<float> Delays = [];
    private static bool RenderNative;
    private static bool MutePreviewAudio() => !RenderNative;
    private static bool InteractiveVisual(ref bool __result)
    {
        if (!RenderNative) return true;
        __result = false; return false;
    }
    private static bool CaptureBeam(Creature owner, Creature target, ref NHyperbeamVfx? __result)
    {
        if (!target.IsAlive) throw new Exception("谜题特效开始前敌人已经死亡");
        BeamCount++; __result = BeamStub = new NHyperbeamVfx(); return false;
    }
    private static bool CaptureDelay(float seconds, ref Task __result)
    {
        if (Math.Abs(seconds - 1.025f) > 0.001f && Math.Abs(seconds - 1f) > 0.001f) return true;
        Delays.Add(seconds); DelaySeconds = seconds; KillDelay = new TaskCompletionSource(); __result = KillDelay.Task; return false;
    }
    public static async Task<int> Run()
    {
        int count = 0; Hints.Clear(); BeamCount = 0; Delays.Clear(); RenderNative = false;
        var hintPatch = new Harmony("frieren.tests.puzzle_hints");
        hintPatch.Patch(AccessTools.Method(typeof(TalkCmd), nameof(TalkCmd.Play)),
            prefix: new HarmonyMethod(typeof(PuzzleResolutionTests), nameof(CaptureHint)));
        hintPatch.Patch(AccessTools.Method(typeof(NHyperbeamVfx), nameof(NHyperbeamVfx.Create), [typeof(Creature),typeof(Creature)]),
            prefix: new HarmonyMethod(typeof(PuzzleResolutionTests), nameof(CaptureBeam)));
        hintPatch.Patch(AccessTools.Method(typeof(Cmd), nameof(Cmd.Wait), [typeof(float),typeof(bool)]),
            prefix: new HarmonyMethod(typeof(PuzzleResolutionTests), nameof(CaptureDelay)));
        hintPatch.Patch(AccessTools.PropertyGetter(typeof(NonInteractiveMode), nameof(NonInteractiveMode.IsActive)),
            prefix: new HarmonyMethod(typeof(PuzzleResolutionTests), nameof(InteractiveVisual)));
        hintPatch.Patch(AccessTools.Method(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string),typeof(float)]),
            prefix: new HarmonyMethod(typeof(PuzzleResolutionTests), nameof(MutePreviewAudio)));
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 谜题结算：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-PUZZLE-INSTANT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        Creature[] SpawnEnemies()
        {
            var spawned = Enumerable.Range(0, 2).Select(_ => combat.CreateCreature(
                ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null)).ToArray();
            foreach (var enemy in spawned) combat.AddCreature(enemy);
            return spawned;
        }
        var enemies = SpawnEnemies();
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        CardModel card = combat.CreateCard<RapidChant>(player);
        var play = new CardPlay { Card = card, Target = null, IsAutoPlay = false, PlayIndex = 0, ResultPile = PileType.Discard, PlayCount = 1,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        var ids = new[] { "Factor100", "Factor010", "Factor001" };
        var deck = new MagicDeckState();
        for (int i = 0; i < 3; i++) { deck.PrepareDraft(_ => 0); deck.SelectInitial(deck.Draft[0]); }
        for (int i = 0; i < ids.Length; i++) deck.Equip(deck.Add(ids[i]).InstanceId, (ManaSlot)i);
        MagicDeckStore.Write(player, deck);
        int Progress() => player.Creature.GetPowerAmount<PuzzlePower>();
        bool Alive() => enemies.All(enemy => enemy.IsAlive);
        async Task Reset()
        {
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            Hints.Clear();
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var mana in pile.Cards.ToArray()) pile.RemoveInternal(mana);
            foreach (var slot in Enum.GetValues<ManaSlot>()) await ManaCmd.Generate(player, ManaCmd.Pile(slot), 8);
        }
        Task Chant(int stage, ManaSlot? slot = null) => ChantCmd.Resolve(context, play,
            new(slot ?? (ManaSlot)stage, ids[stage], 1, 2 * (stage + 1), 1));

        var clueTypes = new[] { typeof(Factor100Power), typeof(Factor010Power), typeof(Factor001Power) };
        PowerModel[] Clues() => player.Creature.Powers.Where(p => p is PuzzleCluePower).ToArray();
        for (int stage=0;stage<3;stage++)
            foreach (var slot in Enum.GetValues<ManaSlot>().Where(s => (int)s!=stage)) {
                await Reset(); await Chant(stage, slot);
                Check(Progress() == 0 && Clues().Length == 0 && Hints.Count == 1 && Alive(),
                    "错误槽位只提示，不给予能力或推进谜题：" + ids[stage] + "/" + slot);
            }
        foreach (string language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language); await Reset(); await Chant(1, ManaSlot.Draw);
            Check(Hints.Single() == (language == "zhs" ? "这好像不太对…" : "This doesn’t seem right…"), "槽位错误使用原生角色气泡文案：" + language);
        }
        LocManager.Instance.SetLanguage("eng");
        foreach (int[] order in new int[][] { [1,0,2], [0,2,1], [1,2,0], [2,0,1], [2,1,0] }) {
            await Reset();
            foreach (int stage in order) await Chant(stage);
            Check(Progress() == PuzzlePower.BrokenSequence && Alive() && Hints.Count > 0,
                "错误顺序不触发击杀且提示：" + string.Join(",", order));
            Check(Clues().Select(p => p.GetType()).SequenceEqual(order.Select(i => clueTypes[i])),
                "错序仍按实际获得顺序保留三个独立能力：" + string.Join(",", order));
        }
        await Reset(); await Chant(0); await Chant(0); await Chant(1); await Chant(2);
        Check(Progress() == PuzzlePower.BrokenSequence && Clues().Length == 3 && Alive(), "重复第一段会令本回合失败，保留线索且无法重新完成");
        await Reset(); await Chant(2); await Chant(0); await Chant(1);
        await ManaCmd.Generate(player, PileType.Discard, 6); await Chant(2);
        Check(Progress() == PuzzlePower.BrokenSequence && Hints.Count == 4 && Alive(), "同回合错序后再补一遍正确顺序仍不能完成");
        await Reset(); await Chant(0); await Chant(1);
        await PowerCmd.Apply<StrengthPower>(context, player.Creature, 1, player.Creature, card);
        Check(Progress() == 2 && Clues().Length == 2 && Alive() && Hints.Count == 0, "正确前两段保留两个能力，其他能力不提前触发");
        foreach (var power in player.Creature.Powers.OfType<TurnOnlyPower>().ToArray())
            await power.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(Progress() == 0 && Clues().Length == 0 && Alive(), "未完成时回合结束统一移除线索和顺序状态");
        await Chant(1);
        foreach (var power in player.Creature.Powers.OfType<TurnOnlyPower>().ToArray())
            await power.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(Progress() == 0 && Clues().Length == 0, "错误顺序锁定同样在回合结束清除");
        Hints.Clear();
        await Reset(); await Chant(0); await Chant(1);
        int manaBefore = ManaCmd.InPile(player, PileType.Discard).Count();
        int hp = player.Creature.CurrentHp, round = combat.RoundNumber;
        Check(BeamCount == 0, "错误槽位、错误顺序和未完成时均不播放完成特效");
        var completing = Chant(2);
        Check(!completing.IsCompleted && Alive() && BeamCount == 0 && Delays.SequenceEqual(new[] { 1f }),
            "正确第三段先停顿1秒，敌人存活且尚未播放特效");
        KillDelay!.SetResult();
        Check(!completing.IsCompleted && Alive() && BeamCount == 1 && Math.Abs(DelaySeconds - 1.025f) < 0.001f,
            "停顿后启动原版光束，等待蓄力及光束结束期间敌人仍存活");
        // 直接加载原版场景验证光束阶段；TestMode 的工厂替身只用于控制结算暂停。
        var root = ((SceneTree)Engine.GetMainLoop()).Root;
        var nativeBeam = GD.Load<PackedScene>(NHyperbeamVfx.scenePath).Instantiate<NHyperbeamVfx>();
        nativeBeam.Position = new Vector2(350, 540); nativeBeam.ApplyRotation(new(350,540), new(1450,540));
        await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        RenderNative = true;
        root.AddChild(nativeBeam);
        Check(nativeBeam.Get(NHyperbeamVfx.PropertyName._laserLine).AsGodotObject() is Line2D { Visible: false },
            "原版光束先隐藏，保留蓄力阶段");
        await root.ToSignal(root.GetTree().CreateTimer(0.72), SceneTreeTimer.SignalName.Timeout);
        Check(nativeBeam.IsInsideTree() && nativeBeam.Get(NHyperbeamVfx.PropertyName._laserLine).AsGodotObject() is Line2D { Visible: true },
            "原版场景可加载并从蓄力进入光束阶段");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/puzzle-native-hyperbeam.png"));
        nativeBeam.QueueFree();
        RenderNative = false;
        KillDelay!.SetResult();
        Check(!completing.IsCompleted && Alive() && Delays.SequenceEqual(new[] { 1f, 1.025f, 1f }),
            "特效结束后再停顿1秒，不提前击杀敌人");
        KillDelay!.SetResult(); await completing; BeamStub!.Free();
        Check(enemies.All(enemy => enemy.IsDead) && Progress() == 3 && Clues().Length == 3 && Hints.Count == 0 && combat.RoundNumber == round,
            "第三段返回前两个敌人已通过原生命令死亡，无需结束回合");
        Check(player.Creature.CurrentHp == hp && ManaCmd.InPile(player, PileType.Discard).Count() == manaBefore - 6,
            "先支付正确牌堆魔力，击杀不影响玩家");
        // 击杀已走过原版胜利清理；终极咏唱使用独立的新战斗。
        CombatManager.Instance.Reset(graceful: true);
        combat = new CombatState(runState: run); combat.AddPlayer(player); enemies = SpawnEnemies();
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        MagicDeckStore.Write(player, deck);
        card = combat.CreateCard<FinalChant>(player);
        play = new CardPlay { Card = card, Target = null, IsAutoPlay = false, PlayIndex = 0, ResultPile = PileType.Discard, PlayCount = 1,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        await Reset(); Delays.Clear(); var finishing = ChantCmd.Final(context, play);
        Check(!finishing.IsCompleted && Alive() && BeamCount == 1 && Delays.SequenceEqual(new[] { 1f }), "终极咏唱完成后也先停顿1秒");
        KillDelay!.SetResult();
        Check(!finishing.IsCompleted && Alive() && BeamCount == 2 && Delays.Count == 2, "终极咏唱等待原版特效结束");
        KillDelay!.SetResult();
        Check(!finishing.IsCompleted && Alive() && Delays.SequenceEqual(new[] { 1f, 1.025f, 1f }), "终极咏唱特效后也保留1秒停顿");
        KillDelay!.SetResult(); await finishing; BeamStub!.Free();
        Check(enemies.All(enemy => enemy.IsDead) && Progress() == 3,
            "终极咏唱按槽位顺序集齐时也立即击杀全部敌人");
        hintPatch.UnpatchAll(hintPatch.Id);
        return count;
    }
}

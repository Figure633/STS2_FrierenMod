using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class ChantAnimationSpeedTests
{
    private static Node Root = null!;
    private static readonly List<(CardModel Card, double Step, int Block)> Exhausts = [];
    private static CardModel? ThrowOn;
    private static readonly List<(CardModel Card, double Step)> Generated = [];
    private static void Generating(CardModel card) => Generated.Add((card, Step(card)));
    private static bool SkipSound() => false;
    private static double Step(CardModel card)
    {
        var tween = Root.CreateTween(); tween.Pause();
        tween.TweenInterval(10);
        ManaAnimationSpeed.ApplySpeed(tween, [card]).CustomStep(0.1);
        double elapsed = tween.GetTotalElapsedTime(); tween.Kill(); return elapsed;
    }
    private static void Exhausting(CardModel card)
    {
        if (card == ThrowOn) throw new InvalidOperationException("模拟消耗中断");
        Exhausts.Add((card, Step(card), card.Owner.Creature.Block));
    }
    public static async Task<int> Run(Node root)
    {
        Root = root;
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); GD.Print("PASS 咏唱倍速：" + name); count++; }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-CHANT-SPEED");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null));
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        var context = new BlockingPlayerChoiceContext();
        var source = combat.CreateCard<FinalChant>(player);
        var play = new CardPlay { Card = source, Target = null, ResultPile = PileType.Discard, PlayCount = 1, PlayIndex = 0, IsAutoPlay = false,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        async Task<CardModel[]> Mana(PileType pile, int n, bool dense = false) {
            var cards = new List<CardModel>();
            for (int i = 0; i < n; i++) {
                CardModel card = dense && i == 0 ? combat.CreateCard<HighDensityMana>(player) : combat.CreateCard<Mana>(player);
                await CardPileCmd.Add(card, pile); cards.Add(card);
            }
            return cards.ToArray();
        }
        void Reset() {
            foreach (var pile in player.PlayerCombatState!.AllPiles)
                foreach (var card in pile.Cards.ToArray()) pile.RemoveInternal(card);
            Exhausts.Clear(); Generated.Clear(); CombatManager.Instance.History.Clear();
        }
        bool AtRate(double value) => Exhausts.All(e => Math.Abs(e.Step - value) < 0.0001);
        var harmony = new Harmony("frieren.tests.chant_animation_speed");
        var exhaust = AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Exhaust));
        harmony.Patch(exhaust, prefix: new HarmonyMethod(typeof(ChantAnimationSpeedTests), nameof(Exhausting)));
        var generate = AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.AddGeneratedCardToCombat));
        harmony.Patch(generate, prefix: new HarmonyMethod(typeof(ChantAnimationSpeedTests), nameof(Generating)));
        try {
            Reset(); var five = await Mana(PileType.Draw, 5);
            await ChantCmd.Resolve(context, play, new(ManaSlot.Draw, "AdvancedDefensiveMagic", 5, 5, 5));
            Check(Exhausts.Count == 5 && AtRate(0.1), "恰好五张保持原速");
            Reset(); var six = await Mana(PileType.Draw, 6, dense: true);
            await PowerCmd.Apply<RingletFormPower>(context, player.Creature, 1, player.Creature, null);
            int block = player.Creature.Block, energy = player.PlayerCombatState!.Energy;
            await ChantCmd.Resolve(context, play, new(ManaSlot.Draw, "BuildUpBody", 6, 6, 6));
            Check(Exhausts.Count == 6 && AtRate(0.2) && six.All(c => c.Pile?.Type == PileType.Exhaust), "六张逐张消耗，原生补间两倍速");
            Check(Exhausts.Select(e => e.Card).SequenceEqual(six) && Exhausts.Select(e => e.Block).SequenceEqual(Enumerable.Range(block, 6)),
                "每张消耗钩子完成后才开始下一张");
            Check(player.Creature.Block == block + 6 && player.PlayerCombatState.Energy == energy + 1, "卷发形态与高密度魔力的原生回调各触发准确次数");
            Check(six.All(c => Math.Abs(Step(c) - 0.1) < 0.0001), "结束支付后全部清理倍速标记");
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            Reset();
            var deck = MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,
                MagicDeckState.CreatePrototypeLoadout().Items.ToArray(), true, 3, StartingPropsGranted: true)));
            MagicDeckStore.Write(player, deck);
            foreach (var pile in new[] { PileType.Draw, PileType.Hand, PileType.Discard }) await Mana(pile, 2);
            await ChantCmd.Final(context, play);
            Check(Exhausts.Count == 6 && AtRate(0.2), "终极咏唱每槽两张、合计六张也全部两倍速");
            foreach (var power in player.Creature.Powers.ToArray()) await PowerCmd.Remove(power);
            Reset(); var shortfall = await Mana(PileType.Draw, 5);
            Check(!await ManaCmd.Spend(context, player, ManaSlot.Draw, 6, fastAnimation: true) && Exhausts.Count == 0 && ManaCmd.Total(player) == 5,
                "魔力不足不部分支付或残留倍速");
            Check(shortfall.All(c => Math.Abs(Step(c) - 0.1) < 0.0001), "失败支付不改变后续动画");
            Reset(); var interrupted = await Mana(PileType.Draw, 6); ThrowOn = interrupted[1];
            try { await ManaCmd.Spend(context, player, ManaSlot.Draw, 6, fastAnimation: true); throw new Exception("未模拟中断"); }
            catch (InvalidOperationException e) when (e.Message == "模拟消耗中断") { }
            finally { ThrowOn = null; }
            Check(interrupted.All(c => Math.Abs(Step(c) - 0.1) < 0.0001), "异常中断也清理所有倍速标记");
            var unrelated = combat.CreateCard<Mana>(player);
            using (ManaAnimationSpeed.Begin(interrupted)) {
                using (ManaAnimationSpeed.Begin(interrupted)) Check(Math.Abs(Step(interrupted[0]) - 0.2) < 0.0001, "嵌套作用域仍为两倍速");
                Check(Math.Abs(Step(interrupted[0]) - 0.2) < 0.0001 && Math.Abs(Step(unrelated) - 0.1) < 0.0001,
                    "嵌套退出保留外层标记，无关卡牌保持原速");
            }
            Reset(); var ear = await RelicCmd.Obtain<EarAccessory>(player);
            await ear.BeforeHandDraw(player, context, combat);
            Check(Generated.Count == 2 && Generated.All(e => Math.Abs(e.Step - 0.1) < 0.0001) && ManaCmd.InPile(player, PileType.Hand).Count() == 2,
                "耳饰两张魔力沿原生生成入口入手，补间保持原速");
            Check(Generated.All(e => Math.Abs(Step(e.Card) - 0.1) < 0.0001), "生成完成后清理加速标记");
            await ear.BeforeHandDraw(Player.CreateForNewRun<Frieren>(UnlockState.all, 2), context, combat);
            Check(Generated.Count == 2, "其他玩家的抽牌阶段不触发耳饰");
            Reset(); await Mana(PileType.Hand, CardPile.MaxCardsInHand);
            await ear.BeforeHandDraw(player, context, combat);
            Check(Generated.Count == 2 && Generated.All(e => Math.Abs(e.Step - 0.1) < 0.0001) &&
                ManaCmd.InPile(player, PileType.Hand).Count() == CardPile.MaxCardsInHand && ManaCmd.InPile(player, PileType.Discard).Count() == 2,
                "满手牌按原生规则溢出两张至弃牌堆");
            Reset(); await ManaCmd.Generate(player, PileType.Hand, 2);
            Check(Generated.Count == 2 && Generated.All(e => Math.Abs(e.Step - 0.1) < 0.0001), "普通卡牌生成魔力保持原速");

            // 用真实原版场景检查 _Ready 初始化后的飞行速度，以及离开作用域后仍能正常完成。
            harmony.Patch(AccessTools.Method(typeof(SfxCmd), nameof(SfxCmd.Play), [typeof(string), typeof(float)]),
                prefix: new HarmonyMethod(typeof(ChantAnimationSpeedTests), nameof(SkipSound)));
            var flights = new List<NCardFlyVfx>();
            foreach (bool fast in new[] { false, true }) {
                var model = combat.CreateCard<Mana>(player); await CardPileCmd.Add(model, PileType.Discard);
                var node = NodePool.Get<NCard>();
                AccessTools.Property(typeof(NCard), nameof(NCard.Model)).SetValue(node, model);
                root.AddChild(node); node.UpdateVisuals(PileType.Discard, CardPreviewMode.Normal);
                var flight = GD.Load<PackedScene>("res://scenes/vfx/vfx_card_fly.tscn").Instantiate<NCardFlyVfx>();
                flight.Set(NCardFlyVfx.PropertyName._card, node);
                flight.Set(NCardFlyVfx.PropertyName._startPos, new Vector2(400, 300));
                flight.Set(NCardFlyVfx.PropertyName._endPos, new Vector2(900, 500));
                using (fast ? ManaAnimationSpeed.Begin([model]) : null) root.AddChild(flight);
                float speed = flight.Get(NCardFlyVfx.PropertyName._speed).AsSingle();
                float accel = flight.Get(NCardFlyVfx.PropertyName._accel).AsSingle();
                Check(fast ? speed >= 2.2f && speed <= 2.5f && accel >= 8 && accel <= 10 :
                    speed >= 1.1f && speed <= 1.25f && accel >= 2 && accel <= 2.5f,
                    fast ? "原版飞入初速度乘2、加速度乘4" : "无关原版飞入的速度不变");
                flights.Add(flight);
            }
            var completion = Task.WhenAll(flights.Select(f => f.SwooshAwayCompletion!.Task));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!completion.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(5))
                await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(completion.IsCompletedSuccessfully, "原版飞入离开加速作用域后仍正常完成并释放卡牌");
            for (int i = 0; i < 5; i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        finally { harmony.UnpatchAll(harmony.Id); ThrowOn = null; }
        return count;
    }
}

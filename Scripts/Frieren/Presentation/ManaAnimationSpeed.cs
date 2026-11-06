using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace FrierenMod.Scripts.Presentation;

// 仅标记本次生成或支付的卡牌；移动、飞入、消耗及玩法钩子继续由原版执行。
public static class ManaAnimationSpeed
{
    private sealed class Counter { public List<float> Speeds = []; }
    private static readonly ConditionalWeakTable<CardModel, Counter> Accelerated = new();

    public static IDisposable Begin(IEnumerable<CardModel> cards, float speed = 2)
    {
        if (!float.IsFinite(speed) || speed <= 0) throw new ArgumentOutOfRangeException(nameof(speed));
        return new Scope(cards.Distinct().ToArray(), speed);
    }

    private sealed class Scope : IDisposable
    {
        private readonly CardModel[] _cards;
        private readonly float _speed;
        private bool _disposed;
        public Scope(CardModel[] cards, float speed)
        {
            _cards = cards;
            _speed = speed;
            foreach (var card in cards) Accelerated.GetOrCreateValue(card).Speeds.Add(speed);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var card in _cards) {
                var speeds = Accelerated.GetOrCreateValue(card).Speeds;
                speeds.Remove(_speed);
                if (speeds.Count == 0) Accelerated.Remove(card);
            }
        }
    }

    public static Tween ApplySpeed(Tween tween, IEnumerable<CardModel> cards)
    {
        bool any = false;
        float speed = float.PositiveInfinity;
        foreach (var card in cards)
        {
            if (!Accelerated.TryGetValue(card, out var counter) || counter.Speeds.Count == 0) return tween;
            any = true;
            speed = Math.Min(speed, counter.Speeds.Max());
        }
        return any ? tween.SetSpeedScale(speed) : tween;
    }

    public static void ApplyFlightSpeed(NCardFlyVfx flight)
    {
        if (flight.Get(NCardFlyVfx.PropertyName._card).AsGodotObject() is not NCard { Model: { } card } ||
            !Accelerated.TryGetValue(card, out var counter) || counter.Speeds.Count == 0) return;
        float speed = counter.Speeds.Max();
        flight.Set(NCardFlyVfx.PropertyName._speed, flight.Get(NCardFlyVfx.PropertyName._speed).AsSingle() * speed);
        flight.Set(NCardFlyVfx.PropertyName._accel, flight.Get(NCardFlyVfx.PropertyName._accel).AsSingle() * speed * speed);
    }

    [HarmonyPatch(typeof(NCardFlyVfx), nameof(NCardFlyVfx._Ready))]
    private static class NativeFlightSpeedPatch
    {
        private static void Postfix(NCardFlyVfx __instance) => ApplyFlightSpeed(__instance);
    }

    private static readonly MethodInfo? TweenFactory = AccessTools.Method(typeof(CardPileCmd), "GetTweenForCardsChangingPiles",
        [typeof(IEnumerable<CardPileAddResult>),typeof(bool)]);

    [HarmonyPatch]
    private static class NativePileTweenPatch
    {
        private static bool Prepare() => TweenFactory != null;
        private static MethodBase TargetMethod() => TweenFactory!;
        // 新版将牌堆补间提取为公开工厂，直接调整返回的原生补间。
        private static void Postfix(IEnumerable<CardPileAddResult> results, (Tween?, bool) __result)
        {
            if (__result.Item1 is { } tween)
                ApplySpeed(tween, results.Where(r => r.success).Select(r => r.cardAdded));
        }
    }

    [HarmonyPatch]
    private static class MainPileTweenPatch
    {
        private static bool Prepare() => TweenFactory == null;
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [typeof(IEnumerable<CardModel>),typeof(CardPile),typeof(CardPilePosition),typeof(AbstractModel),typeof(bool)])
            .StateMachineType().GetMethod("MoveNext", BindingFlags.Instance|BindingFlags.NonPublic)!;
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var createTween=AccessTools.Method(typeof(Node),nameof(Node.CreateTween));
            var cards=__originalMethod.DeclaringType!.FindStateMachineField("cards");
            int matches=0;
            foreach(var instruction in instructions) {
                yield return instruction;
                if(!instruction.Calls(createTween))continue;
                matches++;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld,cards);
                yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(ManaAnimationSpeed),nameof(ApplySpeed)));
            }
            if(matches!=1)throw new InvalidOperationException("正式版牌堆补间接口改变，需核查咏唱速度");
        }
    }
}

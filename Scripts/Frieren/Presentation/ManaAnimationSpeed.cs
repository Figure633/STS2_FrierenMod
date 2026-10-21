using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
    private sealed class Counter { public int Depth; }
    private static readonly ConditionalWeakTable<CardModel, Counter> Accelerated = new();

    public static IDisposable Begin(IEnumerable<CardModel> cards) => new Scope(cards.Distinct().ToArray());

    private sealed class Scope : IDisposable
    {
        private readonly CardModel[] _cards;
        private bool _disposed;
        public Scope(CardModel[] cards)
        {
            _cards = cards;
            foreach (var card in cards) Accelerated.GetOrCreateValue(card).Depth++;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var card in _cards)
                if (--Accelerated.GetOrCreateValue(card).Depth == 0) Accelerated.Remove(card);
        }
    }

    public static Tween ApplySpeed(Tween tween, IEnumerable<CardModel> cards)
    {
        bool any = false;
        foreach (var card in cards)
        {
            if (!Accelerated.TryGetValue(card, out var counter) || counter.Depth == 0) return tween;
            any = true;
        }
        return any ? tween.SetSpeedScale(2) : tween;
    }

    public static void ApplyFlightSpeed(NCardFlyVfx flight)
    {
        if (flight.Get(NCardFlyVfx.PropertyName._card).AsGodotObject() is not NCard { Model: { } card } ||
            !Accelerated.TryGetValue(card, out var counter) || counter.Depth == 0) return;
        // 原版飞行积分 v += a*dt；二倍速对应初速度乘2、加速度乘4。
        flight.Set(NCardFlyVfx.PropertyName._speed, flight.Get(NCardFlyVfx.PropertyName._speed).AsSingle() * 2);
        flight.Set(NCardFlyVfx.PropertyName._accel, flight.Get(NCardFlyVfx.PropertyName._accel).AsSingle() * 4);
    }

    [HarmonyPatch(typeof(NCardFlyVfx), nameof(NCardFlyVfx._Ready))]
    private static class NativeFlightSpeedPatch
    {
        private static void Postfix(NCardFlyVfx __instance) => ApplyFlightSpeed(__instance);
    }

    [HarmonyPatch]
    private static class NativePileTweenPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)])
            .StateMachineType().GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // 在原版 Add 创建的补间上设置速度，保留全部原生动画内容及快进设置。
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var createTween = AccessTools.Method(typeof(Node), nameof(Node.CreateTween));
            var cards = __originalMethod.DeclaringType!.FindStateMachineField("cards");
            int matches = 0;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!instruction.Calls(createTween)) continue;
                matches++;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, cards);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ManaAnimationSpeed), nameof(ApplySpeed)));
            }
            if (matches != 1) throw new InvalidOperationException("原版卡牌堆补间接口已改变，需重新适配咏唱动画速度。");
        }
    }
}

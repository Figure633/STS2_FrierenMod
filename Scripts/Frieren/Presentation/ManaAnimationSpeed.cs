using System.Runtime.CompilerServices;
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

    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.GetTweenForCardsChangingPiles),
        [typeof(IEnumerable<CardPileAddResult>), typeof(bool)])]
    private static class NativePileTweenPatch
    {
        // 新版将牌堆补间提取为公开工厂，直接调整返回的原生补间。
        private static void Postfix(IEnumerable<CardPileAddResult> results, (Tween?, bool) __result)
        {
            if (__result.Item1 is { } tween)
                ApplySpeed(tween, results.Where(r => r.success).Select(r => r.cardAdded));
        }
    }
}

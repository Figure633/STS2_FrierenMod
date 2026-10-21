using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BaseLib.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Presentation;

// 仅标记本次支付的卡牌。原版继续逐张移动、播放消耗特效并执行全部钩子。
public static class ManaExhaustAnimation
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
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ManaExhaustAnimation), nameof(ApplySpeed)));
            }
            if (matches != 1) throw new InvalidOperationException("原版卡牌堆补间接口已改变，需重新适配咏唱动画速度。");
        }
    }
}

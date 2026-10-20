using System.Reflection;
using System.Runtime.CompilerServices;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace FrierenMod.Scripts.Presentation;

public static class SynchroPresentation
{
    public static string Key(CardModel card)
    {
        var creature = card.IsMutable ? card.Owner?.Creature : null;
        bool accelerated = creature?.HasPower<AccelerateFlowPower>() == true;
        bool thunder = creature?.HasPower<ThunderPower>() == true;
        return (accelerated, thunder) switch {
            (true, true) => "ULTIMATE_ACCELERATED_SYNCHRO",
            (true, false) => "ACCELERATED_SYNCHRO",
            (false, true) => "ULTIMATE_SYNCHRO",
            _ => "SYNCHRO"
        };
    }

    public static string ArtName(CardModel card) => Key(card) switch {
        "ACCELERATED_SYNCHRO" => "Mana2",
        "ULTIMATE_SYNCHRO" => "Mana3",
        "ULTIMATE_ACCELERATED_SYNCHRO" => "Mana4",
        _ => "Mana"
    };

    // 订阅原生能力事件，现有卡面立即刷新；离开场景时解绑以适配对象池复用。
    private static readonly ConditionalWeakTable<NCard, Subscription> Subscriptions = new();

    internal static void Observe(NCard node)
    {
        if (!node.IsNodeReady() || !node.IsInsideTree()) return;
        var creature = node.Model is IManaCard && node.Model.IsMutable ? node.Model.Owner?.Creature : null;
        if (Subscriptions.TryGetValue(node, out var previous) && previous.Creature != creature) Forget(node);
        if (creature != null && !Subscriptions.TryGetValue(node, out _))
            Subscriptions.Add(node, new Subscription(node, creature));
        if (node.Model is Mana) node.GetNode<TextureRect>("%Portrait").Texture = node.Model.Portrait;
    }

    internal static void Forget(NCard node)
    {
        if (!Subscriptions.TryGetValue(node, out var previous)) return;
        previous.Dispose(); Subscriptions.Remove(node);
    }

    private sealed class Subscription : IDisposable
    {
        private readonly NCard _node;
        public Creature Creature { get; }
        public Subscription(NCard node, Creature creature)
        {
            _node = node; Creature = creature;
            creature.PowerApplied += Changed; creature.PowerRemoved += Changed;
            creature.PowerIncreased += Increased; creature.PowerDecreased += Decreased;
        }
        private void Increased(PowerModel power, int amount, bool silent) => Changed(power);
        private void Decreased(PowerModel power, bool silent) => Changed(power);
        private void Changed(PowerModel power)
        {
            if (power is not AccelerateFlowPower and not ThunderPower || !GodotObject.IsInstanceValid(_node) ||
                !_node.IsInsideTree() || _node.Model is not IManaCard) return;
            _node.UpdateVisuals(_node.DisplayingPile, CardPreviewMode.Normal);
        }
        public void Dispose()
        {
            Creature.PowerApplied -= Changed; Creature.PowerRemoved -= Changed;
            Creature.PowerIncreased -= Increased; Creature.PowerDecreased -= Decreased;
        }
    }
}

[HarmonyPatch]
internal static class SynchroCardVisuals
{
    private static IEnumerable<MethodBase> TargetMethods() => [
        AccessTools.Method(typeof(NCard), nameof(NCard._Ready)),
        AccessTools.Method(typeof(NCard), nameof(NCard._EnterTree)),
        AccessTools.Method(typeof(NCard), nameof(NCard.UpdateVisuals)),
        AccessTools.PropertySetter(typeof(NCard), nameof(NCard.Model))
    ];
    private static void Postfix(NCard __instance) => SynchroPresentation.Observe(__instance);
}

[HarmonyPatch(typeof(NCard), nameof(NCard._ExitTree))]
internal static class SynchroCardExit
{
    private static void Prefix(NCard __instance) => SynchroPresentation.Forget(__instance);
}

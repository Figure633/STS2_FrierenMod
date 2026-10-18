using FrierenMod.Core;
using FrierenMod.Scripts.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Mechanics;

// 保留原生星星支付和资源钩子，只替换想象力持续期间的普通能量费用。
[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
public static class ManaPayment
{
    public static void Prefix(CardModel __instance, out int __state) =>
        __state = __instance.Owner.Creature.GetPower<ManaInsteadOfEnergyPower>()?.Cost(__instance) ?? 0;

    public static void Postfix(CardModel __instance, int __state, ref Task<(int, int)> __result)
    {
        if (__state > 0) __result = Pay(__instance, __state, __result);
    }

    private static async Task<(int, int)> Pay(CardModel card, int amount, Task<(int, int)> original)
    {
        var result = await original;
        if (ManaCmd.Total(card.Owner) < amount) throw new InvalidOperationException("用于代付能量的魔力不足。");
        foreach (var slot in Enum.GetValues<ManaSlot>())
        {
            int spend = Math.Min(amount, ManaCmd.InPile(card.Owner, ManaCmd.Pile(slot)).Count());
            await ManaCmd.Spend(new ThrowingPlayerChoiceContext(), card.Owner, slot, spend);
            amount -= spend;
        }
        return result;
    }
}

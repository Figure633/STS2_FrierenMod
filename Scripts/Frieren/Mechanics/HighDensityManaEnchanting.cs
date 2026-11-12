using System.Reflection.Emit;
using FrierenMod.Scripts.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Mechanics;

// 只放宽高密度魔力的状态牌禁令，其余原生附魔条件与附魔自身限制照常执行。
[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.CanEnchant))]
internal static class HighDensityManaEnchanting
{
    private static CardType TypeForBaseRestriction(CardModel card) => card is HighDensityMana ? CardType.Skill : card.Type;
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool replaced = false;
        foreach (var code in instructions) {
            if (!replaced && code.Calls(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Type)))) {
                code.opcode = OpCodes.Call;
                code.operand = AccessTools.Method(typeof(HighDensityManaEnchanting), nameof(TypeForBaseRestriction));
                replaced = true;
            }
            yield return code;
        }
        if (!replaced) throw new InvalidOperationException("未找到原生附魔类型检查。");
    }
}

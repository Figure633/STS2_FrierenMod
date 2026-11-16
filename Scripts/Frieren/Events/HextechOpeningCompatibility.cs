using System.Reflection;
using System.Reflection.Emit;
using FrierenMod.Scripts.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

// 海克斯在 StartRun 完成后自动添加 Modifier；芙莉莲此时仍在选因子，
// 稍后进入涅奥便被原生判断当作自定义规则局，失去普通祝福。
// 仅在芙莉莲单人普通开局的涅奥判断中忽略该标记，不修改本局 Modifier。
[HarmonyPatch]
internal static class HextechOpeningCompatibility
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(Neow), "GenerateInitialOptions");
        yield return AccessTools.PropertyGetter(typeof(Neow), nameof(Neow.InitialDescription));
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        var modifiers = AccessTools.PropertyGetter(typeof(IRunState), nameof(IRunState.Modifiers));
        var count = AccessTools.PropertyGetter(typeof(IReadOnlyCollection<ModifierModel>), nameof(IReadOnlyCollection<ModifierModel>.Count));
        var matches = Enumerable.Range(1, code.Count - 1)
            .Where(i => code[i - 1].Calls(modifiers) && code[i].Calls(count)).ToArray();
        if (matches.Length != 1) {
            Log.Warn($"FrierenMod: 涅奥接口已变化，未应用海克斯开局适配：{__originalMethod.Name}。");
            return code;
        }
        int index = matches[0];
        var owner = new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(code[index]).MoveBlocksFrom(code[index]);
        code[index] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(HextechOpeningCompatibility), nameof(CountOpeningModifiers)));
        code.Insert(index, owner);
        return code;
    }

    private static int CountOpeningModifiers(IReadOnlyCollection<ModifierModel> modifiers, Neow neow)
    {
        var owner = neow.Owner;
        if (owner?.Character is not Frieren || owner.RunState.GameMode != GameMode.Standard ||
            owner.RunState.CurrentActIndex != 0 || owner.RunState.Players.Count != 1)
            return modifiers.Count;
        return modifiers.Count(m => m.GetType().FullName != "HextechRunes.HextechMayhemModifier" ||
            m.GetType().Assembly.GetName().Name != "HextechRunes");
    }
}

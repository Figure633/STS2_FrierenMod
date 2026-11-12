using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace FrierenMod.Scripts.Ancients;

// 次数来自原生角色访问记录：0、1各一次，之后2、3交替；不使用随机重复池。
public static class SerieDialogue
{
    private const string Prefix = "FRIERENMOD-SERIE.talk.";
    private const string WaveKey = Prefix + "FRIERENMOD-FRIEREN.3-0.ancient";

    [HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
    private static class SelectInOrder
    {
        private static void Postfix(AncientDialogueSet __instance, ModelId characterId, int charVisits,
            ref IEnumerable<AncientDialogue> __result)
        {
            if (!__instance.CharacterDialogues.TryGetValue(characterId.Entry, out var dialogues) ||
                dialogues.Count != 4 || dialogues[0].Lines[0].LineText?.LocEntryKey != Prefix + characterId.Entry + ".0-0.ancient") return;
            var index = Math.Max(0, charVisits);
            __result = [dialogues[index < 2 ? index : 2 + index % 2]];
        }
    }

    private sealed class ContinueGate { public bool Accepted; }
    private static readonly ConditionalWeakTable<NAncientEventLayout, ContinueGate> Gates = new();
    private static readonly System.Reflection.MethodInfo AnimateLine = AccessTools.Method(typeof(NAncientEventLayout), "SetDialogueLineAndAnimate");

    // 单句旁白也保留原生“继续”按钮，按下后在同一条对话下展开遗物，不添加角色台词。
    [HarmonyPatch(typeof(NAncientEventLayout), nameof(NAncientEventLayout.SetDialogue))]
    private static class PrepareContinue
    {
        private static void Postfix(NAncientEventLayout __instance, IReadOnlyList<AncientDialogueLine> lines)
        {
            Gates.Remove(__instance);
            if (lines.Count != 1 || lines[0].LineText?.LocEntryKey != WaveKey) return;
            lines[0].NextButtonText = new LocString("ancients", Prefix + "FRIERENMOD-FRIEREN.3-0.next");
            Gates.Add(__instance, new ContinueGate());
        }
    }

    [HarmonyPatch(typeof(NAncientEventLayout), "IsDialogueOnLastLine", MethodType.Getter)]
    private static class WaitForContinue
    {
        private static void Postfix(NAncientEventLayout __instance, ref bool __result)
        {
            if (Gates.TryGetValue(__instance, out var gate) && !gate.Accepted) __result = false;
        }
    }

    [HarmonyPatch(typeof(NAncientEventLayout), "OnDialogueHitboxClicked")]
    private static class AcceptContinue
    {
        private static bool Prefix(NAncientEventLayout __instance)
        {
            if (!Gates.TryGetValue(__instance, out var gate) || gate.Accepted) return true;
            gate.Accepted = true;
            AnimateLine.Invoke(__instance, [0]);
            return false;
        }
    }
}

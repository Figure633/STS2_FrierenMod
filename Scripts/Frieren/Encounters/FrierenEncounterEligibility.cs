using FrierenMod.Scripts.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Encounters;

// 原生 Act 会缓存候选池；每次取池时按当前队伍过滤，不能把角色条件缓存到全局模型中。
[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllBossEncounters), MethodType.Getter)]
public static class FrierenEncounterEligibility
{
    public static bool IsFrierenRun(IRunState? run) => run is { Players.Count: > 0 } && run.Players.All(p => p.Character is Frieren);
    private static void Postfix(ref IEnumerable<EncounterModel> __result) {
        if (!IsFrierenRun(RunManager.Instance.DebugOnlyGetState()))
            __result = __result.Where(e => e is not SpiegelFrierenEncounter and not AuraEncounter);
        else if (!Presentation.PresentationSettings.AllowOtherActTwoBosses && __result.Any(e => e is AuraEncounter))
            __result = __result.Where(e => e is AuraEncounter);
    }

    [HarmonyPatch(typeof(ActModel), nameof(ActModel.ValidateRoomsAfterLoad))]
    private static class RepairSavedBoss
    {
        private static void Postfix(ActModel __instance, Rng rng) {
            if (IsFrierenRun(RunManager.Instance.DebugOnlyGetState())) return;
            if (__instance.BossEncounter is SpiegelFrierenEncounter or AuraEncounter)
                __instance.SetBossEncounter(rng.NextItem(__instance.AllBossEncounters.Where(e => e.Id != __instance.SecondBossEncounter?.Id)));
            if (__instance.SecondBossEncounter is SpiegelFrierenEncounter or AuraEncounter)
                __instance.SetSecondBossEncounter(rng.NextItem(__instance.AllBossEncounters.Where(e => e.Id != __instance.BossEncounter.Id)));
        }
    }
}

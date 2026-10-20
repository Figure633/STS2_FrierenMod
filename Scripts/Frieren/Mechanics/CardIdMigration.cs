using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Mechanics;

// 只在读取存档中的模型标识时转换旧名；注册、资源和新存档统一使用新 ID。
[HarmonyPatch(typeof(ModelId), nameof(ModelId.Deserialize))]
public static class CardIdMigration
{
    private static readonly IReadOnlyDictionary<string, string> Entries = new Dictionary<string, string>
    {
        ["FRIERENMOD-FREE"] = "FRIERENMOD-VIOLENT_EXORCISM",
        ["FRIERENMOD-HALF_THE_SKY"] = "FRIERENMOD-ENVY",
        ["FRIERENMOD-METALLICIZ_FLOWER"] = "FRIERENMOD-METAL_PETAL_SPELL",
        ["FRIERENMOD-SAY_THINKING_SPELL"] = "FRIERENMOD-SPEAK_FROM_HEART_SPELL",
        ["FRIERENMOD-PREPARED_POSTURE"] = "FRIERENMOD-READY_STANCE",
        ["FRIERENMOD-CONTINUOUS_SHOOTING"] = "FRIERENMOD-SUSTAINED_BARRAGE"
    };

    [HarmonyPostfix]
    private static void ReadLegacyId(ref ModelId __result)
    {
        if (__result.Category == "CARD" && Entries.TryGetValue(__result.Entry, out var entry))
            __result = new ModelId(__result.Category, entry);
    }
}

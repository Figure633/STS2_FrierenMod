using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Mechanics;

// 海克斯魔盒使用独立变牌助手，绕过CardFactory；在抽样前复用相同候选规则。
[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
public static class HextechCardPoolCompatibility
{
    private static readonly HashSet<Assembly> Patched = [];
    [HarmonyPrefix]
    private static void Initialize()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Install(assembly);
    }
    public static void Install(Assembly assembly)
    {
        var type = assembly.GetType("HextechRunes.CardTransformUpgradeHelper");
        if (type == null || Patched.Contains(assembly)) return;
        var method = AccessTools.Method(type, "GetStableTransformationOptions");
        if (method == null || method.GetParameters().Length != 3) return;
        new Harmony("sts2.arkalin.frierenmod.hextech-cards").Patch(method,
            prefix: new HarmonyMethod(typeof(HextechCardPoolCompatibility), nameof(Filter)));
        Patched.Add(assembly);
    }
    private static void Filter(CardModel __0, ref IEnumerable<CardModel> __1) =>
        __1 = FrierenCardAvailability.Cards(__1, __0.Owner);
}

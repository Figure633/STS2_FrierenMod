using System.Collections;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace FrierenMod.Scripts.Presentation;

// 第三方按卡牌ID保存的原图可能已被原生AssetCache释放；重新记录前剔除失效项。
[HarmonyPatch(typeof(LocManager), nameof(LocManager.Initialize))]
public static class RegentPortraitCompatibility
{
    private static readonly HashSet<Assembly> Patched = [];
    [HarmonyPrefix]
    private static void Initialize()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Install(assembly);
    }
    public static void Install(Assembly assembly)
    {
        var type = assembly.GetType("regentCardsAnimeRework.PortraitPatch");
        if (type == null || Patched.Contains(assembly)) return;
        var method = AccessTools.Method(type, "RememberDefaultState");
        if (method == null || method.GetParameters().Length != 3) return;
        new Harmony("sts2.arkalin.frierenmod.regent-portraits").Patch(method,
            prefix: new HarmonyMethod(typeof(RegentPortraitCompatibility), nameof(RemoveInvalid)));
        Patched.Add(assembly);
    }
    private static void RemoveInvalid(string __0, object __2)
    {
        if (__2 is not IDictionary cache || !cache.Contains(__0)) return;
        var state = cache[__0];
        var texture = state?.GetType().GetProperty("Texture")?.GetValue(state) as Texture2D;
        if (texture != null && !GodotObject.IsInstanceValid(texture)) cache.Remove(__0);
    }
}

using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace FrierenMod.Scripts.Presentation;

// 原生先古标记将整张图片染黑；赛莉耶使用作者选定的圆形彩色头像。
[HarmonyPatch]
internal static class SerieMapPortrait
{
    private static IEnumerable<MethodBase> TargetMethods() => new[] { "TraveledColor", "UntravelableColor", "HoveredColor" }
        .Select(name => (MethodBase)AccessTools.PropertyGetter(typeof(NAncientMapPoint), name));
    private static void Postfix(NAncientMapPoint __instance, MethodBase __originalMethod, ref Color __result)
    {
        if (__instance.GetNodeOrNull<TextureRect>("Icon")?.Texture?.ResourcePath != "res://FrierenMod/images/ancients/serie_node.png") return;
        float brightness = __originalMethod.Name == "get_TraveledColor" && __instance.State == MapPointState.Traveled ? .75f : 1;
        __result = new Color(brightness, brightness, brightness, __result.A);
    }
}

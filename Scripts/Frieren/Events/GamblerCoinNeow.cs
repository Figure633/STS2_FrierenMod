using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace FrierenMod.Scripts.Events;
[HarmonyPatch(typeof(Neow),"PositiveOptions",MethodType.Getter)]
internal static class GamblerCoinNeow
{
    private static void Postfix(Neow __instance,ref IEnumerable<EventOption> __result)
    {
        if(__instance.Owner is {} player&&player.Character is not Frieren)return;
        var method=typeof(AncientEventModel).GetMethods(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
            .Single(m=>m.Name=="RelicOption"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==2&&m.GetParameters().All(p=>p.ParameterType==typeof(string))).MakeGenericMethod(typeof(GamblerCoin));
        __result=__result.Append((EventOption)method.Invoke(__instance,["INITIAL","NEOW.pages.DONE.POSITIVE.description"])!);
    }
}

using FrierenMod.Scripts.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using Godot;

namespace FrierenMod.Scripts.Presentation;

// 一代的 normal_attack 是特定卡牌语音，不是每次动作的通用音效。
// 只接管芙莉莲的三个通用动作，保留动作时序及卡牌/命中/VFX 自身的声音。
[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
public static class FrierenCombatSound
{
    public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
    {
        if (!creature.IsPlayer || creature.Player?.Character is not Frieren ||
            triggerName is not ("Attack" or "Cast" or "PowerUp")) return true;
        __result = Animate(creature, triggerName, waitTime);
        return false;
    }

    private static async Task Animate(Creature creature, string triggerName, float waitTime)
    {
        var node = creature.GetCreatureNode();
        if (node == null || creature.IsDead) return;
        node.SetAnimationTrigger(triggerName);
        await Cmd.CustomScaledWait(Mathf.Min(waitTime * 0.5f, 0.25f), waitTime);
    }
}

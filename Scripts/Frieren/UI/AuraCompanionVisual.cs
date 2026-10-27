using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.UI;

public static class AuraCompanionVisual
{
    public static AnimatedSprite2D Create()
    {
        // 原 GIF 等比例减速两倍；着色器只去除米色背景，不改动原始帧。
        var shader = new Shader { Code = """
            shader_type canvas_item;
            void fragment() {
                vec4 c = texture(TEXTURE, UV);
                float d = distance(c.rgb, vec3(0.82, 0.773, 0.714));
                c.a *= smoothstep(0.085, 0.13, d);
                // 去掉原 GIF 的落地道具与米色阴影，只保留角色及她手中的剑。
                if (UV.x > 0.80 || (UV.y > 0.88 && c.r > 0.40 && c.g > 0.40 && abs(c.r-c.g) < 0.08)) c.a = 0.0;
                COLOR = c;
            }
            """ };
        var sprite = new AnimatedSprite2D {
            Name = "FrierenAuraCompanion", SpriteFrames = GD.Load<SpriteFrames>("res://FrierenMod/images/pets/aura.tres"),
            Position = new Vector2(-130, -70), Scale = new Vector2(0.6f, 0.6f),
            Material = new ShaderMaterial { Shader = shader }
        };
        sprite.Play();
        return sprite;
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
    private static class Attach
    {
        private static void Postfix(NCreature __instance)
        {
            if (__instance.Entity?.Player?.GetRelic<AuraCompanion>() == null) return;
            // BaseLib 优先读取生物的直接 AnimatedSprite2D，不能让宠物抢走角色的 AnimationPlayer。
            var host = new Node2D { Name = "AuraCompanionHost" };
            __instance.AddChild(host);
            host.AddChild(Create());
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    private static class KeepCompanyAfterDeath
    {
        private static void Postfix(NCreature __instance)
        {
            if (__instance.Entity?.Player == null || __instance.GetNodeOrNull<Node2D>("AuraCompanionHost") is not { } host) return;
            // 只移动宠物宿主，角色死亡图仍由原有 Dead 动画控制。
            host.Position = new Vector2(-100, 0);
        }
    }

    [HarmonyPatch(typeof(RewardsSet), "GenerateRewardsFor")]
    private static class Reward
    {
        private static void Postfix(Player player, AbstractRoom room, List<MegaCrit.Sts2.Core.Rewards.Reward> __result)
        {
            if (room is CombatRoom { Encounter: AuraEncounter } && player.GetRelic<AuraCompanion>() == null &&
                !__result.OfType<RelicReward>().Any(r => r.Relic is AuraCompanion))
                __result.Add(new RelicReward(ModelDb.Relic<AuraCompanion>().ToMutable(), player));
        }
    }
}

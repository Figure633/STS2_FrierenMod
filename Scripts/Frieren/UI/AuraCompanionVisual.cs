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
            var pet=host.GetChild<AnimatedSprite2D>(0);
            var corpse=__instance.Visuals?.FindChild("Corpse",true,false) as Sprite2D;
            if(corpse==null) { host.Position=new Vector2(-200,0); return; }
            PlaceBesideCorpse(host,pet,corpse,__instance);
        }
    }

    public static void PlaceBesideCorpse(Node2D host,AnimatedSprite2D pet,Sprite2D corpse,Node creature)
    {
        // 与死亡图处在同一实际Z层，且用真实贴图边界留出间距，避免覆盖或挤在一起。
        int layer=0;
        for(Node? node=corpse;node!=null && node!=creature;node=node.GetParent())
            if(node is CanvasItem canvas) {layer+=canvas.ZIndex;if(!canvas.ZAsRelative)break;}
        host.ZIndex=layer;
        Rect2 bounds=corpse.GlobalTransform*corpse.GetRect();
        var frame=pet.SpriteFrames.GetFrameTexture(pet.Animation,pet.Frame);
        Vector2 half=frame.GetSize()*pet.GlobalScale.Abs()/2;
        pet.GlobalPosition=new Vector2(bounds.Position.X-40-half.X,bounds.End.Y-half.Y);
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

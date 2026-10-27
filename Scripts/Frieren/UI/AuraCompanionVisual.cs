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
    private static SpriteFrames? _croppedFrames;
    private static SpriteFrames Frames()
    {
        if(_croppedFrames!=null)return _croppedFrames;
        var source=GD.Load<SpriteFrames>("res://FrierenMod/images/pets/aura.tres");
        Rect2I? union=null;
        var images=new List<Image>();
        var animation=source.GetAnimationNames()[0];
        for(int frame=0;frame<source.GetFrameCount(animation);frame++) {
            var image=source.GetFrameTexture(animation,frame).GetImage();
            images.Add(image);
            if(image.IsCompressed())image.Decompress();
            int left=image.GetWidth(),top=image.GetHeight(),right=0,bottom=0;
            for(int y=0;y<image.GetHeight();y++)for(int x=0;x<image.GetWidth();x++) {
                var c=image.GetPixel(x,y);
                float distance=new Vector3(c.R-.82f,c.G-.773f,c.B-.714f).Length();
                c.A*=Mathf.SmoothStep(.085f,.13f,distance);
                if(x>.8f*image.GetWidth() || (y>.88f*image.GetHeight() && c.R>.4f && c.G>.4f && Math.Abs(c.R-c.G)<.08f))c.A=0;
                image.SetPixel(x,y,c);
                if(c.A<.1f)continue;
                left=Math.Min(left,x);right=Math.Max(right,x+1);top=Math.Min(top,y);bottom=Math.Max(bottom,y+1);
            }
            if(right>left && bottom>top) {
                var rect=new Rect2I(left,top,right-left,bottom-top);
                union=union.HasValue?union.Value.Merge(rect):rect;
            }
        }
        _croppedFrames=new SpriteFrames();_croppedFrames.RemoveAnimation("default");
        _croppedFrames.AddAnimation(animation);_croppedFrames.SetAnimationLoop(animation,true);
        _croppedFrames.SetAnimationSpeed(animation,source.GetAnimationSpeed(animation));
        for(int i=0;i<source.GetFrameCount(animation);i++) {
            using var image=images[i];
            using var cropped=image.GetRegion(union??new Rect2I(Vector2I.Zero,image.GetSize()));
            _croppedFrames.AddFrame(animation,ImageTexture.CreateFromImage(cropped),source.GetFrameDuration(animation,i));
        }
        return _croppedFrames;
    }

    public static AnimatedSprite2D Create()
    {
        // 原始帧逐张去背景后裁切；不使用图集UV，避免UV落在整张图集时误裁角色。
        var sprite = new AnimatedSprite2D {
            Name = "FrierenAuraCompanion", SpriteFrames = Frames(),
            Position = new Vector2(-135, -85), Scale = new Vector2(0.75f, 0.75f)
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
            // 明确使用玩家自己的动画，避免第三方自定义动画缓存选中陪伴节点。
            var animator=__instance.Visuals?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
            if(animator?.HasAnimation("Dead")==true && animator.CurrentAnimation!="Dead")animator.Play("Dead");
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
        Rect2 bounds=CorpseBounds(corpse);
        var frame=pet.SpriteFrames.GetFrameTexture(pet.Animation,pet.Frame);
        Vector2 half=frame.GetSize()*pet.GlobalScale.Abs()/2;
        pet.GlobalPosition=new Vector2(bounds.Position.X-24-half.X,bounds.End.Y-half.Y);
        pet.Modulate=Colors.White;pet.Show();pet.Play();
    }

    public static Rect2 CorpseBounds(Sprite2D corpse)
    {
        using var image=corpse.Texture.GetImage();if(image.IsCompressed())image.Decompress();
        var used=image.GetUsedRect();
        return corpse.GlobalTransform*new Rect2(corpse.GetRect().Position+(Vector2)used.Position,(Vector2)used.Size);
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

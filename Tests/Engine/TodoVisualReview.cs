using BaseLib.Utils;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

public static class TodoVisualReview
{
    private static bool SkipReady() => false;
    public static async Task<int> Run(Node root, Player player, RunState run)
    {
        int count=0;
        void Check(bool ok,string label) { if(!ok) throw new Exception(label); count++; GD.Print("PASS TODO视觉："+label); }
        var patches=new Harmony("frieren.tests.pet_visual");
        foreach(string method in new[]{"_Ready","_EnterTree","_ExitTree"})
            patches.Patch(AccessTools.Method(typeof(NCreature),method),prefix:new HarmonyMethod(typeof(TodoVisualReview),nameof(SkipReady)));
        player.AddRelicInternal(ModelDb.Relic<AuraCompanion>().ToMutable());
        var creature=new NCreature { Position=new Vector2(720,660) };
        AccessTools.Property(typeof(NCreature),nameof(NCreature.Entity)).SetValue(creature,player.Creature);
        ((Frieren)player.Character).RegisterSceneConversions();
        var avatar=player.Character.CreateVisuals();
        AccessTools.Property(typeof(NCreature),nameof(NCreature.Visuals)).SetValue(creature,avatar);
        creature.AddChild(avatar);root.AddChild(creature);
        Check(CustomAnimation.PlayCustomAnimation(creature,"Dead"),"携带阿乌拉时BaseLib仍能找到玩家死亡动画");
        avatar.GetNode<AnimationPlayer>("AnimationPlayer").Advance(.8);
        Check(avatar.GetNode<Sprite2D>("Visuals/Corpse").Visible && !avatar.GetNode<Node2D>("Visuals/Body").Visible,"玩家死亡图出现，原立绘隐藏");
        AccessTools.Method(AccessTools.Inner(typeof(AuraCompanionVisual),"KeepCompanyAfterDeath"),"Postfix").Invoke(null,[creature]);
        var petHost=creature.GetNode<Node2D>("AuraCompanionHost");
        var companion=petHost.GetChild<AnimatedSprite2D>(0);
        var corpse=avatar.GetNode<Sprite2D>("Visuals/Corpse");
        Rect2 corpseRect=corpse.GlobalTransform*corpse.GetRect();
        float petRight=companion.GlobalPosition.X+companion.SpriteFrames.GetFrameTexture(companion.Animation,companion.Frame).GetWidth()*companion.GlobalScale.X/2;
        Check(petRight<=corpseRect.Position.X-39 && companion.IsPlaying(),"死亡后宠物与尸体留出40像素间距并继续循环");
        var notice=new NowPlayingNotice();root.AddChild(notice);notice.SetTitle("Knife to the Throat");
        Check(notice.Position.Y>=172 && notice.GetChild(0).GetChild(0) is RecordDisc,"曲名提示位于遗物区下方，使用旋转唱片");
        var icon=new TextureRect { Texture=GD.Load<Texture2D>("res://FrierenMod/images/monsters/Aura_node.png"),
            Position=new Vector2(320,280),Size=new Vector2(128,128),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize };
        root.AddChild(icon);
        var currentIndex=run.CurrentActIndex;
        AccessTools.Property(typeof(RunState),nameof(RunState.CurrentActIndex)).SetValue(run,2);
        run.Act.SetBossEncounter(ModelDb.Encounter<SpiegelFrierenEncounter>());
        run.Act.SetSecondBossEncounter(ModelDb.Encounter<MegaCrit.Sts2.Core.Models.Encounters.QueenBoss>());
        var top=GD.Load<PackedScene>("res://scenes/ui/top_bar.tscn").Instantiate<Control>();
        foreach(var deckButton in top.FindChildren("*", "", true, false).OfType<MegaCrit.Sts2.Core.Nodes.TopBar.NTopBarDeckButton>())
            AccessTools.Field(deckButton.GetType(), "_pile").SetValue(deckButton, MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck.GetPile(player));
        var boss=top.GetNode<MegaCrit.sts2.Core.Nodes.TopBar.NTopBarBossIcon>("LeftAlignedStuff/RoomIcons/BossIcon");
        boss.GetParent().RemoveChild(boss);top.Free();root.AddChild(boss);
        boss.Position=new Vector2(1100,45);boss.Initialize(run);
        Check(boss.GetNode<TextureRect>("Icon").Size.IsEqualApprox(new Vector2(44,44)),"复制体顶栏遵守原生44像素边界");
        var second=(TextureRect)AccessTools.Field(boss.GetType(),"_secondBossIcon").GetValue(boss)!;
        Check(!second.Texture.ResourcePath.Contains("Spiegel") && second.Size.IsEqualApprox(new Vector2(44,44)),"女王保留原生图标与尺寸");
        Check(second.Position.X >= 48 && boss.CustomMinimumSize.X >= 96,"双Boss间距增加并为顶栏预留完整宽度");
        for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/redo-ui-review.png"));
        boss.QueueFree();icon.QueueFree();notice.QueueFree();creature.QueueFree();
        AccessTools.Property(typeof(RunState),nameof(RunState.CurrentActIndex)).SetValue(run,currentIndex);
        for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        patches.UnpatchAll("frieren.tests.pet_visual");return count;
    }
}

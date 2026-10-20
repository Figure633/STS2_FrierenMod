using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardFrameTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool valid,string name) { if(!valid) throw new Exception(name); GD.Print("PASS 卡背："+name); count++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FRIEREN-CARD-FRAMES");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        CardModel Card(Type type) {
            var card=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable(); card.Owner=player; return card;
        }
        var ui=new Control { Size=new(1920,1080) }; root.AddChild(ui);
        ui.AddChild(new ColorRect { Size=ui.Size,Color=new("17202c"),MouseFilter=Control.MouseFilterEnum.Ignore });
        var samples=new[] {
            (typeof(StrikeFrieren),"frieren_attack"),(typeof(DefendFrieren),"frieren_skill"),(typeof(RitesPreparation),"frieren_power"),
            (typeof(ManaDetection),"shared_attack"),(typeof(GrapeSpell),"shared_skill"),(typeof(FlyingMagic),"shared_power")
        };
        var nodes=new List<NCard>();
        for(int i=0;i<samples.Length;i++) {
            var (type,style)=samples[i]; var model=Card(type);
            var frame=(AtlasTexture)model.Frame;
            Check(frame.ResourcePath.EndsWith(style+".tres") && frame.Atlas.GetSize()==new Vector2(1024,1024) &&
                frame.Region.Size==new Vector2(600,844), type.Name+"使用对应的一代原图与卡框区域");
            var material=model.FrameMaterial as ShaderMaterial;
            Check(material?.Shader.ResourcePath.EndsWith("legacy_card_frame.gdshader")==true &&
                material.GetShaderParameter("frame_atlas").AsGodotObject() is Texture2D,
                type.Name+"通过原生卡面遮罩保留插画开口");
            var node=NodePool.Get<NCard>(); AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            node.Position=new(420+(i%3)*540,280+(i/3)*510); node.Scale=Vector2.One*.9f;
            ui.AddChild(node); node.UpdateVisuals(PileType.None,CardPreviewMode.Normal); nodes.Add(node);
        }
        foreach(var type in new[]{typeof(BuildUpBody),typeof(MysteryBox),typeof(Mana),typeof(HighDensityMana)}) {
            var card=Card(type); Check(((CustomCardModel)card).CustomFrame==null &&
                !card.Frame.ResourcePath.Contains("FrierenMod"),type.Name+"保留现有原生卡框");
        }
        foreach(var type in new[]{typeof(BabySleeping),typeof(ShavedIceSpell),typeof(ManaDetection),typeof(GrapeSpell),typeof(FlyingMagic),typeof(Disperse),typeof(ClearMind)})
            if(!Card(type).Frame.ResourcePath.Contains("shared_")) throw new Exception("双色卡遗漏："+type.Name);
        Check(true,"七张一代双色卡全部保留公共卡背");
        for(int i=0;i<5;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/card-frames.png"));
        foreach(var node in nodes) { ui.RemoveChild(node); node.QueueFreeSafely(); }
        ui.QueueFree();
        for(int i=0;i<4;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        return count;
    }
}

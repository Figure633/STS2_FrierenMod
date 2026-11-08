using System.Text.Json;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class CardArtAudit
{
    public static async Task<int> Run(Node root)
    {
        string folder=System.Environment.GetEnvironmentVariable("FRIEREN_CONTENT_AUDIT_DIR") ??
            ProjectSettings.GlobalizePath("res://../../artifacts/content-audit-current");
        var names=JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(folder,"native-models.json")))!;
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([player],seed:"ART-AUDIT");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var canvas=new Control{Size=new(1920,1080)};root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=canvas.Size,Color=new("526175")});
        using var atlas=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();ResourceLoader.AddResourceFormatLoader(atlas,true);
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");var records=new List<object>();
        for(int start=0;start<names.Length;start+=6){
            var nodes=new List<NCard>();
            foreach(var (name,i) in names.Skip(start).Take(6).Select((name,i)=>(name,i))){
                var type=typeof(Mana).Assembly.GetType("FrierenMod.Scripts.Cards."+name)!;
                var card=(CardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();card.Owner=player;
                var node=scene.Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,card);canvas.AddChild(node);
                node.Position=new(360+600*(i%3),290+510*(i/3));node.Scale=Vector2.One*.85f;node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);
            }
            for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            string filename=$"native-{start/6+1:00}.png";root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(folder,filename));
            var viewport=root.GetViewport().GetVisibleRect().Size;
            foreach(var node in nodes){var rect=node.GetNode<Control>("%Portrait").GetGlobalRect();records.Add(new{model=node.Model!.GetType().Name,name=node.Model.Title,file=filename,
                portrait=node.Model.PortraitPath,rect=new[]{rect.Position.X,rect.Position.Y,rect.Size.X,rect.Size.Y},viewport=new[]{viewport.X,viewport.Y}});}
            foreach(var node in nodes){canvas.RemoveChild(node);node.QueueFree();}
        }
        File.WriteAllText(Path.Combine(folder,"native-rects.json"),JsonSerializer.Serialize(records,new JsonSerializerOptions{WriteIndented=true}));
        canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);ResourceLoader.RemoveResourceFormatLoader(atlas);RunManager.Instance.CleanUp();return records.Count;
    }
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class CommissionFeedbackUiTests
{
    private static bool Skip()=>false;
    private static Vector2 Cursor;
    private static bool Mouse(ref Vector2 __result){__result=Cursor;return false;}
    private static bool NoRun(ref NRun? __result){__result=null;return false;}
    public static async Task<int> Run(Node root) {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 委托反馈界面："+text);}
        async Task Tick(){for(int i=0;i<8;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        SaveManager.Instance.InitPrefsDataForTest();
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([p],seed:"COMMISSION-FEEDBACK-UI");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;run.PushRoom(new RestSiteRoom());MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var harmony=new Harmony("frieren.tests.commission_hover");
        foreach(var method in new[]{"_EnterTree","_Ready","_ExitTree","_Input"})harmony.Patch(AccessTools.Method(typeof(NGame),method),prefix:new HarmonyMethod(typeof(CommissionFeedbackUiTests),nameof(Skip)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(NRun),nameof(NRun.Instance)),prefix:new HarmonyMethod(typeof(CommissionFeedbackUiTests),nameof(NoRun)));
        // 仅替代物理鼠标坐标，避免离屏截图窗口读取桌面坐标或移动作者鼠标。
        harmony.Patch(AccessTools.Method(typeof(CanvasItem),nameof(CanvasItem.GetGlobalMousePosition)),prefix:new HarmonyMethod(typeof(CommissionFeedbackUiTests),nameof(Mouse)));
        var game=new NGame();var input=new MegaCrit.Sts2.Core.Nodes.CommonUi.NInputManager();
        AccessTools.Property(typeof(NGame),nameof(NGame.InputManager)).SetValue(game,input);root.AddChild(game);
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        var hoverLayer=new CanvasLayer{Layer=100};game.AddChild(hoverLayer);var hoverRoot=new Control();hoverLayer.AddChild(hoverRoot);AccessTools.Property(typeof(NGame),nameof(NGame.HoverTipsContainer)).SetValue(game,hoverRoot);
        var ui=new Control{Size=new(1920,1080)};root.AddChild(ui);
        var board=new CommissionScreen(ui,p);board.Open();await Tick();
        var reward=(Control)board.View.FindChildren("Reward","Label",true,false).First();
        void Move(Vector2 point)=>Cursor=point;
        var hitbox=reward.GetGlobalRect();var start=hitbox.Position+new Vector2(60,hitbox.Size.Y/2);
        Move(start);await Tick();reward.EmitSignal(Control.SignalName.MouseEntered);await Tick();
        var tip=hoverRoot.GetChildren().OfType<NHoverTipSet>().Single();
        var contents=tip.GetChildren().OfType<Control>().Where(c=>c.Size.X>0&&c.Size.Y>0).ToArray();
        Check(contents.Length>0&&contents.All(c=>c.GlobalPosition.Length()>100),"原生悬停出现在鼠标旁，不落在左上角");
        Vector2 before=contents[0].GlobalPosition;Move(start+new Vector2(250,0));await Tick();

        Check(contents[0].GlobalPosition.DistanceTo(before)>100,"鼠标移动后提示跟随并重新排版");
        Check(contents.All(c=>root.GetViewport().GetVisibleRect().Grow(2).Encloses(c.GetGlobalRect())),"原生悬停内容在屏幕范围内");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-cursor-hover.png"));
        reward.EmitSignal(Control.SignalName.MouseExited);await Tick();Check(hoverRoot.GetChildCount()==0,"离开任务奖励后清除提示，不叠加旧文本框");
        var choice=board.View.FindChildren("CardChoice","Label",true,false).OfType<Control>().FirstOrDefault();
        if(choice!=null){
            Move(new(780,500));choice.EmitSignal(Control.SignalName.MouseEntered);await Tick();
            Check(hoverRoot.FindChildren("*","",true,false).OfType<NCard>().Count()==1,"三张奖励分别悬停，每次只展示当前卡牌，避免纵向叠满屏幕");
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-card-hover.png"));
            choice.EmitSignal(Control.SignalName.MouseExited);
        }
        board.Close();await Tick();
        board.View.QueueFree();ui.QueueFree();AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,null);game.QueueFree();await Tick();input.Free();harmony.UnpatchAll(harmony.Id);
        var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});
        using var atlas=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();ResourceLoader.AddResourceFormatLoader(atlas,true);
        var custom=(CustomLegendarySpell)ModelDb.Card<CustomLegendarySpell>().ToMutable();custom.Configure(2,[new("ZeroCost",1),new("Energy",1),new("Vulnerable",3)]);
        CardModel[] models=[ModelDb.Card<BlueMoonWeed>().ToMutable(),ModelDb.Card<GoodLuckSpell>().ToMutable(),ModelDb.Card<ValuableCargo>().ToMutable(),custom];
        var nodes=new List<NCard>();
        for(int i=0;i<models.Length;i++){
            var model=models[i];model.Owner=p;
            var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            canvas.AddChild(node);node.Position=new(285+i*450,540);node.Scale=Vector2.One*1.1f;node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);
        }
        await Tick();
        Check(nodes[0].GetNode<TextureRect>("%Portrait").Texture.GetSize()==new Vector2(500,380),"苍月草使用作者原始构图，尺寸符合250比190原生画框");
        Check(models[2].Type==CardType.Skill&&models[2].EnergyCost.Canonical<0&&!nodes[2].GetNode<TextureRect>("%EnergyIcon").Visible,"贵重商品为无费用技能卡");
        foreach(var node in nodes){var label=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(!label.Text.Contains('{')&&label.GetContentHeight()<=label.Size.Y+2,"卡面正文完整无占位符和溢出："+node.Model!.Id);}
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/feedback-native-cards.png"));
        canvas.QueueFree();await Tick();ResourceLoader.RemoveResourceFormatLoader(atlas);RunManager.Instance.CleanUp();return checks;
    }
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo098UiTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 开局查看："+text);}
        async Task Tick(int n=6){for(int i=0;i<n;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([player],seed:"BOOK098");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
        CombatTestState.SetInProgress(false);
        var game=new NGame{Size=new Vector2(1920,1080)};var scenes=new NSceneContainer();game.AddChild(scenes);
        AccessTools.Property(typeof(NGame),nameof(NGame.RootSceneContainer)).SetValue(game,scenes);
        var input=new NInputManager();var controller=new NControllerManager();input.AddChild(controller);game.AddChild(input);
        AccessTools.Property(typeof(NInputManager),nameof(NInputManager.ControllerManager)).SetValue(input,controller);
        AccessTools.Property(typeof(NGame),nameof(NGame.InputManager)).SetValue(game,input);
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        var canvas=new Control{Size=new Vector2(1920,1080)};root.AddChild(canvas);
        var hover=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};canvas.AddChild(hover);AccessTools.Property(typeof(NGame),nameof(NGame.HoverTipsContainer)).SetValue(game,hover);
        var deck=new MagicDeckState();deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);MagicDeckStore.Write(player,deck);
        var selection=GD.Load<PackedScene>(NChooseACardSelectionScreen.AssetPaths.Single()).Instantiate<NChooseACardSelectionScreen>();
        AccessTools.Field(typeof(NChooseACardSelectionScreen),"_cards").SetValue(selection,new CardModel[]{FactorCard.Create(player,"BuildUpBody",ManaSlot.Hand),FactorCard.Create(player,"MetalPetalSpell",ManaSlot.Hand)});
        TestMode.IsOn=false;canvas.AddChild(selection);await Tick();TestMode.IsOn=true;
        var completion=(TaskCompletionSource<IEnumerable<CardModel>>)AccessTools.Field(typeof(NChooseACardSelectionScreen),"_completionSource").GetValue(selection)!;
        Check(!completion.Task.IsCompleted&&selection.Visible,"原生选因子场景等待选择");
        MagicBookUi.Attach(canvas,player,()=>selection,()=>false);await Tick();canvas.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
        var button=(MagicFactorButton)canvas.FindChild("FrierenMagicBookButton",true,false);
        Check(button.IsEnabled&&MagicBookUi.AllowsViewOver(selection)&&!MagicPropCmd.AllowsUseOver(selection),"允许查看，但不放开选择期间的魔具使用");
        bool oldHover=MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips;MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=true;
        var icon=button.GetNode<TextureRect>("Control/Icon");var center=icon.GetGlobalRect().GetCenter();var pixel=center*(Vector2)root.GetTree().Root.Size/root.GetViewport().GetVisibleRect().Size;
        Input.ParseInputEvent(new InputEventMouseMotion{Position=pixel,GlobalPosition=pixel});await Tick(2);
        Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=true});
        Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=false});await Tick();
        var book=canvas.GetNode<Control>("FrierenMagicBook");
        Check(book.Visible&&!selection.Visible&&!completion.Task.IsCompleted,"实际鼠标点击打开魔法书，选牌任务只隐藏、不会取消或完成");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/todo-current/starting-choice-book.png"));
        book.Hide();await Tick();Check(selection.Visible&&!completion.Task.IsCompleted,"关闭查看后恢复原选因子界面并继续等待");
        Check(MagicDeckStore.Read(player).DraftSlot==1,"查看前后初始因子进度不变");
        completion.TrySetResult(Array.Empty<CardModel>());canvas.QueueFree();await Tick();MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=oldHover;
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,null);game.Free();RunManager.Instance.CleanUp();return count;
    }
}

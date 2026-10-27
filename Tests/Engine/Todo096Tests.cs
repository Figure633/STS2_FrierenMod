using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo096Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;GD.Print("PASS TODO096："+label);}
        async Task Tick(int n=6){for(int i=0;i<n;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO096");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        foreach(var model in new MonsterModel[]{ModelDb.Monster<HunterKiller>(),ModelDb.Monster<Axebot>()})combat.AddCreature(combat.CreateCreature(model.ToMutable(),CombatSide.Enemy,null));
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        foreach(var model in new MonsterModel[]{ModelDb.Monster<Byrdpip>(),ModelDb.Monster<Byrdonis>(),ModelDb.Monster<OwlMagistrate>(),ModelDb.Monster<HunterKiller>()})Check(BirdCapturingSpell.IsBird(model),"支持有翅膀目标："+model.GetType().Name);
        var card=combat.CreateCard<BirdCapturingSpell>(player);
        Check(card.IsValidTarget(combat.Enemies[0])&&!card.IsValidTarget(combat.Enemies[1]),"目标筛选不允许无翅膀敌人");
        Check(card.DynamicVars["Amount"].BaseValue==2 && card.Keywords.Contains(CardKeyword.Exhaust),"捕鸟普通版眩晕2回合并消耗");
        card.UpgradeInternal();Check(card.DynamicVars["Amount"].BaseValue==3&&!card.Keywords.Contains(CardKeyword.Innate),"升级眩晕3回合，不再固有");
        foreach(var lang in new[]{"zhs","eng"}){LocManager.Instance.SetLanguage(lang);Check(!card.GetDescriptionForPile(PileType.None).Contains("{Amount"),"捕鸟动态文本："+lang);}
        Check(ImageHelper.GetRoomIconPath(MapPointType.Ancient,MegaCrit.Sts2.Core.Rooms.RoomType.Event,ModelDb.Event<ReadingGrimoireEvent>().Id).EndsWith("event.png"),"开局前置事件在历史中使用事件图标");
        CombatTestState.SetInProgress(false);
        var game=new NGame{Size=new Vector2(1920,1080)};var scene=new NSceneContainer();game.AddChild(scene);
        AccessTools.Property(typeof(NGame),nameof(NGame.RootSceneContainer)).SetValue(game,scene);
        var input=new NInputManager();var controller=new NControllerManager();input.AddChild(controller);game.AddChild(input);
        AccessTools.Property(typeof(NInputManager),nameof(NInputManager.ControllerManager)).SetValue(input,controller);
        AccessTools.Property(typeof(NGame),nameof(NGame.InputManager)).SetValue(game,input);
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        var console=new MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole();AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole),"_instance").SetValue(null,console);
        var canvas=new Control{Size=new Vector2(1920,1080)};root.AddChild(canvas);
        var hover=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};canvas.AddChild(hover);
        AccessTools.Property(typeof(NGame),nameof(NGame.HoverTipsContainer)).SetValue(game,hover);
        var history=GD.Load<PackedScene>("res://scenes/screens/run_history_screen/run_history.tscn").Instantiate<NRunHistory>();
        canvas.AddChild(history);history.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var saved=new RunHistoryPlayer{Id=1,Character=ModelDb.Character<Frieren>().Id};MagicDeckHistory.Data.Set(saved,MagicDeckState.CreatePrototypeLoadout().ToJson());
        var portrait=GD.Load<PackedScene>("res://scenes/screens/run_history_screen/run_history_player_icon.tscn").Instantiate<NRunHistoryPlayerIcon>();
        canvas.AddChild(portrait);portrait.LoadRun(saved,new RunHistory{Players=[saved]});
        AccessTools.Method(AccessTools.Inner(typeof(MagicDeckHistory),"HistoryButton"),"Postfix").Invoke(null,[history,portrait]);await Tick();
        var button=(MagicFactorButton)history.FindChild("FrierenMagicBookButton",true,false);
        Check(button.GetParent().Name=="FrierenTopBarRow" && button.GetParent().GetParent()==history,"真实历史页入口独立于日期栏和滚动内容");
        var icon=button.GetNode<TextureRect>("Control/Icon");
        bool oldHover=MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips;MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=true;
        foreach(float scale in new[]{.8f,1f,1.2f}){
            history.Scale=Vector2.One*scale;history.Size=canvas.Size/scale;await Tick();
            Check(button.GetGlobalRect().HasPoint(icon.GetGlobalRect().GetCenter()),"图标中心处于点击框，缩放"+scale);
            var center=icon.GetGlobalRect().GetCenter();var pixel=center*(Vector2)root.GetTree().Root.Size/root.GetViewport().GetVisibleRect().Size;
            Input.ParseInputEvent(new InputEventMouseMotion{Position=pixel,GlobalPosition=pixel});await Tick(2);
            Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=true});
            Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=false});await Tick();
            var book=history.GetNode<Control>("FrierenMagicBook");Check(book.Visible,"真实历史页中心点击打开，缩放"+scale);book.Hide();
        }
        foreach(string name in new[]{"Aura_node_outline","Spiegel_node_outline"}){
            var texture=GD.Load<Texture2D>("res://FrierenMod/images/monsters/"+name+".png");using var mask=CustomBossMapMask.For(texture).GetImage();
            Check(texture.GetWidth()==512&&mask.GetPixel(256,256).A>.99f&&mask.GetPixel(0,0).A==0,"高清地图轮廓遮罩："+name);
        }
        canvas.QueueFree();await Tick();MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=oldHover;
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,null);game.Free();AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Debug.NDevConsole),"_instance").SetValue(null,null);console.Free();RunManager.Instance.CleanUp();return count;
    }
}




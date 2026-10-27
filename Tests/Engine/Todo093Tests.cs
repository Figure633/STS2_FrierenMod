using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo093Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS TODO093："+text);}
        async Task Tick(int n=6){for(int i=0;i<n;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO093");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
        LocalContext.NetId=1;SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        Check(ModelDb.Power<BanManaGainPower>().Type==PowerType.Debuff,"禁止生成魔力分类为减益");
        Check(ModelDb.Power<MimicPower>().Type==PowerType.None,"宝箱怪计数按一代保持中性");
        foreach(var p in new PowerModel[]{ModelDb.Power<FlyingMagicPower>(),ModelDb.Power<ManaReturnPower>(),ModelDb.Power<SwallowedPower>(),ModelDb.Power<WeakenedChantPower>()})
            Check(p.Type==PowerType.Debuff,"核对原有减益分类："+p.GetType().Name);
        CardPlay Play(CardModel c)=>CombatTestState.NewPlay(card: c, target: player.Creature, resultPile: PileType.Exhaust, resources: new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0}, playCount: 1, playIndex: 0, isAutoPlay: false);
        async Task Use(CardModel c)=>await (Task)AccessTools.Method(typeof(RustCleanMagic),"OnPlay").Invoke(c,[context,Play(c)])!;
        var rust=combat.CreateCard<RustCleanMagic>(player);
        Check(rust.Rarity==CardRarity.Uncommon && rust.Type==CardType.Skill && rust.TargetType==TargetType.Self && rust.EnergyCost.Canonical==0 && rust.Keywords.Contains(CardKeyword.Exhaust),"清洁铜像：0费罕见技能，自身目标且消耗");
        int energy=player.PlayerCombatState!.Energy;await Use(rust);Check(player.PlayerCombatState.Energy==energy+2,"无减益获得2能量");
        var upgraded=combat.CreateCard<RustCleanMagic>(player);upgraded.UpgradeInternal();energy=player.PlayerCombatState.Energy;await Use(upgraded);
        Check(upgraded.EnergyCost.Canonical==0 && player.PlayerCombatState.Energy==energy+3,"升级仍0费，无减益获得3能量");
        await PowerCmd.Apply<BanManaGainPower>(context,player.Creature,1,enemy,null);
        await PowerCmd.Apply<WeakPower>(context,player.Creature,2,enemy,null);
        await PowerCmd.Apply<StrengthPower>(context,player.Creature,3,player.Creature,null);
        await PowerCmd.Apply<StrengthPower>(context,enemy,5,enemy,null);energy=player.PlayerCombatState.Energy;await Use(rust);
        Check(!player.Creature.Powers.Any(p=>p.Type==PowerType.Debuff) && player.PlayerCombatState.Energy==energy,"有减益清除全部，不同时获得能量");
        Check(player.Creature.GetPower<StrengthPower>()!.Amount==3 && enemy.GetPower<StrengthPower>()!.Amount==5,"保留自身与敌人的增益");
        await PowerCmd.Apply<ArtifactPower>(context,player.Creature,1,player.Creature,null);
        await PowerCmd.Apply<BanManaGainPower>(context,player.Creature,1,enemy,null);
        Check(!player.Creature.HasPower<BanManaGainPower>() && !player.Creature.HasPower<ArtifactPower>(),"人工制品能阻挡无法生成魔力");
        foreach(var (card,name) in new (CardModel,string)[]{(ModelDb.Card<RedAppleSpell>().ToMutable(),"青苹果"),(ModelDb.Card<FlowerFieldSpell>().ToMutable(),"苍月草"),(ModelDb.Card<FireSpell>().ToMutable(),"小小火花"),(ModelDb.Card<DefensiveMagic>().ToMutable(),"额外的防御魔法")}) {
            Check(card.GetDescriptionForPile(PileType.None).Contains("[gold]"+name+"[/gold]"),"未升级衍生牌金色："+name);
            card.UpgradeInternal();Check(card.GetDescriptionForPile(PileType.None).Contains("[green]"+name+"+[/green]"),"升级衍生牌整名绿色："+name);
        }
        CombatTestState.SetInProgress(false);
        var game=new NGame{Size=new Vector2(1920,1080)};var scene=new NSceneContainer();game.AddChild(scene);
        AccessTools.Property(typeof(NGame),nameof(NGame.RootSceneContainer)).SetValue(game,scene);
        var input=new NInputManager();var controller=new NControllerManager();input.AddChild(controller);game.AddChild(input);
        AccessTools.Property(typeof(NInputManager),nameof(NInputManager.ControllerManager)).SetValue(input,controller);
        AccessTools.Property(typeof(NGame),nameof(NGame.InputManager)).SetValue(game,input);
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,game);
        var canvas=new Control{Size=new Vector2(1920,1080)};root.AddChild(canvas);
        var hover=new Control{MouseFilter=Control.MouseFilterEnum.Ignore};canvas.AddChild(hover);
        AccessTools.Property(typeof(NGame),nameof(NGame.HoverTipsContainer)).SetValue(game,hover);
        // 与作者截图相同，完整左半图；源图按覆盖模式裁剪而非留下底部空隙。
        var layout=GD.Load<PackedScene>(NEventLayout.defaultScenePath).Instantiate<NEventLayout>();
        canvas.AddChild(layout);layout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AccessTools.Field(typeof(NEventLayout),"_event").SetValue(layout,ModelDb.Event<ReadingGrimoireEvent>());
        layout.SetPortrait(GD.Load<Texture2D>("res://FrierenMod/images/events/ReadingGrimoire.png"));await Tick();
        var portrait=layout.GetNode<TextureRect>("%Portrait");
        Check(portrait.Visible && portrait.StretchMode==TextureRect.StretchModeEnum.KeepAspectCovered,"左半肖像保留并以覆盖模式铺满");
        Check(portrait.GetGlobalRect().Size.IsEqualApprox(new Vector2(960,1080)) && portrait.GetGlobalRect().Position.IsEqualApprox(Vector2.Zero),"肖像完整覆盖左半幅边界");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/event-left-review.png"));
        layout.QueueFree();await Tick();
        foreach(string name in new[]{"Aura_node_outline","Spiegel_node_outline"}) {
            var mask=CustomBossMapMask.For(GD.Load<Texture2D>("res://FrierenMod/images/monsters/"+name+".png")).GetImage();
            Check(mask.GetPixel(mask.GetWidth()/2,mask.GetHeight()/2).A>.99f && mask.GetPixel(0,0).A==0,"地图背景遮罩填实中心且保留外部透明："+name);mask.Dispose();
        }
        // 使用真实奖励场景，验证查看魔法书不会被整页奖励覆盖层禁用。
        run.PushRoom(new CombatRoom(combat));
        var rewards=GD.Load<PackedScene>("res://scenes/screens/rewards_screen.tscn").Instantiate<NRewardsScreen>();
        var rewardSet=new RewardsSet(player);rewardSet.Rewards.Add(new GoldReward(50,player));
        AccessTools.Field(typeof(NRewardsScreen),"_rewardsSet").SetValue(rewards,rewardSet);
        AccessTools.Field(typeof(NRewardsScreen),"_runState").SetValue(rewards,run);
        // 原生Reward.CreateIcon在TestMode下返回null；显示真实奖励节点时仅启用UI工厂。
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn=false;canvas.AddChild(rewards);await Tick(3);
        MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn=true;
        MagicDeckStore.Data.Set(player,MagicDeckState.CreatePrototypeLoadout().ToJson());
        MagicBookUi.Attach(canvas,player,()=>rewards,()=>false);await Tick();
        canvas.GetNode<Godot.Timer>("FrierenManaRefresh").EmitSignal(Godot.Timer.SignalName.Timeout);
        var toggle=(MagicFactorButton)canvas.FindChild("FrierenMagicBookButton",true,false);
        Check(toggle.IsEnabled,"搜刮战利品时魔法书入口保持可用");
        var icon=toggle.GetNode<TextureRect>("Control/Icon");
        Check(toggle.GetGlobalRect().Encloses(icon.GetGlobalRect()),"历史/全局入口可见图标完全处于真实点击框内");
        // 将真实鼠标输入送到图标中心；不直接调用按钮回调。
        Vector2 center=icon.GetGlobalRect().GetCenter();
        Vector2 pixel=center*(Vector2)root.GetTree().Root.Size/root.GetViewport().GetVisibleRect().Size;
        bool oldHover=MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips;
        MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=true;
        Input.ParseInputEvent(new InputEventMouseMotion{Position=pixel,GlobalPosition=pixel});await Tick(2);
        Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=true});
        Input.ParseInputEvent(new InputEventMouseButton{Position=pixel,GlobalPosition=pixel,ButtonIndex=MouseButton.Left,Pressed=false});await Tick();
        var book=canvas.GetNode<Control>("FrierenMagicBook");Check(book.Visible,"点击图标中心即可打开魔法书，无需点右下角");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/rewards-book-review.png"));
        canvas.QueueFree();await Tick();run.PopCurrentRoom();
        MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet.shouldBlockHoverTips=oldHover;
        AccessTools.Property(typeof(NGame),nameof(NGame.Instance)).SetValue(null,null);game.Free();
        RunManager.Instance.CleanUp();LocManager.Instance.SetLanguage("eng");return count;
    }
}


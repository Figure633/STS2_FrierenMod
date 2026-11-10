using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class AuthorTodoTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 作者TODO："+text);}
        async Task Tick(){for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        async Task Snapshot(string file){await Tick();if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/"+file+".png"));}
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([p],seed:"AUTHOR-TODOS-01213");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;CombatTestState.SetInProgress(false);
        p.Creature.SetMaxHpInternal(100);p.Creature.SetCurrentHpInternal(65);p.Gold=75;
        Check(ModelDb.Event<FoodEvent>().IsAllowed(run),"美食事件在65%血量边界允许出现");
        p.Creature.SetCurrentHpInternal(66);Check(!ModelDb.Event<FoodEvent>().IsAllowed(run),"高于65%血量不会出现美食事件");
        p.Creature.SetCurrentHpInternal(1);p.Gold=74;Check(!ModelDb.Event<FoodEvent>().IsAllowed(run),"保留75金币门槛");p.Gold=75;
        foreach(int act in new[]{0,1,2}){
            AccessTools.Property(typeof(RunState),nameof(RunState.CurrentActIndex)).SetValue(run,act);
            Check(ModelDb.Event<AnimalWell>().IsAllowed(run)==(act<2),"动物井仅一二幕："+(act+1));
        }
        AccessTools.Property(typeof(RunState),nameof(RunState.CurrentActIndex)).SetValue(run,0);
        var cargo=(ValuableCargo)run.CreateCard<ValuableCargo>(p);cargo.Fare=120;
        Check(cargo.Type==CardType.Quest&&cargo.Rarity==CardRarity.Quest&&cargo.CustomFrame==null&&cargo.Frame.ResourcePath==ModelDb.Card<SpoilsMap>().Frame.ResourcePath,"贵重商品使用原版藏宝图同款任务类型、稀有度和边框");
        Check(!cargo.CanBeGeneratedInCombat&&!cargo.CanBeGeneratedByModifiers&&cargo.MaxUpgradeLevel==0,"任务牌不进入随机生成与升级池");
        var host=new Control{Size=new(1920,1080)};root.AddChild(host);host.AddChild(new ColorRect{Size=host.Size,Color=new("17202c")});
        var nodes=new List<NCard>();
        foreach(var (model,x) in new[]{((CardModel)cargo,700f),(run.CreateCard<SpoilsMap>(p),1200f)}){
            var node=NodePool.Get<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,model);
            node.Position=new(x,550);node.Scale=Vector2.One;host.AddChild(node);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);
        }
        await Snapshot("author-todo-quest-cargo");foreach(var node in nodes){host.RemoveChild(node);node.QueueFreeSafely();}host.QueueFree();await Tick();
        var selector=new TestCardSelector();using var choices=CombatTestState.UseSelector(selector);
        MagicDeckStore.Write(p,MagicDeckState.CreatePrototypeLoadout());
        async Task<SealingOreMine> Story(){var story=(SealingOreMine)ModelDb.Event<SealingOreMine>().ToMutable();await CombatTestState.BeginEvent(story,p);return story;}
        var layouts=new Dictionary<SealingOreMine,(Control Scene,NEventLayout Layout)>();
        async Task Draw(SealingOreMine story,string file){
            foreach(var item in layouts.Values)item.Scene.Hide();
            if(!layouts.TryGetValue(story,out var existing)){
                var container=new Control{Size=new(1920,1080)};root.AddChild(container);
                var created=story.CreateScene().Instantiate<NEventLayout>();container.AddChild(created);story.SetNode(created);created.SetEvent(story);
                layouts.Add(story,existing=(container,created));
            }
            var (scene,layout)=existing;scene.Show();layout.ClearOptions();layout.SetPortrait(story.CreateInitialPortrait());
            layout.SetTitle(story.Title.GetFormattedText());layout.SetDescription(story.Description!.GetFormattedText());layout.AddOptions(story.CurrentOptions);layout.OnSetupComplete();
            await Tick();foreach(var tween in root.GetTree().GetProcessedTweens())tween.CustomStep(10);await Tick();
            var portrait=layout.GetNode<TextureRect>("%Portrait");
            Check(portrait.Visible==(story.ArtIndex==2),"矿山横图整幅展示、方图左侧展示："+story.ArtIndex);
            Check(layout.GetNode<TextureRect>("FrierenEventBackdrop").Texture.ResourcePath==story.CustomInitialPortraitPath,"当前分支插图与背景同步："+story.ArtIndex);
            Check(layout.OptionButtons.All(b=>scene.GetGlobalRect().Grow(1).Encloses(b.GetGlobalRect())),"矿山原生选项在视口内："+story.ArtIndex);
            await Snapshot(file);
        }
        var story=await Story();Check(story.GetAssetPaths(run).Count(f=>f.Contains("SealingOreMine")&&f.EndsWith(".png"))>=3,"三张Word事件图均预加载");await Draw(story,"author-todo-mine-main");
        await story.CurrentOptions[1].Chosen();Check(story.ArtIndex==1&&story.CustomInitialPortraitPath.EndsWith("SealingOreMineGreed.png"),"贪分支换成指定图");await Draw(story,"author-todo-mine-greed");
        story=await Story();selector.PrepareToSelect(new[]{0});await story.CurrentOptions[0].Chosen();Check(story.ArtIndex==2&&story.CustomInitialPortraitPath.EndsWith("SealingOreMinePlay.png"),"玩分支换成指定图");await Draw(story,"author-todo-mine-play");
        foreach(var item in layouts.Values)item.Scene.QueueFree();await Tick();
        var combat=new CombatState(runState:run);combat.AddPlayer(p);combat.AddCreature(combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null));CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();p.Creature.SetCurrentHpInternal(10);await PowerCmd.Apply<ShapeOfUnnPower>(context,p.Creature,3,p.Creature,null);
        var power=p.Creature.GetPower<ShapeOfUnnPower>()!;
        Check(ModelDb.Power<ShapeOfUnnPower>().DumbHoverTip.Description.Contains("层数")&&!ModelDb.Power<ShapeOfUnnPower>().DumbHoverTip.Description.Contains("0"),"通用能力悬停按层数说明，不显示0点治疗");
        Check(power.HoverTips.OfType<HoverTip>().First().Description.Contains("3"),"实战悬停显示实际3层治疗");
        await power.AfterChant(context,CombatTestState.NewPlay(combat.CreateCard<RapidChant>(p),null,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0}));
        Check(p.Creature.CurrentHp==13&&!p.Creature.HasPower<ShapeOfUnnPower>(),"咏唱后回复3生命并移除能力");
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();return count;
    }
}

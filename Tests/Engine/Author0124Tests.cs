using System.Text.RegularExpressions;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Author0124Tests
{
    public static async Task<int> Run(Node root)
    {
        int checks=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 0.12.4："+text);}
        SaveManager.Instance.InitPrefsDataForTest();
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,other],seed:"AUTHOR0124");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(p);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);p.PlayerCombatState!.Energy=10;
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CombatTestState.UseSelector(selector);
        T Card<T>(bool up=false)where T:CardModel{var c=combat.CreateCard<T>(p);if(up)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel c){await CardPileCmd.Add(c,PileType.Hand);if(c.EnergyCost.CostsX)c.EnergyCost.CapturedXValue=2;await c.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=2,EnergyValue=2,StarsSpent=0,StarValue=0},true);}
        foreach(var (cost,kind,amount,layer) in new[]{(0,"Damage",6,1),(1,"Damage",12,1),(2,"Damage",18,1),(3,"DamageFive",4,1),(3,"Block",22,1),(1,"ManaDiscard",5,0),(3,"ManaHand",6,0),(2,"ReduceStrength",3,2),(3,"ReduceStrength",4,2)})
            Check(LegendaryCraft.Options(cost,layer).Contains(new(kind,amount)),"自定义费用对应数值 "+(cost,kind,amount));
        Check(!LegendaryCraft.Options(3,1).Any(e=>e.Kind=="Damage")&&!LegendaryCraft.Options(0,0).Any(e=>e.Kind=="ManaHand"),"正义无3费、太阳无0费选项");
        foreach(var kind in new[]{"DamageQuad","DamageFive","DamageSeven"}){
            var c=Card<CustomLegendarySpell>();c.Configure(3,[new(kind,4)]);int hp=enemy.CurrentHp;await Use(c);
            Check(hp-enemy.CurrentHp==(kind=="DamageQuad"?16:20),"恶魔实际段数及旧存档兼容 "+kind);
        }
        foreach(var kind in new[]{"DoubleStats","RemoveDebuffs","ReduceStrength"}){
            var c=Card<CustomLegendarySpell>();c.Configure(3,[new(kind,1)]);
            Check(c.Keywords.Contains(CardKeyword.Exhaust),"女皇／权杖／教皇带消耗 "+kind);
        }
        foreach(bool up in new[]{false,true}){
            var c=Card<AirSlash>(up);int hp=enemy.CurrentHp;await Use(c);
            Check(hp-enemy.CurrentHp==(up?9:6),"空气斩3伤×2／3 "+up);
        }
        var spark=Card<KindlingSpark>(true);int previous=enemy.CurrentHp;await Use(spark);
        Check(previous-enemy.CurrentHp==18&&p.PlayerCombatState.Hand.Cards.OfType<LittleFire>().Count()==2,"星星之火升级18基础群伤，生成X张火花");
        Check(Card<Flow>().Rarity==CardRarity.Basic&&Card<DefensiveMagic>().Rarity==CardRarity.Basic&&
            !FrierenCardAvailability.IsRandomCandidate(Card<Flow>())&&!FrierenCardAvailability.IsRandomCandidate(Card<DefensiveMagic>()),"流转、防御魔法归入原生基础／其他且不掉落");
        Check(!ModelDb.Card<ViolentExorcism>().ShouldShowInCardLibrary,"弃用的物理超度不在百科显示");
        foreach(var pile in p.PlayerCombatState.AllPiles)foreach(var c in pile.Cards.ToArray())c.RemoveFromCurrentPile();
        var deck=MagicDeckState.CreatePrototypeLoadout();MagicDeckStore.Write(p,deck);MagicDeckStore.Write(other,MagicDeckState.CreatePrototypeLoadout());
        var precise=Card<PreciseChant>();Check(precise.CanPlay()&&precise.Rarity==CardRarity.Rare,"精准咏唱0魔力可用，1费稀有卡");
        await ManaCmd.Generate(p,PileType.Draw,7);
        // 两次选择独立：先在弃牌堆造魔力，再咏唱抽牌堆，不能自动复用第一次槽位。
        var expected=ChantCmd.Choices(p,null).First(c=>c.Option.Slot==ManaSlot.Draw).Option;
        selector.PrepareToSelect(new[]{2});selector.PrepareToSelect(new[]{0});await Use(precise);
        Check(ManaCmd.InPile(p,PileType.Discard).Count()==4&&ManaCmd.InPile(p,PileType.Draw).Count()==7-expected.Cost,"精准咏唱两次独立三选一，第二次按最大整数咏唱");
        Check(ManaCmd.Total(other)==0,"生成和咏唱仅影响打牌玩家");
        Check(precise.Pile?.Type==PileType.Exhaust&&!Card<PreciseChant>(true).Keywords.Contains(CardKeyword.Exhaust),"精准普通消耗，升级移除消耗");
        foreach(var lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);
            foreach(bool up in new[]{false,true}){
                var c=Card<HighDensityMana>(up);string text=c.GetDescriptionForPile(PileType.None);
                Check(Regex.Matches(text,"\\[img").Count==(up?2:1)&&!text.Contains("LOC_ERR"),lang+"高密度魔力实际格式化能量图标数量 "+up+" "+text);
            }
            var serie=Card<SerieGrimoire>();serie.UpgradeInternal();serie.UpdateDynamicVarPreview(CardPreviewMode.Upgrade,null,serie.DynamicVars);
            Check(serie.DynamicVars["Options"].IntValue==3&&serie.GetDescriptionForUpgradePreview().Contains("[green]3[/green]"),lang+"塞利耶魔导书升级选择数绿色 "+serie.GetDescriptionForUpgradePreview());
            var submission=await PowerCmd.Apply<AuraSubmissionPower>(context,p.Creature,2,p.Creature,null);
            Check(Regex.Matches(submission!.Description.GetFormattedText(),"\\[img").Count==2,lang+"天平减能量描述显示2个能量图标");
            await PowerCmd.Remove(submission);
            Check(Regex.Matches(ModelDb.Power<AuraScalesPower>().Description.GetFormattedText(),"\\[img").Count==1,lang+"天平无所属者预览同样正确显示能量图标");
            foreach(var c in ChantChoice.ManaGenerationChoices(p,4))Check(!c.Title.Contains('[')&&!c.GetDescriptionForPile(PileType.None).Contains("LOC_ERR"),lang+"牌堆选择标题纯文本，正文有生成提示 "+c.Slot);
        }
        LocManager.Instance.SetLanguage("zhs");
        var canvas=new Control{Size=new(1920,1080)};root.AddChild(canvas);
        canvas.AddChild(new ColorRect{Size=canvas.Size,Color=new("17202c")});
        using var atlas=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();ResourceLoader.AddResourceFormatLoader(atlas,true);
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        var seriePreview=Card<SerieGrimoire>();seriePreview.UpgradeInternal();
        CardModel[][] pages=[[Card<PreciseChant>(),Card<PreciseChant>(true),Card<HighDensityMana>(true),seriePreview],
            [Card<KindlingSpark>(true),..ChantChoice.ManaGenerationChoices(p,4)]];
        for(int page=0;page<pages.Length;page++){
            var nodes=new List<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();
            for(int i=0;i<pages[page].Length;i++){
                var node=scene.Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();AccessTools.Property(node.GetType(),"Model").SetValue(node,pages[page][i]);
                canvas.AddChild(node);node.Position=new(270+455*i,510);node.UpdateVisuals(PileType.None,pages[page][i].IsUpgraded?CardPreviewMode.Upgrade:CardPreviewMode.Normal);nodes.Add(node);
            }
            for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var node in nodes){var label=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(!label.Text.Contains('{')&&label.GetContentHeight()<=label.Size.Y+2,"原生卡面无占位符和正文溢出 "+node.Model!.Id);}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/todo-20261108/cards-{page}.png"));
            foreach(var node in nodes){canvas.RemoveChild(node);node.QueueFree();}
        }
        canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);ResourceLoader.RemoveResourceFormatLoader(atlas);
        CombatTestState.SetInProgress(false);run.CurrentActIndex=1;
        var carried=new Commission{Id=CommissionState.QuestId(0,1),Act=0,Template="HUNT",Stars=3,Goal=CommissionGoal.EmpoweredElite,Completed=true,Status=CommissionStatus.Accepted};
        CommissionCmd.Write(p,new CommissionState{CurrentAct=1,GeneratedActs=[0,1],Quests=[carried]});
        var ui=new Control{Size=new(1920,1080)};root.AddChild(ui);var board=new CommissionScreen(ui,p);board.Open();
        var previousButton=board.View.GetNode<Control>("CommissionCanvas/PreviousAct");
        Check(previousButton.GetNode<Label>("Label").Text.Contains('!'),"旧幕已完成未领取的委托标记上一幕入口");
        var firstColor=previousButton.Modulate;await root.ToSignal(root.GetTree().CreateTimer(.3),Godot.Timer.SignalName.Timeout);
        Check(previousButton.Modulate!=firstColor,"状态未变化时上一幕按钮仍持续闪烁");
        var state=CommissionCmd.Read(p);state.Quests[0].Status=CommissionStatus.Claimed;CommissionCmd.Write(p,state);board.Refresh(true);
        Check(!previousButton.GetNode<Label>("Label").Text.Contains('!')&&previousButton.Modulate==Colors.White,"领奖后上一幕闪烁和标记消失");
        board.View.QueueFree();ui.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        RunManager.Instance.CleanUp();return checks;
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
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
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Author01215Tests
{
    private sealed class Selector:TestCardSelector,MegaCrit.Sts2.Core.TestSupport.ICardSelector {
        public List<CardModel[]> Seen {get;}=[];
        async Task<IEnumerable<CardModel>> MegaCrit.Sts2.Core.TestSupport.ICardSelector.GetSelectedCards(IEnumerable<CardModel> options,int min,int max){var cards=options.ToArray();Seen.Add(cards);return await base.GetSelectedCards(cards,min,max);}
    }
    private static readonly List<int> DrawEnergy=[];
    private static bool TrackDraw;
    private static void Drawing(CardModel card){if(TrackDraw)DrawEnergy.Add(card.Owner.PlayerCombatState!.Energy);}
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 0.12.15："+text);}
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([p],seed:"AUTHOR-01215");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;CombatTestState.SetInProgress(false);
        var state=MagicDeckState.CreatePrototypeLoadout();state=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,state.Items.ToArray(),true,3,StartingPropsGranted:true)));
        int breadId=state.AddProp("MagicBread").InstanceId;MagicDeckStore.Write(p,state);
        Check(!MagicPropCmd.CanUse(p,breadId)&&!await MagicPropCmd.UseSelected(p,breadId,[])&&MagicDeckStore.Read(p).Props.Any(x=>x.InstanceId==breadId),"魔法面包战斗外禁用且不消耗");
        Check(MagicPropRules.CommissionOnly.Count(x=>x.Id!="RoyalGrimoire")==3&&!MagicPropRules.All.Any(x=>x.Id=="MagicBread"),"三种三星专属魔具补齐，普通魔具池不包含面包");
        var saved=Player.FromSerializable(p.ToSerializable());Check(MagicDeckStore.Read(saved).Props.Any(x=>x.PropId=="MagicBread"),"新魔具随原生玩家存档读取");
        var combat=new CombatState(runState:run);combat.AddPlayer(p);var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        foreach(var pile in p.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);
        p.PlayerCombatState.Energy=1;await CardPileCmd.Add(combat.CreateCard<StrikeFrieren>(p),PileType.Draw);await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Draw);await CardPileCmd.Add(combat.CreateCard<RapidChant>(p),PileType.Draw);
        var harmony=new Harmony("frieren.tests.author01215.draw");harmony.Patch(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Hooks.Hook),"AfterCardDrawn"),prefix:new HarmonyMethod(typeof(Author01215Tests),nameof(Drawing)));
        TrackDraw=true;try{Check(await MagicPropCmd.UseSelected(p,breadId,[]),"魔法面包原生使用成功");}finally{TrackDraw=false;harmony.UnpatchAll(harmony.Id);}
        Check(p.PlayerCombatState.Hand.Cards.Count==2&&p.PlayerCombatState.DrawPile.Cards.Count==1&&p.PlayerCombatState.Energy==3,"面包实际抽2牌并回复2能量");
        Check(DrawEnergy.SequenceEqual(new[]{1,1}),"先抽牌，随后回复能量");
        Check(!MagicDeckStore.Read(p).Props.Any(x=>x.InstanceId==breadId)&&!await MagicPropCmd.UseSelected(p,breadId,[]),"使用后移除，同一实例不能重复使用");
        var selector=new Selector();using var selection=CombatTestState.UseSelector(selector);
        foreach(var pile in p.PlayerCombatState.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);
        await ManaCmd.Generate(p,PileType.Draw,7);var expected=ChantCmd.Options(p,null).First(o=>o.Slot==ManaSlot.Draw);
        var precise=combat.CreateCard<PreciseChant>(p);await CardPileCmd.Add(precise,PileType.Hand);selector.PrepareToSelect(new[]{2});selector.PrepareToSelect(new[]{0});
        await precise.OnPlayWrapper(new ThrowingPlayerChoiceContext(),enemy,false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);
        Check(selector.Seen.Count==2&&selector.Seen[0].Length==3&&selector.Seen[1].Length==3,"精准咏唱仍为两次独立三选一");
        Check(ManaCmd.InPile(p,PileType.Discard).Count()==4&&ManaCmd.InPile(p,PileType.Draw).Count()==7-expected.Cost,"换图不影响先生成再咏唱的目标与支付");
        foreach(var group in selector.Seen)for(int i=0;i<3;i++)Check(group[i].PortraitPath.EndsWith(ChantChoice.ArtFor((ManaSlot)i)+".png"),"生成／咏唱选项使用对应牌堆原图："+group[i].GetType().Name);
        foreach(var slot in Enum.GetValues<ManaSlot>()){
            var factor=FactorCard.Create(p,"BuildUpBody",slot,new ChantOption(slot,"BuildUpBody",2,2,2));Check(factor.PortraitPath.EndsWith(ChantChoice.ArtFor(slot)+".png"),"所有咏唱牌共用牌堆图映射："+slot);
            Check(FactorCard.Create(p,"BuildUpBody",slot).PortraitPath.EndsWith("BuildUpBody.png"),"库存／初始因子保留自身卡图："+slot);
        }
        var texts=new Dictionary<string,string>();
        foreach(string language in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(language);var bread=MagicPropCard.Create(p,"MagicBread");string text=bread.GetDescriptionForPile(PileType.None);texts[language]=text;
            Check(Regex.Matches(text,@"\[img").Count==2&&!text.Contains('{')&&!text.Contains("energyIcons"),language+"面包卡面显示两个能量图标");
            Check(!bread.CanBeGeneratedInCombat&&!bread.CanBeGeneratedByModifiers&&bread.MaxUpgradeLevel==0,language+"魔具模型仅展示，不进入主牌组生成池");
            var play=new LocString("events","FRIERENMOD-SEALING_ORE_MINE.pages.PLAY.description").GetFormattedText();
            var greed=new LocString("events","FRIERENMOD-SEALING_ORE_MINE.pages.GREED.description").GetFormattedText();
            Check(!play.Contains("#y")&&!greed.Contains("#r")&&play.Contains("[gold]")&&greed.Contains("[red]"),language+"矿山使用二代颜色标签");
            if(language=="zhs"){
                Check(play.EndsWith("[gold]魔力[/gold]满溢而出。余兴节目结束，你继续踏上了旅程。"),"矿山结尾使用作者补全句子");
                Check(new LocString("relics","FRIERENMOD-HOLY_EMBLEM.flavor").GetFormattedText().StartsWith("相当久远前的魔法使资格证明")&&new LocString("relics","FRIERENMOD-UPGRADED_HOLY_EMBLEM.flavor").GetFormattedText().Contains("所有魔法团体的认可"),"两种圣徽采用作者改稿");
            }
            var cards=new List<CardModel>();cards.AddRange(ChantChoice.ManaGenerationChoices(p,4));cards.AddRange(Enum.GetValues<ManaSlot>().Select(slot=>FactorCard.Create(p,"BuildUpBody",slot,new ChantOption(slot,"BuildUpBody",1,1,1))));cards.Add(bread);
            var host=new Control();root.AddChild(host);host.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});var nodes=new List<NCard>();
            for(int i=0;i<cards.Count;i++){var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,cards[i]);host.AddChild(node);node.Position=new(240+i%4*480,270+i/4*520);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);}
            for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            foreach(var node in nodes){var body=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(body.GetContentHeight()<=body.Size.Y+2&&body.GetThemeFontSize("normal_font_size")>=16,language+"原生选项／面包卡面正文完整："+node.Model!.Id);}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/author01215-{language}.png"));
            host.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/magic-bread-text.json"),JsonSerializer.Serialize(texts));
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();return checks;
    }
}

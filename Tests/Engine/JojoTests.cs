using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Relics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class JojoTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0; void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS JOJO："+text);}
        SaveManager.Instance.InitPrefsDataForTest(); LocalContext.NetId=1;
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var ally=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,ally],seed:"JOJO-ADVENTURE");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        CombatTestState.SetInProgress(false);
        var canonical=ModelDb.Event<JojoAdventure>();
        Check(ModelDb.AllSharedEvents.Contains(canonical)&&canonical.IsAllowed(run),"正式随机事件登记，不再只有插画预览");
        Check(!canonical.IsAllowed(RunState.CreateForTest([Player.CreateForNewRun<Ironclad>(UnlockState.all,3)],seed:"JOJO-IRON")),"原版角色不抽到芙莉莲专属事件");
        async Task<JojoAdventure> Enter(){var e=(JojoAdventure)canonical.ToMutable();await CombatTestState.BeginEvent(e,p);return e;}
        p.Creature.SetCurrentHpInternal(p.Creature.MaxHp-12);var story=await Enter();
        Check(story.CurrentOptions.Count==3&&story.CurrentOptions[0].HoverTips.Any()&&story.CurrentOptions[1].HoverTips.Any(),"三个作者分支提供附魔和遗物悬停");
        var tip=story.CurrentOptions[2].HoverTips.OfType<CardHoverTip>().Single();
        Check(tip.Card is Spinner&&!tip.Card.IsUpgraded,"旋转工艺选项预览普通版本");
        var selector=new TestCardSelector();using(CombatTestState.UseSelector(selector)){selector.PrepareToSelect([0]);await story.CurrentOptions[0].Chosen();}
        var enchanted=p.Deck.Cards.Single(c=>c.Enchantment is HamonOverdrive);
        Check(story.IsFinished&&p.Creature.CurrentHp==p.Creature.MaxHp-4,"假面之门治疗8并完成事件");
        Check(CardModel.FromSerializable(enchanted.ToSerializable()).Enchantment is HamonOverdrive,"波纹疾走永久附魔经原生卡牌存档恢复");
        story=await Enter();var diamondOption=story.CurrentOptions[1];await diamondOption.Chosen();await diamondOption.Chosen();
        Check(p.Relics.OfType<GhostInBloom>().Count()==1&&ally.GetRelic<GhostInBloom>()==null,"钻石之门仅给持有者，重复点击不重复领取");
        Check((await Enter()).CurrentOptions[1].IsLocked,"已有花上亡灵时该分支锁定");
        story=await Enter();int spinnerCount=p.Deck.Cards.OfType<Spinner>().Count();await story.CurrentOptions[2].Chosen();
        Check(story.IsFinished&&p.Deck.Cards.OfType<Spinner>().Count()==spinnerCount+1&&!p.Deck.Cards.OfType<Spinner>().Last().IsUpgraded,"漆黑之门实际获得未升级的旋转工艺");
        var serial=p.ToSerializable();var writer=new PacketWriter();serial.Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);var copy=new SerializablePlayer();copy.Deserialize(reader);var restored=Player.FromSerializable(copy);
        Check(restored.GetRelic<GhostInBloom>()!=null&&restored.Deck.Cards.Any(c=>c.Enchantment is HamonOverdrive)&&reader.BitPosition==writer.BitPosition,"网络玩家数据往返保留新遗物、附魔和卡牌");
        foreach(string lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);story=await Enter();
            Check(!story.Title.GetFormattedText().Contains("LOC_ERR")&&story.CurrentOptions.All(o=>!o.Description.GetFormattedText().Contains("LOC_ERR"))&&
                !ModelDb.Enchantment<HamonOverdrive>().DynamicDescription.GetFormattedText().Contains("LOC_ERR"),lang+"事件、遗物与附魔文本完整");
        }
        LocManager.Instance.SetLanguage("zhs");
        var combat=new CombatState(runState:run);combat.AddPlayer(p);combat.AddPlayer(ally);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);var context=new ThrowingPlayerChoiceContext();
        void Clear(){foreach(var type in new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Play,PileType.Exhaust})foreach(var c in CardPile.Get(type,p)!.Cards.ToArray())c.RemoveFromCurrentPile();p.PlayerCombatState!.Energy=6;}
        async Task<CardModel[]> Hand(int size){Clear();var cards=new List<CardModel>();for(int i=0;i<size;i++){var c=combat.CreateCard<DefendFrieren>(p);CardCmd.Enchant<HamonOverdrive>(c,1);await CardPileCmd.Add(c,PileType.Hand);cards.Add(c);}return cards.ToArray();}
        for(int size=1;size<=10;size++){
            var cards=await Hand(size);var golden=cards.Select((c,i)=>(c,i)).Where(x=>x.c.ShouldGlowGold).Select(x=>x.i).ToArray();
            Check(golden.SequenceEqual(size%2==1?new[]{size/2}:new[]{size/2-1,size/2}),"当前手牌中央自动金光提示，数量="+size);
        }
        var hand=await Hand(3);var selectedCard=hand[1];
        await CardPileCmd.Add(hand[0],PileType.Discard);
        Check(hand[1].ShouldGlowGold&&hand[2].ShouldGlowGold,"弃牌后由奇数中央更新为偶数中间两张");
        await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Hand);
        Check(!selectedCard.ShouldGlowGold&&hand[2].ShouldGlowGold,"新增手牌后移出中央立即失去发光");
        ((HamonOverdrive)hand[2].Enchantment!).Status=EnchantmentStatus.Disabled;Check(!hand[2].ShouldGlowGold,"附魔失效时不发光");
        async Task Use(CardModel c,bool auto=false){var spent=auto?(0,0):await c.SpendResources();await c.OnPlayWrapper(context,null,auto,new ResourceInfo{EnergySpent=spent.Item1,EnergyValue=c.EnergyCost.GetWithModifiers(CostModifiers.All),StarsSpent=spent.Item2,StarValue=0},true);}
        hand=await Hand(3);int energy=p.PlayerCombatState!.Energy;await Use(hand[1]);
        Check(p.PlayerCombatState.Energy==energy&&hand[1].Pile?.Type==PileType.Discard,"居中手动打出在离开手牌后仍返还真实支付的能量");
        hand=await Hand(3);energy=p.PlayerCombatState.Energy;await Use(hand[0]);Check(p.PlayerCombatState.Energy==energy-1,"侧边牌不返能量");
        hand=await Hand(4);energy=p.PlayerCombatState.Energy;await Use(hand[2]);Check(p.PlayerCombatState.Energy==energy,"偶数张右侧中央牌实际返能量");
        hand=await Hand(1);hand[0].BaseReplayCount=2;energy=p.PlayerCombatState.Energy;await Use(hand[0]);Check(p.PlayerCombatState.Energy==energy,"重放两次仍只返还一次支付");
        hand=await Hand(1);hand[0].EnergyCost.SetThisCombat(0);energy=p.PlayerCombatState.Energy;await Use(hand[0]);Check(p.PlayerCombatState.Energy==energy,"0费不会凭空获得能量");
        hand=await Hand(1);energy=p.PlayerCombatState.Energy;await Use(hand[0],true);Check(p.PlayerCombatState.Energy==energy,"自动打出不返还名义费用");
        Clear();var drawCard=combat.CreateCard<PommelStrike>(p);CardCmd.Enchant<HamonOverdrive>(drawCard,1);
        await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Hand);await CardPileCmd.Add(drawCard,PileType.Hand);await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Hand);await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Draw);
        var paid=await drawCard.SpendResources();energy=p.PlayerCombatState.Energy;await drawCard.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=paid.Item1,EnergyValue=1,StarsSpent=0,StarValue=0},true);
        Check(p.PlayerCombatState.Energy==energy+1,"本牌效果先抽牌改变手牌时仍按打出前位置判断");
        Clear();var relic=p.GetRelic<GhostInBloom>()!;await CardPileCmd.Add(combat.CreateCard<DefendFrieren>(p),PileType.Discard);int permanent=p.Deck.Cards.Count;
        await CardPileCmd.Shuffle(context,p);Check(CardPile.Get(PileType.Draw,p)!.Cards.OfType<BlueMoonWeed>().Count()==1&&p.Deck.Cards.Count==permanent,"真实洗牌钩子生成一张苍月草，主牌组不增加");
        await CardPileCmd.Shuffle(context,ally);Check(CardPile.Get(PileType.Draw,p)!.Cards.OfType<BlueMoonWeed>().Count()==1,"队友洗牌不触发持有者遗物");
        foreach(var c in CardPile.Get(PileType.Draw,p)!.Cards.ToArray())await CardPileCmd.Add(c,PileType.Discard);
        await CardPileCmd.Shuffle(context,p);Check(CardPile.Get(PileType.Draw,p)!.Cards.OfType<BlueMoonWeed>().Count()==2,"再次洗牌再次生成，沿用原生随机洗牌流程");
        Check(relic.Icon.GetWidth()>0&&relic.IconOutline.GetWidth()>0&&ModelDb.Enchantment<HamonOverdrive>().Icon.GetWidth()>0,"遗物透明图标、轮廓及附魔矢量图标可加载");
        Check(FrierenMod.Scripts.Presentation.EventArtComposition.For(canonical).Artist=="ononotsu","正式事件保留原海报构图和画师署名");
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();LocManager.Instance.SetLanguage("eng");return count;
    }
}

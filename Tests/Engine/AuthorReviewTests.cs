using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.RestSite;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

public static class AuthorReviewTests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;GD.Print("PASS 作者核对："+name);}
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([p],seed:"AUTHOR-REVIEW-01212");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        run.PushRoom(new RestSiteRoom());run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);RunManager.Instance.ActionExecutor.Unpause();CombatTestState.SetInProgress(false);
        await CardPileCmd.Add(run.CreateCard<GrapeSpell>(p),PileType.Deck);
        CommissionState Contract(CommissionReward reward,int gold=30){
            var s=new CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10002,Act=0,Template="GRAPES",Goal=CommissionGoal.Deck,Spell="GrapeSpell",BaseGold=gold,Reward=reward,Status=CommissionStatus.Accepted}]};
            CommissionCmd.Write(p,s);return s;
        }
        async Task Claim(CommissionReward reward,int baseGold=30){var s=Contract(reward,baseGold);Check(await CommissionCmd.Execute(p,10002,CommissionOperation.Claim,s.Revision),"原生领奖成功："+reward.Kind);}
        var deck=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3,StartingPropsGranted:true)));
        MagicDeckStore.Write(p,deck);
        int before=p.Gold;await Claim(new(CommissionRewardKind.Gold,"",50));Check(p.Gold==before+80,"一星额外50加固定30，合计80");
        before=p.Gold;await Claim(new(CommissionRewardKind.Prop,"UnbelievableTool",1,ExtraProp:"MysteryBox"));
        Check(p.Gold==before+30&&MagicDeckStore.Read(p).Props.Select(x=>x.PropId).Order().SequenceEqual(new[]{"MysteryBox","UnbelievableTool"}),"一件固定工具、一件独立随机魔具及固定金币");
        before=p.Gold;await Claim(new(CommissionRewardKind.Relic,"FakeFlammeGrimoire",200,150,DuplicateGold:200),100);Check(p.Gold==before+250&&p.GetRelic<FakeFlammeGrimoire>()!=null&&p.GetRelic<FlammeGrimoire>()==null,"首幕只奖励伪作，报酬合计250金币");
        before=p.Gold;await Claim(new(CommissionRewardKind.Relic,"FakeFlammeGrimoire",200,150,DuplicateGold:200),100);Check(p.Gold==before+300,"已有伪作时合计300金币，不重复累加150");
        await Claim(new(CommissionRewardKind.Prop,"RoyalGrimoire",1,UniqueKey:"RoyalGrimoire"),50);
        Check(CommissionCmd.Read(p).ClaimedRewards.Contains("RoyalGrimoire"),"王室魔导书仅领取后标记唯一奖励");
        var potion=Contract(new(CommissionRewardKind.Potion,"BlockPotion",1)).Active!;
        Check(CommissionScreen.RewardText(p,potion).Contains("一瓶药水")&&!CommissionScreen.RewardTips(p,potion).Any(),"药水奖励不提前泄露具体品种或悬停");
        var selector=new TestCardSelector();using var selections=CombatTestState.UseSelector(selector);
        selector.PrepareToSelectCardReward((_,_)=>new());
        RewardsSet.testSelector=async set=>{await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(set.Rewards.Single());RunManager.Instance.RewardsSetSynchronizer.SkipLocalRewardsSet();};
        try{await Claim(new(CommissionRewardKind.Card,"FolkGrimoire",1,UniqueKey:"FolkGrimoire"));}
        finally{RewardsSet.testSelector=null;selector.PrepareToSelectCardRewardAtIndex(0);}
        Check(!CommissionCmd.Read(p).ClaimedRewards.Contains("FolkGrimoire")&&!CommissionCmd.Read(p).Quests.Single().RewardReceived,"跳过民间魔导书卡牌奖励不消耗唯一领取次数");
        Check(ModelDb.Event<Neow>().AllPossibleOptions.Count(o=>o.Relic is GamblerCoin)==1,"赌徒金币通过原生涅奥遗物选项加入候选");
        await RelicCmd.Obtain<MagicShopMembership>(p);p.Creature.SetCurrentHpInternal(20);
        var sync=RunManager.Instance.RestSiteSynchronizer;sync.BeginRestSite();
        int shop=sync.GetOptionsForPlayer(p).ToList().FindIndex(o=>o is BuyMagicPropsOption);selector.PrepareToSelect(new[]{0,1});
        Check(await sync.ChooseLocalOption(shop)&&sync.GetOptionsForPlayer(p).Count==2&&!sync.GetOptionsForPlayer(p).Any(o=>o is BuyMagicPropsOption),"会员逛店成功后保留休息和锻造，但本营地不能重复逛店");
        Check(await sync.ChooseLocalOption(0)&&sync.GetOptionsForPlayer(p).Count==0,"下一次普通休息仍正常消费行动，会员卡不扩大其他选项");
        deck=MagicDeckStore.Read(p);int pot=deck.AddProp("MagicPot").InstanceId;MagicDeckStore.Write(p,deck);
        before=p.Gold;int props=deck.Props.Count;
        Check(await MagicPropCmd.UseSelected(p,pot,[])&&await MagicPropCmd.UseSelected(p,pot,[])&&p.Gold==before-60&&MagicDeckStore.Read(p).Props.Count==props+2&&MagicDeckStore.Read(p).Props.Any(x=>x.InstanceId==pot),"陶罐连续使用扣60金币、生成两件魔具并保留原实例");
        p.Gold=29;Check(!MagicPropCmd.CanUse(p,pot),"陶罐金币不足30时不可使用");
        int honest=p.Deck.Cards.OfType<HonestLiving>().Count();await RelicCmd.Obtain<GamblerCoin>(p);
        Check(p.Gold==0&&p.Deck.Cards.OfType<HonestLiving>().Count()==honest+1,"赌徒金币实际失去全部金币并通过原生加牌加入指定卡");
        for(int i=0;i<3;i++)run.AppendToMapPointHistory(MapPointType.Unknown,RoomType.Monster,null);
        var guide=new CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10002,Act=0,Template="GUIDE",Stars=2,Goal=CommissionGoal.UnknownRooms,Target=4}]};CommissionCmd.Write(p,guide);
        guide=CommissionCmd.State(p);Check(guide.Quests.Single().Progress==3,"从原生地图历史补回接取前的问号战斗房");
        guide.Accept(10002,0,1);CommissionCmd.Write(p,guide);run.AppendToMapPointHistory(MapPointType.Unknown,RoomType.Event,null);
        Check(CommissionState.Ready(CommissionCmd.State(p).Active!,_=>false)&&CommissionCmd.State(p).Active!.Progress==4,"第四个问号房达标，重复查看不重复计数");
        var encounter=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Monster&&e.ShouldGiveRewards).ToMutable();encounter.GenerateMonstersWithSlots(run);
        var room=new CombatRoom(encounter,run);run.PushRoom(room);var combat=room.CombatState;combat.AddPlayer(p);
        var enemy=combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
        deck=MagicDeckStore.Read(p);int vial=deck.AddProp("MagicExamVial").InstanceId;MagicDeckStore.Write(p,deck);p.Creature.SetCurrentHpInternal(1);
        var boss=new CombatRoom(ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Boss).ToMutable(),run);run.PushRoom(boss);
        Check(!MagicPropCmd.CanUse(p,vial)&&MagicDeckStore.Read(p).Props.Any(x=>x.InstanceId==vial),"Boss战禁用考试小瓶，且不消耗道具");run.PopCurrentRoom();
        CommissionState BattleContract(CommissionGoal goal,int target){var s=new CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10002,Act=0,Template="STARK",Goal=goal,Target=target,Status=CommissionStatus.Accepted}]};CommissionCmd.Write(p,s);return s;}
        BattleContract(CommissionGoal.FirstTurnBattle,1);await Hook.BeforeCombatStart(run,combat);CommissionCmd.Victory(run,room);
        Check(CommissionCmd.Read(p).Active!.Progress==1,"首回合胜利由真实战斗记录计数");
        BattleContract(CommissionGoal.FlawlessBattle,3);await Hook.BeforeCombatStart(run,combat);p.Creature.SetCurrentHpInternal(20);
        await FrierenMod.Scripts.Shared.Compatibility.GameApi.Damage(new ThrowingPlayerChoiceContext(),p.Creature,1,ValueProp.Unpowered|ValueProp.Unblockable,enemy,null);
        CommissionCmd.Victory(run,room);Check(CommissionCmd.Read(p).LostHp&&CommissionCmd.Read(p).Active!.Progress==0,"实际未格挡生命伤害阻止无伤委托计数");
        BattleContract(CommissionGoal.FlawlessBattle,3);await Hook.BeforeCombatStart(run,combat);CommissionCmd.Victory(run,room);CommissionCmd.Victory(run,room);
        Check(CommissionCmd.Read(p).Active!.Progress==1,"无伤胜利每场只累积一次");p.Creature.SetCurrentHpInternal(1);
        Check(await MagicPropCmd.UseSelected(p,vial,[])&&p.Creature.CurrentHp==1+decimal.Floor(p.Creature.MaxHp*.30m)&&!MagicDeckStore.Read(p).Props.Any(x=>x.InstanceId==vial),"考试小瓶治疗30%最大生命并消耗");
        Check(combat.Enemies.Count==0&&combat.EscapedCreatures.Contains(enemy)&&!room.Encounter.ShouldGiveRewards,"考试小瓶复用原生逃跑，逃跑战斗不产生战利品");
        Check(MagicExamEscape.IsEscaped(combat),"逃跑标记持久化于所属跑局房间");
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();return count;
    }
}

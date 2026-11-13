using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Presentation;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class SlotBackUnlockTests
{
    public static async Task<int> Run()
    {
        int count=0;void Check(bool value,string text){if(!value)throw new Exception(text);count++;GD.Print("PASS 二代解锁："+text);}
        SaveManager.Instance.InitPrefsDataForTest();PresentationSettings.Achievements=true;
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,other],seed:"SLOT-BACK-UNLOCKS");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;CombatTestState.SetInProgress(false);
        SlotBackStore.Read();
        void Reset(){AccessTools.Field(typeof(SlotBackStore),"_progress").SetValue(null,new SlotBackProgress());SaveManager.Instance.Progress=ProgressState.CreateDefault();}
        bool Has(string id)=>SlotBackStore.Read().Unlocked.Contains(id);
        Reset();SlotBackStore.CollectLibraryRewards();Check(Has("8015")&&!Has("8011"),"打开收藏赠送8015，未集齐20张不提前解锁8011");
        foreach(string id in SlotBackCatalog.LegacyIds.Take(20))SlotBackStore.Unlock(id);
        Check(Has("8011"),"获得第20张时立即解锁收藏成就");
        Reset();var stats=SaveManager.Instance.Progress.GetOrCreateCharacterStats(p.Character.Id);stats.TotalLosses=2;
        NewSlotBackAchievements.ReconcileProfile();Check(Has("8001")&&!Has("8004")&&!Has("8012"),"两次失败也算完成两局，胜场成就不提前发");
        stats.TotalWins=5;NewSlotBackAchievements.ReconcileProfile();Check(Has("8004")&&!Has("8012"),"5胜解锁8004，10胜尚锁定");
        stats.TotalWins=10;NewSlotBackAchievements.ReconcileProfile();Check(Has("8012"),"原生历史10胜可补发8012");
        Reset();var deck=new MagicDeckState();var a=deck.Add("Hypnosis");var b=deck.Add("BlackGoldWings");MagicDeckStore.Write(p,deck);
        Check(!Has("8003"),"仅持有两个因子不算同时装载");deck.Equip(a.InstanceId,ManaSlot.Draw);deck.Equip(b.InstanceId,ManaSlot.Hand);MagicDeckStore.Write(other,deck);
        Check(!Has("8003"),"远端玩家装载不替本机解锁");MagicDeckStore.Write(p,deck);Check(Has("8003"),"同时装载精神操作与黑金之翼解锁");
        Reset();await CombatTestState.BeginEvent((FoodEvent)ModelDb.Event<FoodEvent>().ToMutable(),p);Check(!Has("8002"),"普通事件不算冒险物语");
        await CombatTestState.BeginEvent((AnimalWell)ModelDb.Event<AnimalWell>().ToMutable(),p);Check(Has("8002"),"动物井进入冒险物语解锁");
        Reset();await CombatTestState.BeginEvent((HollowKnightAdventure)ModelDb.Event<HollowKnightAdventure>().ToMutable(),p);Check(Has("8002"),"空洞骑士也算冒险物语");
        Reset();SaveManager.Instance.Progress.MarkCardAsSeen(ModelDb.Card<FrierenTheSlayer>().Id);NewSlotBackAchievements.ReconcileProfile();Check(!Has("8006"),"只看过先古卡不会误判已获得");
        await CardPileCmd.Add(run.CreateCard<FrierenTheSlayer>(p),PileType.Deck);Check(Has("8006"),"原生获得先古葬送的芙莉莲解锁");
        await CardPileCmd.Add(run.CreateCard<EndlessFlow>(p),PileType.Deck);Check(Has("8020"),"原生获得先古生生流转解锁");
        Reset();var legendary=ModelDb.AllCards.Where(c=>c is ILegendaryCard && c.ShouldShowInCardLibrary).ToArray();
        foreach(var card in legendary.SkipLast(1))SaveManager.Instance.Progress.MarkCardAsSeen(card.Id);
        NewSlotBackAchievements.ReconcileProfile();Check(!Has("8018"),"传说魔法差一张时不提前解锁");
        var last=run.CreateCard(legendary[^1],p);SaveManager.Instance.MarkCardAsSeen(last);Check(Has("8018"),"原生百科最后一张传说魔法被发现后解锁");
        Reset();var state=new CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10002,Act=0,Goal=CommissionGoal.FirstTurnBattle,Target=1,Status=CommissionStatus.Available}]};
        CommissionCmd.Write(p,state);Check(!Has("8014"),"展示未接取委托不算完成");state.Quests[0].Status=CommissionStatus.Accepted;state.Quests[0].Progress=1;CommissionCmd.Write(p,state);
        Check(Has("8014")&&SlotBackStore.Read().CompletedCommissions.Count==1,"达标未领奖也计一次完成");
        CommissionCmd.Write(p,CommissionState.FromJson(state.ToJson()));Check(SlotBackStore.Read().CompletedCommissions.Count==1,"重复刷新及存档往返不重复累计");
        state.Quests[0].Status=CommissionStatus.Claimed;
        for(int i=1;i<20;i++)state.Quests.Add(new(){Id=10002+i,Goal=i==19?CommissionGoal.EmpoweredElite:CommissionGoal.FirstTurnBattle,Status=CommissionStatus.Claimed});
        CommissionCmd.Write(p,state);Check(Has("8009")&&Has("8013")&&SlotBackStore.Read().CompletedCommissions.Count==20,"20次委托及完成强化精英分别解锁");
        Check(SlotBackProgress.FromJson(SlotBackStore.Read().ToJson()).CompletedCommissions.Count==20,"累计记录随收藏档案保存");
        Reset();p.Gold=998;await PlayerCmd.GainGold(1,p);Check(!Has("8016"),"999金币不满足超过999");await PlayerCmd.GainGold(1,p);Check(Has("8016"),"原生获得金币达到1000时解锁");
        Reset();NewSlotBackAchievements.RunVictory(p,1801);Check(!Has("8010"),"30分01秒不满足速通");NewSlotBackAchievements.RunVictory(p,1800);Check(Has("8010"),"正好30分钟满足速通");
        foreach(var card in new CardModel[]{run.CreateCard<GrapeSpell>(p),run.CreateCard<Snuggle>(p)})await CardPileCmd.Add(card,PileType.Deck);
        NewSlotBackAchievements.RunVictory(p,2000);Check(!Has("8021"),"两张双色卡不足3张");await CardPileCmd.Add(run.CreateCard<Snuggle>(p),PileType.Deck);
        NewSlotBackAchievements.RunVictory(p,2000);Check(Has("8021"),"胜利时至少3张双色卡，重复卡按张数算");
        var encounter=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Monster&&e.ShouldGiveRewards).ToMutable();encounter.GenerateMonstersWithSlots(run);
        var room=new CombatRoom(encounter,run);run.PushRoom(room);var combat=room.CombatState;combat.AddPlayer(p);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        Reset();p.Creature.SetCurrentHpInternal(1);p.Creature.HealInternal(19);Check(!Has("8008"),"战内实际回复19点不足门槛");
        p.Creature.HealInternal(1);Check(Has("8008"),"战内多次实际回复累计20点解锁");
        Reset();other.Creature.SetCurrentHpInternal(1);other.Creature.HealInternal(50);Check(!Has("8008"),"队友治疗不替本机解锁");
        p.Creature.SetCurrentHpInternal(p.Creature.MaxHp);p.Creature.HealInternal(999);Check(!Has("8008"),"满血过量治疗不计入实际回复");
        Reset();NewSlotBackAchievements.Victory(room);Check(!Has("8007"),"首回合普通战胜利不算Boss");
        // 实际原生777投点与金币结算，避免仅验证一个手动解锁调用。
        Reset();var rng=run.Rng.Niche;
        for(int counter=rng.ToSerializable().counter;counter<100000;counter++){
            var probe=new Rng(rng.ToSerializable());for(int advance=rng.ToSerializable().counter;advance<counter;advance++)probe.NextInt(8);
            if(Enumerable.Range(0,3).All(_=>probe.NextInt(4,8)==7)){while(rng.ToSerializable().counter<counter)rng.NextInt(8);break;}
        }
        var money=combat.CreateCard<HonestLiving>(p);await CardPileCmd.Add(money,PileType.Hand);
        await money.OnPlayWrapper(new ThrowingPlayerChoiceContext(),null,false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);
        Check(Has("8017"),"老实赚钱实际投出777并领取金币后解锁");
        Reset();await MagicExamEscape.Flee(p);Check(Has("8005"),"原生非Boss逃跑成功解锁");NewSlotBackAchievements.Victory(room);Check(!Has("8007")&&!Has("8022")&&!Has("8023"),"逃跑不授予击败成就");
        Reset();SaveManager.Instance.Progress.GetOrCreateEnemyStats(ModelDb.Monster<HunterKiller>().Id).FightStats.Add(new(){Character=p.Character.Id,Wins=1});
        SaveManager.Instance.Progress.GetOrCreateEnemyStats(ModelDb.Monster<InfestedPrism>().Id).FightStats.Add(new(){Character=p.Character.Id,Wins=1});NewSlotBackAchievements.ReconcileProfile();
        Check(Has("8022")&&Has("8023"),"原生历史击败猎人杀手与感染棱柱补发");
        CombatTestState.SetInProgress(false);CombatManager.Instance.Reset(true);
        var bossEncounter=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Boss).ToMutable();bossEncounter.GenerateMonstersWithSlots(run);
        var boss=new CombatRoom(bossEncounter,run);run.PushRoom(boss);boss.CombatState.AddPlayer(p);CombatManager.Instance.SetUpCombat(boss.CombatState);
        NewSlotBackAchievements.Victory(boss);Check(Has("8007"),"首回合Boss胜利解锁");
        Check(!Has("8019"),"未实装的圣雪结晶事件成就保持锁定");
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();return count;
    }
}

using BaseLib.Config.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

public static class CommissionTests
{
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;GD.Print("PASS 委托："+label);}
        async Task Tick(){for(int i=0;i<8;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        var context=new ThrowingPlayerChoiceContext();SaveManager.Instance.InitPrefsDataForTest();
        var selector=new TestCardSelector();using var selections=CombatTestState.UseSelector(selector);
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,other],seed:"VILLAGE-COMMISSIONS");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        run.ActFloor=1;run.PushRoom(new RestSiteRoom());run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);CombatTestState.SetInProgress(false);RunManager.Instance.ActionExecutor.Unpause();
        int Id(int slot)=>CommissionState.QuestId(run.CurrentActIndex,slot);
        Commission Make(int slot,string template,CommissionGoal goal,string requirement="",CommissionReward? reward=null,int affix=0)=>new(){Id=Id(slot),Act=run.CurrentActIndex,Template=template,Stars=slot==1?3:template=="CARGO"||template=="TUTOR"?2:1,Goal=goal,Spell=requirement,Reward=reward??new(CommissionRewardKind.Gold,"",80),Affix=affix};
        CommissionState Fixture(Player player,Commission? first=null,Commission? second=null,Commission? third=null){var s=new CommissionState{CurrentAct=run.CurrentActIndex,GeneratedActs=[run.CurrentActIndex],Quests=[first??Make(1,"HUNT",CommissionGoal.EmpoweredElite,reward:new(CommissionRewardKind.Prop,"RoyalGrimoire",1)),second??Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell"),third??Make(3,"CARGO",CommissionGoal.Cargo,reward:new(CommissionRewardKind.Freight,"",1))]};CommissionCmd.Write(player,s);return s;}
        async Task<CardModel> Add(Player player,CardModel model,bool upgraded=false){var card=run.CreateCard(model,player);if(upgraded)CardCmd.Upgrade(card);await CardPileCmd.Add(card,PileType.Deck);return card;}
        var generated=CommissionCmd.State(p);var original=generated.ToJson();
        Check(generated.Quests.Count==3&&generated.Quests[0].Stars==3&&CommissionCmd.State(p).ToJson()==original,"本幕三项含固定三星，重复查看不重掷");
        Check(CommissionCmd.State(other).Quests[0].Affix==generated.Quests[0].Affix,"联机双方同一幕显示同一种精英强化");
        Check(CommissionState.Villagers.Length>=15&&generated.Quests.Skip(1).Select(q=>q.Template).Distinct().Count()==2,"丰富村民池且两条随机任务不重复");
        var state=Fixture(p);int stale=state.Revision;
        Check(CommissionScreen.NeedsAttention(p),"有可接任务且未接取时提示");
        Check(await CommissionCmd.Send(p,Id(2),CommissionOperation.Accept)&&!await CommissionCmd.Send(p,Id(3),CommissionOperation.Accept),"原生同步动作接取，最多一项");
        Check(!await CommissionCmd.Execute(p,Id(1),CommissionOperation.Accept,stale),"拒绝旧版本输入");
        Check(!CommissionScreen.NeedsAttention(p)&&!await CommissionCmd.Send(p,Id(2),CommissionOperation.Claim),"未完成任务不闪烁也不能领取");
        var grape=await Add(p,ModelDb.Card<GrapeSpell>(),true);
        Check(CommissionCmd.OwnsSpell(p,"GrapeSpell")&&CommissionScreen.NeedsAttention(p),"升级版指定魔法满足要求，完成时提示");
        Check(CommissionScreen.CurrentSummary(p).Contains(CommissionCmd.Spell("GrapeSpell").Title),"委托栏悬停摘要包含当前任务与实际要求");
        int gold=p.Gold;Check(await CommissionCmd.Send(p,Id(2),CommissionOperation.Claim)&&p.Gold==gold+80&&p.Deck.Cards.Contains(grape),"金币报酬正确，提交不删除指定魔法");
        Check(!await CommissionCmd.Send(p,Id(2),CommissionOperation.Claim)&&p.Gold==gold+80,"重复领奖拒绝");
        state=Fixture(p,second:Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell",new(CommissionRewardKind.Card,"FolkGrimoire",1)));state.Accept(Id(2),0,1);CommissionCmd.Write(p,state);
        Check(await CommissionCmd.Execute(p,Id(2),CommissionOperation.Claim,state.Revision)&&p.Deck.Cards.OfType<FolkGrimoire>().Any(c=>c.IsUpgraded),"一星奖励池可给予民间的魔导书+");
        state=Fixture(p,second:Make(2,"TUTOR",CommissionGoal.Deck,"GrapeSpell",new(CommissionRewardKind.Relic,"Anchor",160)));state.Accept(Id(2),0,1);CommissionCmd.Write(p,state);
        Check(await CommissionCmd.Execute(p,Id(2),CommissionOperation.Claim,state.Revision)&&p.Relics.Any(r=>r.Id==ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.Anchor>().Id),"普通遗物奖励走原生获得命令");
        state=Fixture(p,second:Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell",new(CommissionRewardKind.Potion,"BlockPotion",1,30)));state.Accept(Id(2),0,1);CommissionCmd.Write(p,state);
        while(p.HasOpenPotionSlots)await PotionCmd.TryToProcure(ModelDb.Potion<BlockPotion>().ToMutable(),p);
        gold=p.Gold;Check(!await CommissionCmd.Execute(p,Id(2),CommissionOperation.Claim,state.Revision)&&p.Gold==gold&&CommissionCmd.Read(p).Active!=null,"满药水栏保留任务且不先给金币");
        await PotionCmd.Discard(p.Potions.First());Check(await CommissionCmd.Execute(p,Id(2),CommissionOperation.Claim,CommissionCmd.State(p).Revision)&&p.Gold==gold+30,"空出栏位后领取药水及金币");
        var rares=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(c=>c.Rarity==CardRarity.Rare&&FrierenCardAvailability.IsRandomCandidate(c)).Take(3).Select(c=>c.GetType().Name).ToArray();
        state=Fixture(p,second:Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell",new(CommissionRewardKind.RareChoice,"",1,Choices:rares)));state.Accept(Id(2),0,1);CommissionCmd.Write(p,state);
        {selector.PrepareToSelectCardRewardAtIndex(1);Check(await CommissionCmd.Execute(p,Id(2),CommissionOperation.Claim,state.Revision)&&p.Deck.Cards.Any(c=>c.GetType().Name==rares[1]),"固定金卡三选一实际选牌并加入主牌组");}
        state=Fixture(p,second:Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell",new(CommissionRewardKind.RareChoice,"",1,Choices:rares)));state.Accept(Id(2),0,1);CommissionCmd.Write(p,state);
        int deckBeforeSkip=p.Deck.Cards.Count;
        selector.PrepareToSelectCardReward((_,_)=>new());
        MegaCrit.Sts2.Core.Rewards.RewardsSet.testSelector=async set=>{
            Check(set.Rewards.Single() is MegaCrit.Sts2.Core.Rewards.CardReward {CanSkip:true},"委托卡牌奖励使用可跳过的原生战利品模型");
            Check(!await RunManager.Instance.RewardsSetSynchronizer.SelectLocalReward(set.Rewards.Single()),"不选卡即可返回战利品列表，无需先取消另一张");
            RunManager.Instance.RewardsSetSynchronizer.SkipLocalRewardsSet();
        };
        try{Check(await CommissionCmd.Send(p,Id(2),CommissionOperation.Claim)&&p.Deck.Cards.Count==deckBeforeSkip,"原生跳过整份卡牌奖励不强制获得卡牌，动作队列正常恢复");}
        finally{MegaCrit.Sts2.Core.Rewards.RewardsSet.testSelector=null;selector.PrepareToSelectCardRewardAtIndex(0);}
        await CardPileCmd.RemoveFromDeck(other.Deck.Cards.ToArray(),false);
        for(int i=0;i<3;i++)await Add(other,ModelDb.Card<DefensiveMagic>());
        Check(!CommissionCmd.OwnsSpell(other,"tutor"),"三张咏唱牌还不足以应聘家教");
        await Add(other,ModelDb.Card<RapidChant>());
        Check(CommissionCmd.OwnsSpell(other,"tutor"),"防御魔法与高速咏唱均计入四张咏唱牌");
        await CardPileCmd.RemoveFromDeck(other.Deck.Cards.ToArray(),false);
        await Add(other,ModelDb.Card<GrapeSpell>());await Add(other,ModelDb.Card<GrapeSpell>(),true);await Add(other,ModelDb.Card<RapidChant>());
        Check(!CommissionCmd.OwnsSpell(other,"tutor")&&CommissionCmd.TutorCounts(other).Mana==2,"同名及升级版生成魔力牌不重复算种类");
        await Add(other,ModelDb.Card<Disperse>());Check(CommissionCmd.OwnsSpell(other,"tutor"),"三种生成魔力牌满足另一条家教条件");
        for(int affix=0;affix<4;affix++){
            state=Fixture(p,first:Make(1,"HUNT",CommissionGoal.EmpoweredElite,reward:new(CommissionRewardKind.Prop,"RoyalGrimoire",1),affix:affix));state.Accept(Id(1),0,1);CommissionCmd.Write(p,state);
            var os=Fixture(other,first:Make(1,"HUNT",CommissionGoal.EmpoweredElite,affix:affix));os.Accept(Id(1),0,1);CommissionCmd.Write(other,os);
            var encounter=ModelDb.AllEncounters.First(e=>e.RoomType==RoomType.Elite).ToMutable();encounter.GenerateMonstersWithSlots(run);var room=new CombatRoom(encounter,run);run.PushRoom(room);var combat=room.CombatState;combat.AddPlayer(p);combat.AddPlayer(other);
            var initial=encounter.MonstersWithSlots[0];var enemy=combat.CreateCreature(initial.Item1,CombatSide.Enemy,initial.Item2);combat.AddCreature(enemy);int hp=enemy.MaxHp;
            CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
            await Hook.BeforeCombatStart(run,combat);int boosted=enemy.MaxHp;await CommissionCmd.Empower(combat);
            Check(boosted==hp+(affix==3?(int)Math.Ceiling(hp*.25):0)&&enemy.MaxHp==boosted&&enemy.GetPower<CommissionElitePower>()?.Amount==1,"精英生命强化正确，多人及重复通知不叠加："+affix);
            Check(CommissionCmd.Read(p).Active!.EliteBattle==CommissionCmd.BattleKey(run,room)&&CommissionCmd.Read(other).Active!.EliteBattle==CommissionCmd.Read(p).Active!.EliteBattle,"目标在开战时绑定给各自已接任务："+affix);
            if(affix==0)Check(enemy.GetPowerAmount<StrengthPower>()==2,"力量精英仅增加2力量");
            if(affix==1){enemy.LoseBlockInternal(enemy.Block);await enemy.GetPower<CommissionElitePower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,[enemy]);Check(enemy.Block==4,"护甲精英回合结束获得格挡");}
            if(affix==2){enemy.SetCurrentHpInternal(enemy.MaxHp-12);await enemy.GetPower<CommissionElitePower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,[enemy]);Check(enemy.CurrentHp==enemy.MaxHp-9,"再生精英每回合回复生命");}
            if(affix==3)Check(enemy.GetPowerAmount<ArtifactPower>()==0,"生命精英不叠加人工制品");
            other.Creature.SetCurrentHpInternal(0);CommissionCmd.Victory(run,room);CommissionCmd.Victory(run,room);
            Check(CommissionCmd.Read(p).Active!.Progress==1&&CommissionCmd.Read(other).Active!.Progress==0,"强化胜利只给存活参与者计一次："+affix);other.Creature.SetCurrentHpInternal(other.Creature.MaxHp);
            if(affix==0){
                var magic=MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,[],true,3,StartingPropsGranted:true)));int hourglass=magic.AddProp("ElvenHourglass").InstanceId;MagicDeckStore.Write(p,magic);
                foreach(var player in run.Players)player.PlayerCombatState!.Phase=PlayerTurnPhase.Play;
                if(AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetMethod!=null)AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsStarting)).SetValue(CombatManager.Instance,false);
                else {var turn=AccessTools.Field(typeof(CombatManager),"_turnState").GetValue(CombatManager.Instance);AccessTools.Property(turn.GetType(),"IsStarting").SetValue(turn,false);}
                if(AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetMethod!=null)AccessTools.Property(typeof(CombatManager),nameof(CombatManager.PlayerActionsDisabled)).SetValue(CombatManager.Instance,false);
                RunManager.Instance.ActionExecutor.Unpause();RunManager.Instance.ActionQueueSynchronizer.SetCombatState(Enum.Parse<MegaCrit.Sts2.Core.Entities.Multiplayer.ActionSynchronizerCombatState>("PlayPhase"));
                for(int i=0;i<3;i++)await CardPileCmd.Add(combat.CreateCard<StrikeFrieren>(p),PileType.Draw);
                int energy=p.PlayerCombatState!.Energy,hand=p.PlayerCombatState.Hand.Cards.Count;
                bool used=await MagicPropCmd.UseSelected(p,hourglass,[]);
                Check(used&&p.PlayerCombatState.Energy==energy+3&&p.PlayerCombatState.Hand.Cards.Count==hand+3,"时砂漏实际经原生同步动作获得3能量并抽3牌");
                var carried=run.CreateCard<ValuableCargo>(p);carried.Fare=10;await CardPileCmd.Add(carried,PileType.Deck);
                var battleCopy=combat.CreateCard<ValuableCargo>(p);battleCopy.Fare=10;AccessTools.Property(typeof(CardModel),nameof(CardModel.DeckVersion)).SetValue(battleCopy,carried);await CardPileCmd.Add(battleCopy,PileType.Hand);
                int refreshed=0;void Changed()=>refreshed++;p.PlayerCombatState.Hand.ContentsChanged+=Changed;
                CommissionCargo.Damaged(p.Creature,new DamageResult(p.Creature,ValueProp.Move){UnblockedDamage=1});
                Check(carried.Fare==8&&battleCopy.Fare==8&&refreshed>0,"战斗副本同步主牌组运费且不重复扣费，并通知手牌刷新");
                p.PlayerCombatState.Hand.ContentsChanged-=Changed;await CommissionCargo.Remove(p,0);
            }
            CombatManager.Instance.Reset(true);run.PushRoom(new RestSiteRoom());CombatTestState.SetInProgress(false);
        }
        var book=MagicDeckStore.Read(p);int royal=book.AddProp("RoyalGrimoire").InstanceId;MagicDeckStore.Write(p,book);
        int factorsBefore=book.Items.Count;var royalChoices=SpellCatalog.Available.OrderBy(f=>f.Id,StringComparer.Ordinal).ToArray();int puzzleIndex=Array.FindIndex(royalChoices,f=>f.Puzzle);selector.PrepareToSelect(new[]{puzzleIndex});
        Check(await MagicPropCmd.UseSelected(p,royal,[])&&MagicDeckStore.Read(p).Items.Count==factorsBefore+1&&MagicDeckStore.Read(p).Items.Last().SpellId==royalChoices[puzzleIndex].Id&&MagicDeckStore.Read(p).Items.Last().Slot<0,"王室魔导书可选择谜题，新增一个库存因子且不自动装槽");
        book=MagicDeckStore.Read(p);int quill=book.AddProp("PhoenixQuill").InstanceId;int factor=book.Items[0].InstanceId;MagicDeckStore.Write(p,book);
        Check(await MagicPropCmd.UseSelected(p,quill,[factor])&&MagicDeckStore.Read(p).Props.Count(x=>x.PropId=="UnbelievableTool")==2&&MagicDeckStore.Read(p).Items.All(i=>i.InstanceId!=factor),"羽笔实际替换因子并生成两件工具");
        state=Fixture(p);state.Accept(Id(3),0,1);CommissionCmd.Write(p,state);await CommissionCargo.Add(p,Id(3));
        Check(CommissionCargo.Goods(p).Single().Fare==0,"护送开始生成运费0的货物");
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);await Hook.AfterRoomEntered(run,run.CurrentRoom!);await CommissionCmd.Entered(run);
        var cargo=CommissionCargo.Goods(p).Single();Check(cargo.Fare==20,"新地点加20，重复房间通知不重复加运费");
        var copy=(ValuableCargo)run.CloneCard(cargo);await CardPileCmd.Add(copy,PileType.Deck);copy.Fare=5;
        Check(cargo.Fare==20&&copy.LastStop==cargo.LastStop&&copy.ContractId==cargo.ContractId,"原生永久复制继承状态且运费相互独立");copy.Fare=20;
        var hit=new DamageResult(p.Creature,ValueProp.Move){UnblockedDamage=1};
        await Hook.AfterDamageReceived(context,run,null,p.Creature,hit,ValueProp.Move,null,null);await Hook.AfterDamageReceived(context,run,null,p.Creature,hit,ValueProp.Move,null,null);
        await Hook.AfterDamageReceived(context,run,null,p.Creature,new DamageResult(p.Creature,ValueProp.Move){BlockedDamage=99,UnblockedDamage=0},ValueProp.Move,null,null);
        Check(cargo.Fare==16&&copy.Fare==16,"每段实际伤害分别扣2，完全格挡不扣，复数货物分别计费");
        var json=JsonSerializer.Serialize(p.ToSerializable(),JsonSerializationUtility.Options);var restored=Player.FromSerializable(JsonSerializer.Deserialize<SerializablePlayer>(json,JsonSerializationUtility.Options)!);
        Check(CommissionCargo.Goods(restored).Length==2&&CommissionCargo.Goods(restored).All(c=>c.Fare==16&&c.LastStop==cargo.LastStop)&&CommissionCmd.Read(restored).ToJson()==CommissionCmd.Read(p).ToJson(),"原生玩家存档保留每份货物运费、地点与当前委托");
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);await CommissionCmd.Entered(run);Check(cargo.Fare==36&&copy.Fare==36,"复制品一起到新地点，各增加20");
        var shop=new MerchantRoom();run.PushRoom(shop);run.AppendToMapPointHistory(MapPointType.Shop,RoomType.Shop,null);gold=p.Gold;await Hook.AfterRoomEntered(run,shop);await CommissionCmd.Entered(run,shop);
        Check(p.Gold==gold+112&&CommissionCargo.Goods(p).Length==0&&CommissionCmd.Read(p).Quests.Single(q=>q.Id==Id(3)).Status==CommissionStatus.Claimed,"到商人自动移除两份商品并支付112运费，重复通知不重付");
        state=Fixture(p);state.Accept(Id(3),0,1);CommissionCmd.Write(p,state);await CommissionCargo.Add(p,Id(3));cargo=CommissionCargo.Goods(p).Single();cargo.Fare=1;CommissionCargo.Damaged(p.Creature,hit);CommissionCargo.Damaged(p.Creature,hit);
        Check(cargo.Fare==0,"运费不会为负数");gold=p.Gold;
        await CommissionCmd.Entered(run,shop);Check(CommissionCargo.Goods(p).Length==1&&CommissionCmd.Read(p).Active?.Goal==CommissionGoal.Cargo,"商人处接取后重开当前地点不能提前交货");
        Check(await CommissionCmd.Send(p,Id(3),CommissionOperation.Abandon)&&CommissionCargo.Goods(p).Length==0&&p.Gold==gold&&!await CommissionCmd.Send(p,Id(3),CommissionOperation.Accept),"放弃归还货物不付运费，该条不能重接");
        state=Fixture(p);state.Accept(Id(3),0,1);CommissionCmd.Write(p,state);await CommissionCargo.Add(p,Id(3));CommissionCargo.Goods(p).Single().Fare=99;
        int previous=Id(3);run.CurrentActIndex=1;run.ActFloor=1;gold=p.Gold;await CommissionCmd.Entered(run,new RestSiteRoom());
        Check(CommissionCargo.Goods(p).Length==0&&p.Gold==gold&&CommissionCmd.Read(p).Quests.Single(q=>q.Id==previous).Status==CommissionStatus.Abandoned,"过幕自动放弃并清理商品，不支付未交付运费");
        state=Fixture(p,second:Make(2,"GRAPES",CommissionGoal.Deck,"GrapeSpell"));state.Accept(Id(2),1,1);CommissionCmd.Write(p,state);
        int carriedQuest=Id(2);run.CurrentActIndex=2;run.ActFloor=1;state=CommissionCmd.State(p);
        Check(state.Quests.Single(q=>q.Id==carriedQuest).Completed&&state.ActiveCount==0&&CommissionScreen.NeedsAttention(p),"跨幕保留已完成任务资格并提示领奖，不占新幕名额");
        await CardPileCmd.RemoveFromDeck(p.Deck.Cards.OfType<GrapeSpell>().ToArray(),false);gold=p.Gold;
        Check(await CommissionCmd.Send(p,carriedQuest,CommissionOperation.Claim)&&p.Gold==gold+80,"跨幕后失去条件牌仍可领取已完成报酬");
        run.CurrentActIndex=1;
        Check(CommissionCmd.BossRelics.All(t=>CommissionCmd.Relic(t.Name).Rarity==RelicRarity.Event),"一代Boss遗物不在普通掉落池");
        state=Fixture(p,first:Make(1,"HUNT",CommissionGoal.EmpoweredElite,reward:new(CommissionRewardKind.Relic,"ComfortableBed",1,FallbackProp:"PhoenixQuill")));state.Accept(Id(1),1,1);state.Active!.Progress=1;CommissionCmd.Write(p,state);
        int maxEnergy=p.MaxEnergy;Check(await CommissionCmd.Execute(p,Id(1),CommissionOperation.Claim,state.Revision)&&p.Relics.OfType<ComfortableBed>().Any()&&p.GetRelic<ComfortableBed>()!.ModifyMaxEnergy(p,maxEnergy)==maxEnergy+1,"第二幕一代Boss遗物实际获得并保留能量加成");
        state=Fixture(p,first:Make(1,"HUNT",CommissionGoal.EmpoweredElite,reward:new(CommissionRewardKind.Relic,"ComfortableBed",1,FallbackProp:"PhoenixQuill")));state.Accept(Id(1),1,1);state.Active!.Progress=1;CommissionCmd.Write(p,state);int props=MagicDeckStore.Read(p).Props.Count;
        Check(await CommissionCmd.Execute(p,Id(1),CommissionOperation.Claim,state.Revision)&&MagicDeckStore.Read(p).Props.Count==props+1&&MagicDeckStore.Read(p).Props.Last().PropId=="PhoenixQuill","重复Boss遗物按预先显示的专属魔具兜底");
        var display=Fixture(p,first:Make(1,"HUNT",CommissionGoal.EmpoweredElite,reward:new(CommissionRewardKind.Prop,"MagicExamVial",1)),second:Make(2,"TUTOR",CommissionGoal.Deck,"tutor",new(CommissionRewardKind.Relic,"BagOfPreparation",120)),third:Make(3,"CARGO",CommissionGoal.Cargo,reward:new(CommissionRewardKind.Freight,"",1)));
        display.Quests=display.Quests.Select(q=>new Commission{Id=q.Id,Act=q.Act,Template=q.Template,Stars=q.Stars,Goal=q.Goal,Spell=q.Spell,Reward=q.Reward,BaseGold=q.Stars==3?100:50}).ToList();CommissionCmd.Write(p,display);
        run.PushRoom(new RestSiteRoom());var ui=new Control{Size=new(1920,1080)};root.AddChild(ui);var screen=new CommissionScreen(ui,p);
        foreach(string language in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(language);screen.Open();await Tick();var list=screen.View.GetNode<VBoxContainer>("CommissionCanvas/CommissionScroll/Quests");
            Check(list.GetChildCount()==3&&list.GetChildren().Cast<Control>().All(row=>row.FindChild("Illustration",true,false) is TextureRect{Texture:not null}),"三项任务各有插图："+language);
            Check(list.FindChildren("*","Label",true,false).OfType<Label>().All(l=>!l.Text.Contains('{')&&!l.Text.Contains("COMMISSION.")),"星级、强化、条件和报酬本地化完整："+language);
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1280,720),new Vector2(2560,1080)}){ui.Size=size;await Tick();Check(new Rect2(Vector2.Zero,size).Grow(1).Encloses(screen.View.GetNode<Control>("CommissionCanvas").GetGlobalRect()),"任务板适配分辨率："+language+size);}
            ui.Size=new(1920,1080);await Tick();if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")){
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-village-"+language+".png"));
                screen.View.GetNode<ScrollContainer>("CommissionCanvas/CommissionScroll").ScrollVertical=10000;await Tick();root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-village-bottom-"+language+".png"));
            }
            screen.Close();await Tick();
        }
        screen.View.QueueFree();ui.QueueFree();await Tick();
        var art=new Control{Size=new(1920,1080)};root.AddChild(art);art.AddChild(new ColorRect{Size=art.Size,Color=new("17202c")});
        LocManager.Instance.SetLanguage("zhs");var models=MagicPropRules.CommissionOnly.Select(x=>(CardModel)MagicPropCard.Create(p,x.Id)).Append(ModelDb.Card<ValuableCargo>().ToMutable()).ToArray();
        var cards=new List<NCard>();bool renderCards=OS.GetCmdlineUserArgs().Contains("--ui-snapshot");
        for(int i=0;i<models.Length;i++){
            if(models[i] is ValuableCargo goods){goods.Owner=p;goods.Fare=120;}
            // 本机只有正式版PCK；完整原生卡面仅在正式版截图模式渲染。
            if(renderCards){var node=NodePool.Get<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,models[i]);node.Position=new(310+i*430,450);node.Scale=Vector2.One*.95f;
                art.AddChild(node);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);cards.Add(node);}
            Check(!models[i].GetDescriptionForPile(PileType.None).Contains('{')&&models[i].Portrait!=null,"新增卡牌使用真实插图且描述完整："+models[i].GetType().Name);
        }
        await Tick();if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/commission-exclusive-cards.png"));
        foreach(var card in cards){art.RemoveChild(card);card.QueueFreeSafely();}art.QueueFree();await Tick();
        LocManager.Instance.SetLanguage("zhs");RunManager.Instance.CleanUp();return checks;
    }
}

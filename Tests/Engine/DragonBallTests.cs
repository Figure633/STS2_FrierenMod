using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Enchantments;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Potions;
using FrierenMod.Scripts.Relics;
using FrierenMod.Scripts.RestSite;
using FrierenMod.Scripts.Rewards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class DragonBallTests
{
    private static IReadOnlyList<CardModel> LastOffer=[];
    public static void ObserveOffer(IReadOnlyList<CardModel> cardsIn)=>LastOffer=cardsIn;
    public static async Task<int> RunFixes(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);GD.Print("PASS 事件修正："+text);checks++;}
        SaveManager.Instance.InitProfileId(1);SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var manager=RunManager.Instance;var player=Player.CreateForNewRun<Frieren>(UnlockState.none,1);var ally=Player.CreateForNewRun<Frieren>(UnlockState.none,2);
        var run=RunState.CreateForTest([player,ally],seed:"EVENT-FIXES-01222");manager.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;manager.GenerateRooms();
        CombatTestState.SetInProgress(false);run.CurrentActIndex=1;
        async Task<T> Begin<T>() where T:EventModel {var e=(T)ModelDb.GetById<T>(ModelDb.GetId<T>()).ToMutable();await CombatTestState.BeginEvent(e,player);return e;}
        var debt=await Begin<DebtCollector>();int gold=player.Gold;
        Check(ModelDb.Event<DebtCollector>().IsAllowed(run)&&!debt.CurrentOptions[0].IsLocked,"没欠债且没有老实赚钱时，仍能进入事件并选择第一项");
        await debt.CurrentOptions[0].Chosen();Check(player.Gold>=gold+100&&player.Gold<=gold+125&&debt.IsFinished,"第一项真实获得100至125金币并结束");
        DebtStore.Add(player,7);debt=await Begin<DebtCollector>();Check(debt.CurrentOptions[0].IsLocked&&debt.CurrentOptions[1].IsLocked,"有债时第一项锁定，没有老实赚钱时第二项锁定");
        await CardPileCmd.Add(run.CreateCard<HonestLiving>(player),PileType.Deck);debt=await Begin<DebtCollector>();Check(!debt.CurrentOptions[1].IsLocked,"有债、足额金币和老实赚钱时可还债");
        var selector=new TestCardSelector();using var selection=CombatTestState.UseSelector(selector);
        var results=new HashSet<string>();
        for(int i=0;i<8;i++){
            var original=new CardModel[]{run.CreateCard<StrikeFrieren>(player),run.CreateCard<StrikeFrieren>(player)};
            foreach(var card in original)await CardPileCmd.Add(card,PileType.Deck);
            var snow=await Begin<SnowCrystalLand>();
            var old=player.Deck.Cards.ToHashSet();selector.Cleanup();selector.PrepareToSelect(original);
            await snow.CurrentOptions[0].Chosen();foreach(var added in player.Deck.Cards.Except(old).Where(c=>c is not Doubt))results.Add(added.Id.Entry);
        }
        Check(results.Count>=6&&results.All(id=>FrierenCardAvailability.IsRandomCandidate(ModelDb.AllCards.Single(c=>c.Id.Entry==id))),"同一局重复进入也按原生变化随机流推进，结果覆盖多张正常牌");
        Check(CombatTestState.RngCounter(player.PlayerRng.Transformations)==16,"每次真实变化两张，使用原生可存档的玩家变化随机流");
        MagicDeckStore.Write(player,MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3,StartingPropsGranted:true))));
        run.PushRoom(new RestSiteRoom());DragonBallStore.GrantNext(player,true);DragonBallStore.GrantNext(ally,true);
        var harmony=new Harmony("frieren.tests.event-fixes");
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd),nameof(CardSelectCmd.FromSimpleGrid)),prefix:new HarmonyMethod(typeof(DragonBallTests),nameof(ObserveOffer)));
        try {
            var shop=AccessTools.Method(typeof(MagicPropCmd),"VisitShopCore");selector.Cleanup();selector.PrepareToSelect([7,0]);
            Check(await (Task<bool>)shop.Invoke(null,[player,new BlockingPlayerChoiceContext()])!&&LastOffer.OfType<DragonBall>().Count()==1,"整局第一次可以从魔具店获得一颗龙珠");
            selector.Cleanup();selector.PrepareToSelect([0,1]);await (Task<bool>)shop.Invoke(null,[player,new BlockingPlayerChoiceContext()])!;
            Check(!LastOffer.OfType<DragonBall>().Any()&&!DragonBallStore.CanFindInShop(ally),"之后逛店不再出现龙珠，其他玩家也共享整局一次限制");
            var copy=Player.FromSerializable(player.ToSerializable());Check(DragonBallStore.Read(copy).ShopBallClaimed,"魔具店领取记录随玩家存档保存");
            run.PopCurrentRoom();
            var canonical=ModelDb.Event<DragonBallAdventure>();run.AppendToMapPointHistory(MapPointType.Unknown,RoomType.Event,canonical.Id);run.PushRoom(new EventRoom(canonical));
            manager.EventSynchronizer.BeginEvent(canonical);var story=(DragonBallAdventure)manager.EventSynchronizer.GetLocalEvent();
            harmony.Patch(AccessTools.Method(typeof(RunManager),nameof(RunManager.EnterRoomWithoutExitingCurrentRoom)),prefix:new HarmonyMethod(typeof(DemonArmyAttackTests),nameof(DemonArmyAttackTests.CaptureEnterRoom)));
            manager.CombatReplayWriter.IsEnabled=true;manager.CombatReplayWriter.StopRecording();DemonArmyAttackTests.CapturedRoom=null;
            await story.CurrentOptions[0].Chosen();var room=DemonArmyAttackTests.CapturedRoom!;
            Check(manager.CombatReplayWriter.IsRecordingReplay,"从控制台式事件入口开战也先建立原生战斗回放起点");
            harmony.Unpatch(AccessTools.Method(typeof(RunManager),nameof(RunManager.EnterRoomWithoutExitingCurrentRoom)),HarmonyPatchType.Prefix,harmony.Id);
            run.PushRoom(room);run.Map=new MockSinglePointActMap();run.AppendToMapPointHistory(MapPointType.Unknown,room.RoomType,room.ModelId);
            var combat=room.CombatState;foreach(var p in run.Players)combat.AddPlayer(p);room.Encounter.GenerateMonstersWithSlots(run);
            foreach(var(m,s)in room.Encounter.MonstersWithSlots)combat.AddCreature(combat.CreateCreature(m,CombatSide.Enemy,s));
            CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);await Hook.BeforeCombatStart(run,combat);
            var dummy=combat.Enemies.Single();await dummy.Monster!.AfterAddedToRoom();
            for(int turn=0;turn<3;turn++)await Hook.AfterTurnEnd(combat,CombatSide.Enemy,[dummy]);
            Check(((BattlewornDummyEventEncounter)room.Encounter).RanOutOfTime&&CombatManager.Instance.IsEnding,"三回合未击杀后由原生计时能力标记失败、假人逃离并结束战斗");
            await CombatManager.Instance.CheckWinCondition();Check(room.IsPreFinished&&!CombatManager.Instance.IsInProgress,"实际走完原生战斗结束结算，不因回放缺失抛错");
            await manager.ProceedFromTerminalRewardsScreen();await manager.EventSynchronizer.AwaitPendingOptionTasks();
            Check(run.CurrentRoom is EventRoom&&story.IsFinished&&story.Description!.LocEntryKey.Contains("FAILURE")&&!player.Potions.OfType<SenzuBean>().Any(),"实际返回失败事件页，没有仙豆，原生前进选项可用");
            var reward=new DragonBallReward(player);reward.Populate();TextureRect icon;
            TestMode.IsOn=false;try{icon=(TextureRect)reward.CreateIcon()!;}finally{TestMode.IsOn=true;}
            Check(icon.Texture.GetWidth()==icon.Texture.GetHeight(),"原生战利品图标使用正方形纹理，圆球不再被横向卡图比例压扁");icon.Free();
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")){
                var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new Vector2(1920,1080),Color=new Color("17202c")});
                var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
                for(int i=1;i<=7;i++){
                    var card=scene.Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(card,MagicPropCard.Create(player,DragonBallStore.Ball(i)));canvas.AddChild(card);
                    card.Position=new Vector2(150+(i-1)*270,370);card.Scale=Vector2.One*.85f;card.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
                }
                NRewardButton button;TestMode.IsOn=false;try{button=NRewardButton.Create(reward,null!);canvas.AddChild(button);}finally{TestMode.IsOn=true;}
                button.Position=new Vector2(600,780);
                for(int i=0;i<8;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
                root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/dragon-ball-fixed.png"));canvas.QueueFree();
            }
        }
        finally{manager.CombatReplayWriter.IsEnabled=false;harmony.UnpatchAll(harmony.Id);manager.CleanUp();}
        return checks;
    }
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);GD.Print("PASS 作者事件："+text);checks++;}
        SaveManager.Instance.InitProfileId(1);SaveManager.Instance.InitPrefsDataForTest(); LocalContext.NetId=1;
        var manager=RunManager.Instance;
        Player NewPlayer(ulong id=1)=>Player.CreateForNewRun<Frieren>(UnlockState.all,id);
        RunState Start(params Player[] players) {
            var state=RunState.CreateForTest(players,seed:"DRAGON-BALL-01222");manager.SetUpTest(state,new NetSingleplayerGameService());LocalContext.NetId=1;manager.GenerateRooms();
            CombatTestState.SetInProgress(false);foreach(var player in players)MagicDeckStore.Write(player,
                MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3,StartingPropsGranted:true))));return state;
        }
        Player Copy(Player player) {
            var writer=new PacketWriter();player.ToSerializable().Serialize(writer);var reader=new PacketReader();reader.Reset(writer.Buffer);
            var save=new SerializablePlayer();save.Deserialize(reader);Check(reader.BitPosition==writer.BitPosition,"玩家网络包完整往返");return Player.FromSerializable(save);
        }
        async Task<T> Event<T>(Player player) where T:EventModel { var model=(T)ModelDb.GetById<T>(ModelDb.GetId<T>()).ToMutable();await CombatTestState.BeginEvent(model,player);return model; }
        var p=NewPlayer();var ally=NewPlayer(2);var run=Start(p,ally);var context=new BlockingPlayerChoiceContext();
        var fame=run.CreateCard<Famehameha>(p);await CardPileCmd.Add(fame,PileType.Deck);CardCmd.Upgrade(fame);CardCmd.Enchant<HamonOverdrive>(fame,1);
        var dragon=await Event<DragonBallAdventure>(p);var select=new TestCardSelector();
        using(CombatTestState.UseSelector(select)){select.PrepareToSelect([0]);await dragon.CurrentOptions[1].Chosen();}
        var kame=p.Deck.Cards.OfType<Kamehameha>().Single();
        Check(kame.IsUpgraded&&kame.Enchantment is HamonOverdrive&&kame.DynamicVars.Damage.IntValue==80,"龟派气功继承升级及原附魔");
        Check(!kame.CanBeGeneratedInCombat&&!kame.CanBeGeneratedByModifiers&&kame.EnergyCost.GetWithModifiers(CostModifiers.All)==4,"金色龟派气功仅事件获取，基础4费");
        Check(DragonBallStore.GrantNext(p,true)&&DragonBallStore.Next(p)==2,"首次只获得一星龙珠");
        var deck=MagicDeckStore.Read(p);deck.Remove(deck.Props.Single(x=>x.PropId=="DragonBall1").InstanceId);MagicDeckStore.Write(p,deck);
        Check(DragonBallStore.GrantNext(p)&&DragonBallStore.Next(p)==3&&DragonBallStore.Held(p)==1,"丢失的龙珠不再获取，继续按编号发放");
        var restored=Copy(p);Check(DragonBallStore.Next(restored)==3&&DragonBallStore.Read(restored).GrantedMask==3,"龙珠丢失及已获取记录经网络存档保留");
        var reward=new DragonBallReward(p);reward.Populate();
        Check(Reward.FromSerializable(reward.ToSerializable(),p) is DragonBallReward {IsPopulated:true},"龙珠奖励经原生存档恢复");
        Check(!MagicPropRules.CanUse(MagicDeckStore.Read(p),MagicDeckStore.Read(p).Props.Single(x=>x.PropId=="DragonBall2"),false),"龙珠作为收藏魔具，不能单独使用");
        var mat=await RelicCmd.Obtain<Matryoshka>(p);var chest=new TreasureRoom(0);
        var chestSet=new RewardsSet(p).WithRewardsFromRoom(chest);await chestSet.GenerateWithoutOffering();var chestRewards=chestSet.Rewards;
        Check(chestRewards.OfType<DragonBallReward>().Count()==1&&chestRewards.OfType<RelicReward>().Any()&&mat.Counter==1,"宝箱同时提供下一颗龙珠及套娃额外遗物");
        mat.TryModifyRewards(p,[],chest);Check(mat.Counter==0&&!mat.TryModifyRewards(p,[],chest),"套娃仅增加两次宝箱遗物");
        run.PushRoom(new RestSiteRoom());manager.RestSiteSynchronizer.BeginRestSite();
        int next=DragonBallStore.Next(p);using(CombatTestState.UseSelector(select)){select.Cleanup();select.PrepareToSelect([MagicPropRules.All.Count,0]);await (Task<bool>)AccessTools.Method(typeof(MagicPropCmd),"VisitShopCore").Invoke(null,[p,context])!;}
        Check(DragonBallStore.Next(p)==next+1,"魔具店可选择且只提供下一颗龙珠");
        var honest=run.CreateCard<HonestLiving>(p);await CardPileCmd.Add(honest,PileType.Deck);DebtStore.Add(p,21);
        var debtEvent=await Event<DebtCollector>(p);using(CombatTestState.UseSelector(select)){select.PrepareToSelect([0]);await debtEvent.CurrentOptions[1].Chosen();}
        Check(DebtStore.Read(p)==0&&honest.Enchantment is LoadedDice&&!honest.Keywords.Contains(CardKeyword.Exhaust)&&DebtCollector.Settled.Get(p),"还清债务、永久灌铅并移除消耗");
        var savedHonest=CardModel.FromSerializable(honest.ToSerializable());Check(savedHonest.Enchantment is LoadedDice&&!savedHonest.Keywords.Contains(CardKeyword.Exhaust),"灌铅和不消耗经原生卡牌存档保留");
        foreach(bool upgraded in new[]{false,true}) {
            if(upgraded)CardCmd.Upgrade(honest);
            int count=0;for(int i=0;i<10000;i++)if((int)AccessTools.Method(typeof(HonestLiving),"RollDamage").Invoke(honest,null)! == 7)count++;
            double expected=upgraded?10000*(1d/3+.15):4000;Check(Math.Abs(count-expected)<220,"灌铅使最大值概率提高15个百分点，升级="+upgraded);
        }
        foreach(int act in new[]{0,1,2}) {
            run.CurrentActIndex=act;var enc=(BattlewornDummyEventEncounter)ModelDb.Encounter<BattlewornDummyEventEncounter>().ToMutable();enc.Setting=BattlewornDummyEventEncounter.DummySetting.Setting1;
            var room=new CombatRoom(enc,run){ParentEventId=ModelDb.Event<DragonBallAdventure>().Id};run.PushRoom(room);var combat=room.CombatState;
            combat.AddPlayer(p);combat.AddPlayer(ally);enc.GenerateMonstersWithSlots(run);foreach(var(m,s)in enc.MonstersWithSlots)combat.AddCreature(combat.CreateCreature(m,CombatSide.Enemy,s));
            CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);await Hook.BeforeCombatStart(run,combat);
            Check(combat.Enemies.Single().MaxHp==100*(act+1),"猫仙人复用原生三回合假人，幕数="+(act+1));
            if(act==2){
                p.Creature.SetCurrentHpInternal(1);ally.Creature.SetCurrentHpInternal(1);
                await PowerCmd.Apply<WeakPower>(context,ally.Creature,2,p.Creature,null);await PowerCmd.Apply<StrengthPower>(context,ally.Creature,3,p.Creature,null);await PowerCmd.Apply<DexterityPower>(context,ally.Creature,-2,p.Creature,null);
                var bean=(await PotionCmd.TryToProcure<SenzuBean>(p)).potion;var fairy=(await PotionCmd.TryToProcure<FairyInABottle>(p)).potion;
                Check(bean.IsValidTarget(p.Creature)&&bean.IsValidTarget(ally.Creature)&&!fairy.IsValidTarget(p.Creature)&&fairy.ShouldDie(p.Creature),"仙豆可用于自己和队友，并阻止其他药水及瓶中仙灵自动使用");
                await fairy.OnUseWrapper(context,p.Creature);Check(p.Potions.Contains(fairy)&&p.Creature.CurrentHp==1,"被禁用药水不消耗、不治疗");
                await bean.OnUseWrapper(context,ally.Creature);
                Check(ally.Creature.CurrentHp==ally.Creature.MaxHp&&!ally.Creature.HasPower<WeakPower>()&&!ally.Creature.HasPower<DexterityPower>()&&ally.Creature.GetPowerAmount<StrengthPower>()==3&&p.Creature.CurrentHp==1,"仙豆清除负面效果及负敏捷，回满目标生命，保留正面能力");
                Check(!SenzuBean.Blocks(fairy)&&!fairy.ShouldDie(p.Creature),"仙豆吃掉后恢复其他药水及自动复活");
            }
            CombatTestState.SetInProgress(false);CombatManager.Instance.Reset(true);run.PopCurrentRoom();
        }
        manager.CleanUp();p=NewPlayer();run=Start(p);
        for(int i=0;i<7;i++)Check(DragonBallStore.GrantNext(p,i==0),"按顺序收集龙珠"+(i+1));
        Check(DragonBallStore.CanSummon(p)&&DragonBallStore.Next(p)==0,"集齐后停止所有后续掉落");
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);await manager.EnterRoom(new RestSiteRoom());
        var before=manager.RestSiteSynchronizer.GetOptionsForPlayer(p).Where(o=>o is not SummonDragonOption).Select(o=>o.OptionId).ToArray();
        int summon=manager.RestSiteSynchronizer.GetLocalOptions().ToList().FindIndex(o=>o is SummonDragonOption);
        Check(summon>=0&&!await manager.RestSiteSynchronizer.ChooseLocalOption(summon),"原生营地召唤选项不消耗本次行动");
        await DragonSummoning.Pending;
        Check(run.CurrentRoom is EventRoom{CanonicalEvent:SummonDragon}&&manager.EventSynchronizer.GetLocalEvent().CurrentOptions.Count==3,"真正切换到神龙事件并保留三项愿望");
        var save=manager.ToSave(run.CurrentRoom);var saveRun=RunState.FromSerializable(JsonSerializer.Deserialize<SerializableRun>(JsonSerializer.Serialize(save,JsonSerializationUtility.Options),JsonSerializationUtility.Options)!);
        Check(save.PreFinishedRoom?.EventId==ModelDb.Event<SummonDragon>().Id&&DragonBallStore.Read(saveRun.Players[0]).Summoning&&DragonBallStore.Held(saveRun.Players[0])==7,"许愿前检查点保存正式事件、龙珠及营地选项");
        manager.CleanUp();run=saveRun;p=run.Players[0];await manager.SetUpSavedSingleplayer(run,save);LocalContext.NetId=1;
        AccessTools.Property(typeof(RunManager),nameof(RunManager.ShouldSave)).SetValue(manager,false);
        await manager.EnterRoom(new EventRoom(save.PreFinishedRoom!));
        Check(manager.EventSynchronizer.GetLocalEvent() is SummonDragon{CurrentOptions.Count:3},"实际载入许愿前检查点后仍能选择愿望");
        manager.EventSynchronizer.ChooseLocalOption(1);await manager.EventSynchronizer.AwaitPendingOptionTasks();await DragonSummoning.Pending;
        Check(run.CurrentRoom is RestSiteRoom&&p.MaxEnergy==999&&DragonBallStore.Held(p)==0&&DragonBallStore.Read(p).WishUsed,"许愿消耗七颗且只增加集齐者能量，随后返回营地");
        Check(manager.RestSiteSynchronizer.GetLocalOptions().Select(o=>o.OptionId).SequenceEqual(before),"返回后原先营地选项完整保留，无法再次召唤");
        Check(!DragonBallStore.GrantNext(p)&&!DragonBallStore.ConsumeForWish(p,"JOY"),"一局一次，重复许愿和再获取全部拒绝");
        restored=Copy(p);Check(restored.MaxEnergy==999&&DragonBallStore.Read(restored).WishUsed&&DragonBallStore.Held(restored)==0,"许愿结果与消耗经网络存档保留");
        manager.CleanUp();p=NewPlayer();run=Start(p);for(int i=0;i<7;i++)DragonBallStore.GrantNext(p,i==0);
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);await manager.EnterRoom(new RestSiteRoom());
        await manager.RestSiteSynchronizer.ChooseLocalOption(0);
        Check(manager.RestSiteSynchronizer.GetLocalOptions() is [SummonDragonOption],"先休息后仍保留免费召唤入口");
        await manager.RestSiteSynchronizer.ChooseLocalOption(0);await DragonSummoning.Pending;
        manager.EventSynchronizer.ChooseLocalOption(0);await manager.EventSynchronizer.AwaitPendingOptionTasks();await DragonSummoning.Pending;
        Check(p.Creature.CurrentHp==9999&&p.Creature.MaxHp==9999&&manager.RestSiteSynchronizer.GetLocalOptions().Count==0,"生命永恒设为9999，返回后不会重新获得用掉的休息");
        var snow=await Event<SnowCrystalLand>(p);await snow.AfterEventStarted();int deckCount=p.Deck.Cards.Count;
        using(CombatTestState.UseSelector(select)){select.Cleanup();select.PrepareToSelect([0,1]);await snow.CurrentOptions[0].Chosen();}
        Check(snow.IsFinished&&p.Deck.Cards.Count==deckCount+1&&p.Deck.Cards.Any(c=>c is Doubt),"圣雪结晶实际变化两张牌并加入疑虑");
        Check(FrierenMod.Scripts.Presentation.SlotBackStore.Read().Unlocked.Contains("8019"),"进入圣雪结晶自动解锁8019卡背");
        foreach(string lang in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(lang);
            foreach(var model in new EventModel[]{await Event<SnowCrystalLand>(p),await Event<DebtCollector>(p),await Event<DragonBallAdventure>(p)})
                Check(!model.InitialDescription.GetFormattedText().Contains("LOC_ERR")&&model.CurrentOptions.All(o=>!o.Title.GetFormattedText().Contains("LOC_ERR")&&!o.Description.GetFormattedText().Contains("LOC_ERR")),lang+"完整事件文本："+model.Id.Entry);
        }
        // 许愿中途保存：金币已到账而送货员尚未到账，重进只补后半段。
        manager.CleanUp();p=NewPlayer();run=Start(p);for(int i=0;i<7;i++)DragonBallStore.GrantNext(p,i==0);
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);
        var partial=DragonBallStore.Read(p);partial.Summoning=true;partial.CampFloor=run.TotalFloor;partial.CampOptions=["HEAL","SMITH","FRIEREN_MAGIC_SHOP"];DragonBallStore.Write(p,partial);
        DragonBallStore.ConsumeForWish(p,"JOY");await PlayerCmd.GainGold(9999,p);int paidGold=p.Gold;
        await manager.EnterRoom(new EventRoom(ModelDb.Event<SummonDragon>()));await DragonSummoning.Pending;
        Check(p.Gold==paidGold&&p.Relics.OfType<MegaCrit.Sts2.Core.Models.Relics.TheCourier>().Count()==1&&DragonBallStore.Read(p).WishResolved,"许愿中途的检查点补发缺失遗物，不重复加金币");
        manager.CleanUp();p=NewPlayer();run=Start(p);
        var create=AccessTools.Method(typeof(RunManager),"CreateRoom");
        Check(create.Invoke(manager,[RoomType.RestSite,MapPointType.Unknown,null]) is RestSiteRoom&&DragonBallStore.Read(p).EventMisses==0,"没有芙派气功时未知房间不累计龙珠事件概率");
        await CardPileCmd.Add(run.CreateCard<Famehameha>(p),PileType.Deck);var pity=DragonBallStore.Read(p);pity.EventMisses=9;DragonBallStore.Write(p,pity);
        Check(create.Invoke(manager,[RoomType.Monster,MapPointType.Unknown,null]) is EventRoom{CanonicalEvent:DragonBallAdventure}&&DragonBallStore.Read(p).EventSeen,"未知房间累计十次后保证进入龙珠冒险");
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))await Visuals(root,p);
        manager.CleanUp();LocManager.Instance.SetLanguage("eng");return checks;
    }
    private static async Task Visuals(Node root,Player player)
    {
        string directory=ProjectSettings.GlobalizePath("res://../../artifacts/new-event-review");Directory.CreateDirectory(directory);
        async Task Tick(){for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        var state=DragonBallStore.Read(player);state.Summoning=true;state.WishUsed=false;DragonBallStore.Write(player,state);
        if(!InputMap.HasAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi))InputMap.AddAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi);
        foreach(string lang in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(lang);
            foreach(var canonical in new EventModel[]{ModelDb.Event<SnowCrystalLand>(),ModelDb.Event<DebtCollector>(),ModelDb.Event<DragonBallAdventure>(),ModelDb.Event<SummonDragon>()}) {
                var model=canonical.ToMutable();await CombatTestState.BeginEvent(model,player);
                var host=new Control{Size=new Vector2(1920,1080)};root.AddChild(host);
                var layout=model.CreateScene().Instantiate<NEventLayout>();host.AddChild(layout);model.SetNode(layout);layout.SetEvent(model);layout.SetTitle(model.Title.GetFormattedText());
                var description=model.Description!;model.DynamicVars.AddTo(description);layout.SetDescription(description.GetFormattedText());layout.AddOptions(model.CurrentOptions);layout.OnSetupComplete();
                await Tick();foreach(var tween in root.GetTree().GetProcessedTweens())tween.CustomStep(10);await Tick();
                foreach(var button in layout.GetNode<VBoxContainer>("%OptionsContainer").GetChildren().OfType<Control>())
                    if(button.GetGlobalRect().End.Y>1080 || button.Modulate.A<.99f)throw new Exception("事件选项不可见或超出画面："+lang+model.Id);
                root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory,lang+"-"+model.GetType().Name+".png"));
                host.QueueFree();await Tick();
            }
            var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new Vector2(1920,1080),Color=new Color("17202c")});
            var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");
            var models=new CardModel[]{player.RunState.CreateCard<Kamehameha>(player),player.RunState.CreateCard<Kamehameha>(player),MagicPropCard.Create(player,"DragonBall1"),MagicPropCard.Create(player,"DragonBall7")};
            CardCmd.Upgrade(models[1]);
            for(int i=0;i<models.Length;i++) {
                var card=scene.Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(card,models[i]);canvas.AddChild(card);
                card.Position=new Vector2(280+i*430,500);card.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
            }
            canvas.AddChild(new TextureRect{Texture=GD.Load<Texture2D>("res://FrierenMod/images/potions/SenzuBean.png"),Position=new Vector2(900,850),Size=new Vector2(160,160),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered});
            await Tick();root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(directory,lang+"-cards.png"));canvas.QueueFree();await Tick();
        }
    }
    private sealed class DelayedHeal(Player player) : RestSiteOption(player)
    {
        public static TaskCompletionSource Gate = new();
        public static bool Started;
        public override string OptionId=>"FRIEREN_TEST_DELAYED_HEAL";
        public override async Task<bool> OnSelect(){Started=true;await Gate.Task;return await new HealRestSiteOption(Owner).OnSelect();}
    }
    public static async Task<int> RunParty(Node root,RunState run,bool host,Func<string,Task> barrier,Func<Func<bool>,string,Task> wait)
    {
        SaveManager.Instance.InitProfileId(1);SaveManager.Instance.InitPrefsDataForTest();
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);GD.Print("PASS 神龙联机："+text);checks++;}
        var manager=RunManager.Instance;var collector=run.Players[0];var ally=run.Players[1];
        manager.CombatStateSynchronizer.IsDisabled=false;
        foreach(var player in run.Players) MagicDeckStore.Write(player,MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,MagicDeckState.CreatePrototypeLoadout().Items.ToArray(),true,3,StartingPropsGranted:true))));
        for(int i=0;i<7;i++)DragonBallStore.GrantNext(collector,i==0);
        await RelicCmd.Obtain<MegaCrit.Sts2.Core.Models.Relics.TheCourier>(collector);
        int gold=collector.Gold;int allyGold=ally.Gold;ally.Creature.SetCurrentHpInternal(1);
        run.AppendToMapPointHistory(MapPointType.RestSite,RoomType.RestSite,null);await manager.EnterRoom(new RestSiteRoom());
        var sync=manager.RestSiteSynchronizer;var before=sync.GetOptionsForPlayer(collector).Where(o=>o is not SummonDragonOption).Select(o=>o.OptionId).ToArray();
        DelayedHeal.Gate=new();DelayedHeal.Started=false;
        ((List<RestSiteOption>)sync.GetOptionsForPlayer(ally)).Add(new DelayedHeal(ally));
        await barrier("dragon-camp-ready");
        Task<bool>? heal=null;if(!host)heal=sync.ChooseLocalOption(sync.GetLocalOptions().Count-1);
        await wait(()=>DelayedHeal.Started,"等待队友已开始的营地操作");
        if(host) await sync.ChooseLocalOption(sync.GetLocalOptions().ToList().FindIndex(o=>o is SummonDragonOption));
        await wait(()=>DragonBallStore.Read(collector).Summoning,"接收原生召唤选项");await barrier("dragon-requested");
        Check(run.CurrentRoom is RestSiteRoom&&DragonSummoning.IsTransitioning,"队友操作未结束时保留营地，等待完成后再切换");
        DelayedHeal.Gate.SetResult();if(heal!=null)await heal;
        await wait(()=>run.CurrentRoom is EventRoom{CanonicalEvent:SummonDragon}&&!DragonSummoning.IsTransitioning,"全队进入正式神龙事件");
        Check(manager.EventSynchronizer.GetEventForPlayer(collector).CurrentOptions.Count==3&&manager.EventSynchronizer.GetEventForPlayer(ally).CurrentOptions.Single().IsLocked,"只有集齐者许愿，队友等待");
        Check(DragonBallStore.Held(collector)==7&&ally.Creature.CurrentHp>1,"进入前等待的队友休息真实结算，龙珠未提前消耗");
        await barrier("dragon-wish-ready");
        if(host)manager.EventSynchronizer.ChooseLocalOption(2);
        await wait(()=>run.CurrentRoom is RestSiteRoom&&!DragonSummoning.IsTransitioning,"全队返回营地");
        Check(collector.Gold==gold+9999&&collector.Relics.OfType<MegaCrit.Sts2.Core.Models.Relics.TheCourier>().Count()==2&&ally.Gold==allyGold,"快乐无穷仅给集齐者，已有送货员仍再获得一件");
        Check(DragonBallStore.Read(collector).WishUsed&&DragonBallStore.Held(collector)==0&&DragonBallStore.Held(ally)==0,"双端消耗全部龙珠并保存每局一次");
        Check(sync.GetOptionsForPlayer(collector).Select(o=>o.OptionId).SequenceEqual(before)&&sync.GetOptionsForPlayer(ally).Count==0,"集齐者保留行动，队友已用掉的休息不被重置");
        string dir=System.Environment.GetEnvironmentVariable("FRIEREN_MP_TEST_DIR")!;string role=host?"host":"client",peer=host?"client":"host";
        string snapshot=JsonSerializer.Serialize(run.Players.Select(p=>new{p.NetId,p.Gold,p.MaxEnergy,hp=p.Creature.CurrentHp,deck=MagicDeckStore.Read(p).ToJson(),dragon=DragonBallStore.Data.Get(p),relics=p.Relics.Select(r=>r.Id.ToString()).ToArray()}));
        File.WriteAllText(Path.Combine(dir,"dragon-"+role+".json"),snapshot);await barrier("dragon-final");
        Check(snapshot==File.ReadAllText(Path.Combine(dir,"dragon-"+peer+".json")),"真实ENet两端所有玩家数据完全一致");
        return checks;
    }
}

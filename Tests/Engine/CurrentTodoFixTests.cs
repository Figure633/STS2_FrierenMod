using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Mechanics;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;

public static class CurrentTodoFixTests
{
    private sealed class TodoSelector : MegaCrit.Sts2.Core.TestSupport.TestCardSelector, MegaCrit.Sts2.Core.TestSupport.ICardSelector
    {
        public CardModel[] Offered = [];
        async Task<IEnumerable<CardModel>> MegaCrit.Sts2.Core.TestSupport.ICardSelector.GetSelectedCards(IEnumerable<CardModel> cards, int min, int max)
        { Offered=cards.ToArray(); return await base.GetSelectedCards(Offered,min,max); }
    }
    public static async Task<int> RunNov13(Node root)
    {
        int checks=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;GD.Print("PASS 11月13日："+message);}
        SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var manager=RunManager.Instance;bool running=false;
        Player Start(){if(running)manager.CleanUp();running=true;var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var r=RunState.CreateForTest([p],seed:"TODO-01233");manager.SetUpTest(r,new NetSingleplayerGameService());CombatTestState.SetInProgress(false);return p;}
        var p=Start();var context=new ThrowingPlayerChoiceContext();
        var prizes=new HashSet<string>();bool fakeGoldCorrect=true;
        for(uint seed=0;seed<300;seed++){
            var rng=FrierenMod.Scripts.Shared.Compatibility.GameApi.CreateRng(seed);var state=new FrierenMod.Core.CommissionState();
            state.EnsureAct(0,rng.NextInt,["A","B","C"],["UnbelievableTool"],["BlockPotion"],["CommonRelic"],["A","B","C"],["BossRelic"]);
            var q=state.Quests.Single(q=>q.Stars==3);prizes.Add(q.Reward.Kind+":"+q.Reward.Item);
            if(q.Reward.Item=="FakeFlammeGrimoire")fakeGoldCorrect&=q.BaseGold==100&&q.Reward.BonusGold==0;
        }
        Check(fakeGoldCorrect,"伪作报酬仅带固定100金币");
        Check(new[]{"RareChoice:","Relic:FakeFlammeGrimoire","Prop:MagicExamVial","Prop:MagicPot","Prop:MagicBread"}.All(prizes.Contains)&&!prizes.Any(s=>s.Contains("BossRelic")),"首幕三星包含金卡、伪作和三种专属魔具，不提前出现Boss遗物");
        var old=new FrierenMod.Core.CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10001,Act=0,Stars=3,Goal=FrierenMod.Core.CommissionGoal.EmpoweredElite,Progress=1,BaseGold=100,Status=FrierenMod.Core.CommissionStatus.Accepted,Reward=new(FrierenMod.Core.CommissionRewardKind.Relic,"FakeFlammeGrimoire",200,150,DuplicateGold:200)}]};
        CommissionCmd.Data.Set(p,old.ToJson());var updated=CommissionCmd.Read(p);
        Check(updated.Quests[0].Reward.BonusGold==0,"未领取旧伪作合同取消额外150金币");
        var gold=p.Gold;
        Check(await CommissionCmd.Execute(p,10001,FrierenMod.Core.CommissionOperation.Claim,updated.Revision)&&p.Gold==gold+100&&p.GetRelic<FrierenMod.Scripts.Relics.FakeFlammeGrimoire>()!=null,"伪作合同实际领取为遗物加100金币");
        Check(!await CommissionCmd.Execute(p,10001,FrierenMod.Core.CommissionOperation.Claim,CommissionCmd.Read(p).Revision)&&p.Gold==gold+100,"重复点击不重复领取金币");
        foreach(bool up in new[]{false,true}){
            var honest=p.RunState.CreateCard<HonestLiving>(p);if(up)CardCmd.Upgrade(honest);CardCmd.Enchant<FrierenMod.Scripts.Enchantments.LoadedDice>(honest,1);
            Check(honest.Keywords.Contains(CardKeyword.Exhaust),"灌铅保留普通与升级牌的消耗："+up);
            honest.RemoveKeyword(CardKeyword.Exhaust);
            var restored=CardModel.FromSerializable(honest.ToSerializable());
            Check(restored.Enchantment is FrierenMod.Scripts.Enchantments.LoadedDice&&restored.Keywords.Contains(CardKeyword.Exhaust),"旧灌铅牌读档恢复消耗："+up);
        }
        foreach(int gift in new[]{0,1,2}){
            p=Start();p.Creature.SetMaxHpInternal(82);p.Creature.SetCurrentHpInternal(40);await PlayerCmd.GainGold(400-p.Gold,p);
            while(p.Deck.Cards.Count<30)await CardPileCmd.Add(p.RunState.CreateCard<StrikeFrieren>(p),PileType.Deck);
            var story=(FrierenMod.Scripts.Events.KraftGift)ModelDb.Event<FrierenMod.Scripts.Events.KraftGift>().ToMutable();await CombatTestState.BeginEvent(story,p);
            if(gift==2)Check(story.CurrentOptions[2].HoverTips.Any(t=>t is MegaCrit.Sts2.Core.HoverTips.CardHoverTip),"长蛇戒指选项可预览苦恼");
            await story.CurrentOptions[gift].Chosen();
            Check(story.IsFinished&&(gift switch{
                0=>p.Creature.CurrentHp==30&&p.GetRelic<MegaCrit.Sts2.Core.Models.Relics.MeatOnTheBone>()!=null,
                1=>p.Gold==300&&p.GetRelic<MegaCrit.Sts2.Core.Models.Relics.MembershipCard>()!=null,
                _=>p.Deck.Cards.OfType<MegaCrit.Sts2.Core.Models.Cards.Writhe>().Count()==1&&p.GetRelic<FrierenMod.Scripts.Relics.KraftSerpentRing>()!=null}),"礼物实际代价与奖励正确："+gift);
        }
        p=Start();var combat=new CombatState(runState:p.RunState);combat.AddPlayer(p);
        var enemies=Enumerable.Range(0,2).Select(_=>combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null)).ToArray();
        foreach(var enemy in enemies){combat.AddCreature(enemy);enemy.SetMaxHpInternal(10000);enemy.SetCurrentHpInternal(10000);}
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        foreach(bool up in new[]{false,true}){
            foreach(var power in p.Creature.Powers.ToArray())await PowerCmd.Remove(power);
            foreach(var pile in p.PlayerCombatState!.AllPiles)foreach(var card in pile.Cards.ToArray())card.RemoveFromCurrentPile();
            var flight=combat.CreateCard<FlyingMagic>(p);if(up)CardCmd.Upgrade(flight);await CardPileCmd.Add(flight,PileType.Hand);
            await flight.OnPlayWrapper(context,null,true,new ResourceInfo{EnergySpent=3,EnergyValue=3,StarsSpent=0,StarValue=0},true);
            await ManaCmd.Generate(p,PileType.Discard,4);
            await p.Creature.GetPower<FlyingMagicPower>()!.BeforeSideTurnEnd(context,CombatSide.Player,[p.Creature]);
            Check(flight.EnergyCost.Canonical==3&&p.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.IntangiblePower>()==(up?3:2)&&ManaCmd.InPile(p,PileType.Discard).Count()==2&&ManaCmd.InPile(p,PileType.Exhaust).Count()==2,"飞行魔法3费，2／3无实体，固定消耗2魔力："+up);
        }
        foreach(var power in p.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        p.Creature.LoseBlockInternal(p.Creature.Block);
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.StrengthPower>(context,p.Creature,3,p.Creature,null);
        await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.DexterityPower>(context,p.Creature,2,p.Creature,null);
        var caster=combat.CreateCard<RapidChant>(p);var play=CombatTestState.NewPlay(caster,null,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0});
        var option=FrierenMod.Core.ChantRules.Preview(FrierenMod.Core.ManaSlot.Draw,FrierenMod.Core.SpellCatalog.Get("BlackGoldWings").Rules,2);
        await FactorEffects.Apply(context,play,option);
        Check(option.Reward==8&&enemies.All(e=>e.CurrentHp==9989)&&p.Creature.Block==10,"黑金之翼4X群伤及格挡仍受到力量、敏捷影响");
        var selector=new TodoSelector();using(var selection=CombatTestState.UseSelector(selector)){
            var relic=await RelicCmd.Obtain<FrierenMod.Scripts.Relics.SacredSnowCrystal>(p);selector.PrepareToSelect([0]);
            await relic.BeforeHandDraw(p,context,combat);
            Check(selector.Offered.Length==2&&selector.Offered.All(c=>c.Rarity==CardRarity.Rare)&&selector.Offered.Select(c=>c.Id).Distinct().Count()==2&&p.PlayerCombatState!.Hand.Cards.Any(c=>c.Rarity==CardRarity.Rare&&c.Keywords.Contains(CardKeyword.Retain)),"圣雪结晶实际二选一，加入手牌并保留");
        }
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")){
            if(!InputMap.HasAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi))InputMap.AddAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi);
            var dir=ProjectSettings.GlobalizePath("res://../../artifacts/todo-current/ui");Directory.CreateDirectory(dir);
            async Task Tick(){for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
            p=Start();p.Creature.SetMaxHpInternal(82);await PlayerCmd.GainGold(400,p);while(p.Deck.Cards.Count<30)await CardPileCmd.Add(p.RunState.CreateCard<StrikeFrieren>(p),PileType.Deck);
            foreach(string lang in new[]{"zhs","eng"}){
                MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(lang);
                foreach(var canonical in new EventModel[]{ModelDb.Event<FrierenMod.Scripts.Events.SnowCrystalLand>(),ModelDb.Event<FrierenMod.Scripts.Events.KraftGift>()}){
                    var model=canonical.ToMutable();await CombatTestState.BeginEvent(model,p);
                    var panel=new Control{Size=new(1920,1080)};root.AddChild(panel);
                    var layout=model.CreateScene().Instantiate<MegaCrit.Sts2.Core.Nodes.Events.NEventLayout>();panel.AddChild(layout);model.SetNode(layout);
                    layout.SetEvent(model);layout.SetTitle(model.Title.GetFormattedText());var description=model.Description!;model.DynamicVars.AddTo(description);layout.SetDescription(description.GetFormattedText());layout.AddOptions(model.CurrentOptions);layout.OnSetupComplete();
                    await Tick();foreach(var tween in root.GetTree().GetProcessedTweens())tween.CustomStep(10);await Tick();
                    if(model is FrierenMod.Scripts.Events.SnowCrystalLand)Check(layout.GetNode<TextureRect>("%Portrait").StretchMode==TextureRect.StretchModeEnum.KeepAspectCovered,"圣雪事件原生竖幅裁切铺满左栏："+lang);
                    root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,lang+"-"+model.GetType().Name+".png"));panel.QueueFree();await Tick();
                }
                var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("20252f")});
                var flight=p.RunState.CreateCard<FlyingMagic>(p);var upgraded=p.RunState.CreateCard<FlyingMagic>(p);CardCmd.Upgrade(upgraded);
                var wing=FactorCard.Create(p,"BlackGoldWings");var cards=new CardModel[]{flight,upgraded,wing};
                for(int i=0;i<cards.Length;i++){var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();AccessTools.Property(node.GetType(),"Model").SetValue(node,cards[i]);canvas.AddChild(node);node.Position=new(420+i*530,450);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);}
                await Tick();root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,lang+"-cards.png"));canvas.QueueFree();await Tick();
            }
        }
        manager.CleanUp();MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("eng");return checks;
    }
    public static async Task<int> RunLateNov12(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 最新待办："+text);}
        SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var run=RunState.CreateForTest([player],seed:"TODO-01229");
        var manager=RunManager.Instance;manager.SetUpTest(run,new NetSingleplayerGameService());CombatTestState.SetInProgress(false);
        run.PushRoom(new MegaCrit.Sts2.Core.Rooms.RestSiteRoom());manager.ActionExecutor.Unpause();
        var density=run.CreateCard<HighDensityMana>(player);await CardPileCmd.Add(density,PileType.Deck);
        var swift=ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.Swift>();
        Check(swift.CanEnchant(density)&&ModelDb.Enchantment<FrierenMod.Scripts.Enchantments.ManaSource>().CanEnchant(density),"高密度魔力可用于原生与模组通用附魔");
        Check(!ModelDb.Enchantment<MegaCrit.Sts2.Core.Models.Enchantments.Sharp>().CanEnchant(density),"攻击牌专属附魔仍不能附在高密度魔力上");
        Check(!swift.CanEnchant(run.CreateCard<Mana>(player))&&!swift.CanEnchant(run.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Dazed>(player)),"其他状态牌仍保留原生限制");
        CardCmd.Enchant<MegaCrit.Sts2.Core.Models.Enchantments.Swift>(density,1);
        Check(!ModelDb.Enchantment<FrierenMod.Scripts.Enchantments.ManaSource>().CanEnchant(density)&&Player.FromSerializable(player.ToSerializable()).Deck.Cards.OfType<HighDensityMana>().Single().Enchantment is MegaCrit.Sts2.Core.Models.Enchantments.Swift,"附魔真实存档恢复，不能覆盖不同附魔");
        bool foundFake=false;bool foundTrue=false;
        for(uint seed=0;seed<100;seed++) {
            var random=FrierenMod.Scripts.Shared.Compatibility.GameApi.CreateRng(seed);var generated=new FrierenMod.Core.CommissionState();
            generated.EnsureAct(0,random.NextInt,["A","B","C"],["UnbelievableTool"],["BlockPotion"],["CommonRelic"],["A","B","C"],["BossRelic"]);
            foundFake|=generated.Quests.Any(q=>q.Reward.Item=="FakeFlammeGrimoire");foundTrue|=generated.Quests.Any(q=>q.Reward.Item=="FlammeGrimoire");
        }
        Check(foundFake&&!foundTrue,"首幕委托生成伪作，不再生成真作");
        var old=new FrierenMod.Core.CommissionState{CurrentAct=0,GeneratedActs=[0],Quests=[new(){Id=10000,Act=0,Stars=3,Goal=FrierenMod.Core.CommissionGoal.Elites,Progress=1,BaseGold=100,Status=FrierenMod.Core.CommissionStatus.Accepted,Reward=new(FrierenMod.Core.CommissionRewardKind.Relic,"FlammeGrimoire",200,150,DuplicateGold:200)}]};
        CommissionCmd.Data.Set(player,old.ToJson());var migrated=CommissionCmd.Read(player);
        Check(migrated.Quests.Single().Reward.Item=="FakeFlammeGrimoire"&&CommissionCmd.Data.Get(player).Contains("FakeFlammeGrimoire"),"旧存档未领奖的真作合同迁移为伪作并保存");
        int gold=player.Gold;Check(await CommissionCmd.Execute(player,10000,FrierenMod.Core.CommissionOperation.Claim,migrated.Revision),"迁移后的旧委托仍可原生领奖");
        Check(player.GetRelic<FrierenMod.Scripts.Relics.FakeFlammeGrimoire>()!=null&&player.GetRelic<FrierenMod.Scripts.Relics.FlammeGrimoire>()==null&&player.Gold==gold+100,"旧未领取委托改为伪作加固定100金币，不再额外150");
        Check(FrierenMod.Scripts.Encounters.MimicEncounter.Rewards(player).OfType<MegaCrit.Sts2.Core.Rewards.RelicReward>().Any(r=>r.Relic is FrierenMod.Scripts.Relics.FlammeGrimoire),"持有伪作时宝箱怪仍奖励真作");
        old.Quests[0].Status=FrierenMod.Core.CommissionStatus.Claimed;CommissionCmd.Data.Set(player,old.ToJson());
        Check(CommissionCmd.Read(player).Quests.Single().Reward.Item=="FlammeGrimoire","已领奖的历史合同不改写");
        var normal=run.CreateCard<RapidChant>(player);var precise=run.CreateCard<PreciseChant>(player);
        foreach(var slot in Enum.GetValues<FrierenMod.Core.ManaSlot>()) {
            var option=new FrierenMod.Core.ChantOption(slot,"BuildUpBody",1,1,1);
            Check(FactorCard.Create(player,"BuildUpBody",slot,option,chantSource:normal).PortraitPath.EndsWith("BuildUpBody.png"),"普通咏唱使用因子图："+slot);
            Check(FactorCard.Create(player,"BuildUpBody",slot,option,chantSource:precise).PortraitPath.EndsWith(ChantChoice.ArtFor(slot)+".png"),"精准咏唱使用牌堆图："+slot);
        }
        var point=new MegaCrit.Sts2.Core.Nodes.Screens.Map.NAncientMapPoint();var icon=new TextureRect{Name="Icon",Texture=ModelDb.AncientEvent<FrierenMod.Scripts.Ancients.Serie>().MapIcon};point.AddChild(icon);
        var type=point.GetType();var color=(Color)AccessTools.PropertyGetter(type,"UntravelableColor").Invoke(point,null)!;
        Check(color==MegaCrit.Sts2.Core.Helpers.StsColors.bossNodeUntraveled,"赛莉耶简笔图标沿用原生未访问配色");
        AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint),"_state").SetValue(point,MegaCrit.Sts2.Core.Map.MapPointState.Travelable);
        color=(Color)AccessTools.PropertyGetter(type,"TraveledColor").Invoke(point,null)!;Check(color==MegaCrit.Sts2.Core.Helpers.StsColors.pathDotTraveled,"可以进入的简笔图标沿用原生配色");
        AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint),"_state").SetValue(point,MegaCrit.Sts2.Core.Map.MapPointState.Traveled);
        color=(Color)AccessTools.PropertyGetter(type,"TraveledColor").Invoke(point,null)!;Check(color==MegaCrit.Sts2.Core.Helpers.StsColors.pathDotTraveled,"已访问简笔图标沿用原生配色");
        icon.Texture=ModelDb.AncientEvent<MegaCrit.Sts2.Core.Models.Events.Neow>().MapIcon;color=(Color)AccessTools.PropertyGetter(type,"UntravelableColor").Invoke(point,null)!;
        Check(color.R<.99f||color.G<.99f||color.B<.99f,"其他先古地图颜色不变");point.Free();
        Check(new MegaCrit.Sts2.Core.Localization.LocString("events","FRIERENMOD-DRAGON_BALL_ADVENTURE.title").GetFormattedText()=="Adventure Story: Dragon Ball","七龙珠事件英文标题同步");
        manager.CleanUp();return checks;
    }
    private static async Task Nov12Visuals(Node root,Player player)
    {
        var dir=ProjectSettings.GlobalizePath("res://../../artifacts/todo-nov12/ui");Directory.CreateDirectory(dir);
        async Task Tick(){for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);}
        for(int n=0;n<7;n++)DragonBallStore.GrantNext(player,true);
        var state=DragonBallStore.Read(player);state.Summoning=true;DragonBallStore.Write(player,state);
        if(!InputMap.HasAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi))InputMap.AddAction(MegaCrit.Sts2.Core.Debug.DebugHotkey.hideEventUi);
        foreach(string lang in new[]{"zhs","eng"}){
            MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(lang);
            foreach(var canonical in new EventModel[]{ModelDb.Event<FrierenMod.Scripts.Events.DemonArmyAttack>(),ModelDb.Event<FrierenMod.Scripts.Events.FoodEvent>(),ModelDb.Event<FrierenMod.Scripts.Events.HollowKnightAdventure>(),ModelDb.Event<FrierenMod.Scripts.Events.SummonDragon>()}){
                var model=canonical.ToMutable();await CombatTestState.BeginEvent(model,player);
                var panel=new Control{Size=new(1920,1080)};root.AddChild(panel);
                var layout=model.CreateScene().Instantiate<MegaCrit.Sts2.Core.Nodes.Events.NEventLayout>();panel.AddChild(layout);model.SetNode(layout);
                layout.SetEvent(model);layout.SetTitle(model.Title.GetFormattedText());var text=model.Description!;model.DynamicVars.AddTo(text);layout.SetDescription(text.GetFormattedText());layout.AddOptions(model.CurrentOptions);layout.OnSetupComplete();
                await Tick();foreach(var tween in root.GetTree().GetProcessedTweens())tween.CustomStep(10);await Tick();
                root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,lang+"-"+model.GetType().Name+".png"));panel.QueueFree();await Tick();
            }
            var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("20252f")});
            var normal=player.RunState.CreateCard<RustCleanMagic>(player);var upgrade=player.RunState.CreateCard<RustCleanMagic>(player);CardCmd.Upgrade(upgrade);
            var mana=player.RunState.CreateCard<Mana>(player);CardCmd.Enchant<FrierenMod.Scripts.Enchantments.ManaSource>(mana,2);
            var kame=player.RunState.CreateCard<Kamehameha>(player);CardCmd.Upgrade(kame);
            var cards=new CardModel[]{normal,upgrade,mana,kame};
            for(int i=0;i<cards.Length;i++){
                var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();
                AccessTools.Property(node.GetType(),"Model").SetValue(node,cards[i]);canvas.AddChild(node);node.Position=new(280+i*430,400);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);
            }
            var relics=new RelicModel[]{ModelDb.Relic<FrierenMod.Scripts.Relics.SacredSnowCrystal>(),ModelDb.Relic<FrierenMod.Scripts.Relics.Skycleaver>(),ModelDb.Relic<FrierenMod.Scripts.Relics.WeatheredHelmet>(),ModelDb.Relic<FrierenMod.Scripts.Relics.RottingSageSkull>(),ModelDb.Relic<FrierenMod.Scripts.Relics.Matryoshka>()};
            for(int i=0;i<relics.Length;i++){
                var node=GD.Load<PackedScene>("res://scenes/relics/relic.tscn").Instantiate<MegaCrit.Sts2.Core.Nodes.Relics.NRelic>();node.Model=relics[i];canvas.AddChild(node);node.Position=new(240+i*350,880);node.Scale=new(2,2);
            }
            await Tick();root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,lang+"-cards-relics.png"));canvas.QueueFree();await Tick();
        }
    }
    public static async Task<int> RunNov12(Node root)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 11月12日："+text);}
        var manager=RunManager.Instance;SaveManager.Instance.InitProfileId(1);SaveManager.Instance.InitPrefsDataForTest();LocalContext.NetId=1;
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO-01224");manager.SetUpTest(run,new NetSingleplayerGameService());manager.GenerateRooms();
        var visits=SaveManager.Instance.Progress.GetOrCreateAncientStats(ModelDb.AncientEvent<FrierenMod.Scripts.Ancients.Serie>().Id);
        visits.CharStats.Clear();
        await FrierenMod.Scripts.Ancients.SerieFirstVisit.Ensure(run,2);
        Check(run.Acts[2].Ancient is FrierenMod.Scripts.Ancients.Serie && FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(player)==2,"从未遇见赛莉耶时首次第三幕必遇，决策进入存档");
        Check(FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(Player.FromSerializable(player.ToSerializable()))==2,"首次选择记录真实存档恢复");
        visits.CharStats.Add(new AncientCharacterStats{Character=player.Character.Id,Wins=1});
        manager.CleanUp();player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);run=RunState.CreateForTest([player],seed:"TODO-01224-NEXT");manager.SetUpTest(run,new NetSingleplayerGameService());manager.GenerateRooms();
        var rolled=run.Acts[2].Ancient;await FrierenMod.Scripts.Ancients.SerieFirstVisit.Ensure(run,2);
        Check(ReferenceEquals(rolled,run.Acts[2].Ancient)&&FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(player)==1,"再访保留原生随机结果");
        visits.CharStats.Clear();FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Set(player,0);
        run.CurrentActIndex=2;run.AddVisitedMapCoord(new MegaCrit.Sts2.Core.Map.MapCoord(0,0));
        await FrierenMod.Scripts.Ancients.SerieFirstVisit.Ensure(run,2);
        Check(ReferenceEquals(rolled,run.Acts[2].Ancient)&&FrierenMod.Scripts.Ancients.SerieFirstVisit.Decision.Get(player)==1,"升级前已走过先古的第三幕旧存档不被强制改写");
        run.CurrentActIndex=0;run.ClearVisitedMapCoordsDebug();
        var selector=new MegaCrit.Sts2.Core.TestSupport.TestCardSelector();using var selected=CombatTestState.UseSelector(selector);
        selector.PrepareToSelect([0,1,2]);await RelicCmd.Obtain<FrierenMod.Scripts.Relics.RottingSageSkull>(player);
        Check(player.Deck.Cards.Count(c=>c.Enchantment is FrierenMod.Scripts.Enchantments.ManaSource {Amount:3})==3,"贤者之颅原生选三张牌附魔，存档保存附魔类型和数量");
        Check(Player.FromSerializable(player.ToSerializable()).Deck.Cards.Count(c=>c.Enchantment is FrierenMod.Scripts.Enchantments.ManaSource {Amount:3})==3,"魔力源3存读档完整");
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);var context=new ThrowingPlayerChoiceContext();
        CardPlay Play(CardModel card)=>CombatTestState.NewPlay(card,null,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0});
        var printed=combat.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Corruption>(player);
        printed.EnergyCost.SetThisTurn(0);
        Check(FrierenMod.Scripts.Relics.WeatheredHelmet.PrintedEnergyCost(printed)==3,"临时零费仍按百科3费判定");
        CardCmd.Upgrade(printed);
        Check(FrierenMod.Scripts.Relics.WeatheredHelmet.PrintedEnergyCost(printed)==2,"升级降为2费后不再满足头盔条件");
        var helmet=await RelicCmd.Obtain<FrierenMod.Scripts.Relics.WeatheredHelmet>(player);
        var expensive=combat.CreateCard<Kamehameha>(player);expensive.EnergyCost.SetThisTurn(0);await helmet.AfterCardPlayed(context,Play(expensive));
        Check(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.BufferPower>()==1,"原本5费免费打出仍获得原生缓冲");
        await helmet.AfterCardPlayed(context,Play(printed));
        var x=combat.CreateCard<MultipleOffensiveMagic>(player);await helmet.AfterCardPlayed(context,Play(x));
        Check(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.BufferPower>()==1,"升级2费和X费不按当前能量误触发");
        await PowerCmd.Remove(player.Creature.GetPower<MegaCrit.Sts2.Core.Models.Powers.BufferPower>()!);
        await RelicCmd.Obtain<FrierenMod.Scripts.Relics.Skycleaver>(player);
        await CreatureCmd.GainBlock(player.Creature,5,MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered,null);
        decimal hp=player.Creature.CurrentHp;
        await FrierenMod.Scripts.Shared.Compatibility.GameApi.Damage(context,player.Creature,3,MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,enemy,null);
        Check(player.Creature.CurrentHp==hp&&player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.BlockNextTurnPower>()==2,"完全格挡伤害也触发开天斧次回合格挡");
        await FrierenMod.Scripts.Shared.Compatibility.GameApi.Damage(context,player.Creature,4,MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,enemy,null);
        Check(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.BlockNextTurnPower>()==4,"部分格挡的第二次伤害累计2格挡");
        await FrierenMod.Scripts.Shared.Compatibility.GameApi.Damage(context,player.Creature,1,MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,enemy,null);
        Check(player.Creature.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.BlockNextTurnPower>()==4,"没有格挡的伤害不触发开天斧");
        foreach(var type in new[]{PileType.Hand,PileType.Draw,PileType.Discard,PileType.Play,PileType.Exhaust})foreach(var c in CardPile.Get(type,player)!.Cards.ToArray())c.RemoveFromCurrentPile();
        for(int upgraded=0;upgraded<2;upgraded++){
            var original=combat.CreateCard<StrikeFrieren>(player);CardCmd.Enchant<FrierenMod.Scripts.Enchantments.HamonOverdrive>(original,1);await CardPileCmd.Add(original,PileType.Draw);
            var rust=combat.CreateCard<RustCleanMagic>(player);if(upgraded==1)CardCmd.Upgrade(rust);await CardPileCmd.Add(rust,PileType.Hand);
            await rust.OnPlayWrapper(context,null,true,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);
            var mana=player.PlayerCombatState!.DrawPile.Cards.OfType<Mana>().Single();
            Check(mana.Enchantment is FrierenMod.Scripts.Enchantments.ManaSource source && source.Amount==upgraded+1 && original.Pile==null,"原生变化替换抽牌堆卡牌并丢弃旧附魔："+upgraded);
            int before=player.PlayerCombatState.Hand.Cards.OfType<Mana>().Count();
            await mana.Enchantment!.OnPlay(context,Play(mana));await mana.Enchantment.OnPlay(context,Play(mana));
            Check(player.PlayerCombatState.Hand.Cards.OfType<Mana>().Count()==before+upgraded+1,"每张魔力源仅第一次打出生成魔力："+upgraded);
            mana.RemoveFromCurrentPile();
        }
        var snow=await RelicCmd.Obtain<FrierenMod.Scripts.Relics.SacredSnowCrystal>(player);
        selector.PrepareToSelect([0]);
        await snow.BeforeHandDraw(player,context,combat);
        Check(player.PlayerCombatState!.Hand.Cards.Any(c=>c.Rarity==CardRarity.Rare&&c.Keywords.Contains(CardKeyword.Retain)),"圣雪结晶原生三选一生成稀有牌并赋予保留");
        int handCount=player.PlayerCombatState.Hand.Cards.Count;player.PlayerCombatState.IncrementTurnNumber();await snow.BeforeHandDraw(player,context,combat);
        Check(player.PlayerCombatState.Hand.Cards.Count==handCount,"圣雪结晶只在战斗第一回合提供选择");
        foreach(string lang in new[]{"zhs","eng"}){
            MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(lang);
            foreach(var relic in new RelicModel[]{snow,helmet,ModelDb.Relic<FrierenMod.Scripts.Relics.Skycleaver>(),ModelDb.Relic<FrierenMod.Scripts.Relics.RottingSageSkull>()})
                Check(!relic.DynamicDescription.GetFormattedText().Contains("LOC_ERR"),"新遗物动态描述与图标："+lang+relic.Id);
        }
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))await Nov12Visuals(root,player);
        manager.CleanUp();MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("eng");return count;
    }
    public static async Task<int> Run()
    {
        int count = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; GD.Print("PASS " + name); }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "TODO-083");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);
        CombatTestState.SetInProgress(true);
        var context = new ThrowingPlayerChoiceContext();
        var hellfire = combat.CreateCard<HellFireSummoning>(player);
        await CardPileCmd.Add(hellfire, PileType.Draw); await hellfire.BeforeCombatStart();
        var seal = player.Creature.GetPower<SealPower>()!;
        int updates = 0; seal.DisplayAmountChanged += () => updates++;
        Check(seal.Cards.Count == 1 && seal.DisplayAmount == 0, "封印初始显示零次咏唱，不显示封印卡数");
        CardPlay MakePlay(CardModel card) => CombatTestState.NewPlay(card: card, target: null, isAutoPlay: false, resultPile: PileType.Discard, resources: new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, playCount: 1, playIndex: 0);
        var ordinary = MakePlay(combat.CreateCard<StrikeFrieren>(player));
        CombatManager.Instance.History.CardPlayStarted(combat, ordinary);
        Check(seal.DisplayAmount == 0, "普通牌不增加咏唱计数");
        for (int i = 1; i <= 2; i++) {
            var play = MakePlay(combat.CreateCard<RapidChant>(player));
            CombatManager.Instance.History.CardPlayStarted(combat, play); await seal.AfterChant(context, play);
            Check(seal.DisplayAmount == i && updates == i, "第" + i + "张咏唱即时更新图标");
        }
        player.PlayerCombatState!.IncrementTurnNumber();
        await seal.AfterPlayerTurnStart(context, player);
        Check(seal.DisplayAmount == 0 && updates == 3, "换回合归零并通知图标刷新");
        for (int i = 1; i <= 5; i++) {
            var play = MakePlay(combat.CreateCard<RapidChant>(player));
            CombatManager.Instance.History.CardPlayStarted(combat, play); await seal.AfterChant(context, play);
        }
        Check(!player.Creature.HasPower<SealPower>() && hellfire.Pile?.Type == PileType.Hand, "单回合五次后原有解封入手正常");
        bool original = PresentationSettings.OriginalMusic;
        try {
            PresentationSettings.OriginalMusic = true;
            Check(FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/ambience/act1_neow"), "一代音乐开启时抑制涅奥独立主题循环");
            Check(!FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/npcs/neow/neow_welcome"), "涅奥台词和其他音效保留");
            PresentationSettings.OriginalMusic = false;
            Check(!FrierenMusic.SuppressOpeningTheme(run, "event:/sfx/ambience/act1_neow"), "关闭音乐替换保留原生主题");
            PresentationSettings.OriginalMusic = true;
            var other = RunState.CreateForTest([Player.CreateForNewRun<Ironclad>(UnlockState.all, 1)], seed: "OTHER");
            Check(!FrierenMusic.SuppressOpeningTheme(other, "event:/sfx/ambience/act1_neow"), "其他角色不改变开局音乐");
        } finally { PresentationSettings.OriginalMusic = original; }
        return count;
    }
}

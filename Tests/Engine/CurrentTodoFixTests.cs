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

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Relics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class Author0120Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;GD.Print("PASS 作者改稿："+message);}
        SaveManager.Instance.InitPrefsDataForTest();
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"AUTHOR0120");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var room=new CombatRoom(ModelDb.Encounter<TurretOperatorWeak>().ToMutable(),run);run.PushRoom(room);
        var combat=room.CombatState;combat.AddPlayer(player);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CombatTestState.UseSelector(selector);
        T Card<T>(bool upgrade=false)where T:CardModel{var c=combat.CreateCard<T>(player);if(upgrade)CardCmd.Upgrade(c);return c;}
        CardPlay Play(CardModel card)=>CombatTestState.NewPlay(card,enemy,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0});
        async Task Use(CardModel card){await CardPileCmd.Add(card,PileType.Hand);await card.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);}
        var deck=new MagicDeckState();for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);}
        MagicDeckStore.Write(player,deck);MagicDeckStore.Write(other,MagicDeckState.FromJson(deck.ToJson()));
        await Use(Card<EnchantedStrike>(true));Check(enemy.CurrentHp==9990,"附魔打击基础9伤，升级保留每张魔力加4");
        Check(Card<PaperPlaneSpell>().EnergyCost.Canonical==3&&Card<PaperPlaneSpell>(true).EnergyCost.GetWithModifiers(CostModifiers.Local)==3,"纸飞机普通升级均3费");
        var source=Card<RapidChant>();
        int hp=enemy.CurrentHp;await FactorEffects.Apply(context,Play(source),ChantRules.Preview(ManaSlot.Draw,SpellCatalog.Get("BlackGoldWings").Rules,2));
        Check(enemy.CurrentHp==hp-12&&player.Creature.Block==12,"黑金之翼咏唱2给予12群伤12格挡");
        await FactorEffects.Apply(context,Play(source),ChantRules.Preview(ManaSlot.Draw,SpellCatalog.Get("GrossedOutLook").Rules,2));
        Check(enemy.GetPowerAmount<StrengthPower>()==-3,"嫌弃脸X+1减力量");
        await enemy.GetPower<RestoreStrengthPower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,[enemy]);
        Check(enemy.GetPowerAmount<StrengthPower>()==0,"嫌弃脸回合结束精确归还力量");
        foreach(var item in new (int cost,string kind,int amount)[]{(0,"Damage",6),(1,"Damage",12),(2,"Damage",18),(2,"DamageQuad",4),(3,"DamageFive",4),(3,"Block",22)})
            Check(LegendaryCraft.Options(item.cost,1).Contains(new(item.kind,item.amount)),"自定义数值："+item);
        Check(!LegendaryCraft.Options(2,0).Any(e=>e.Kind=="ManaDiscard")&&!LegendaryCraft.Options(3,0).Any(e=>e.Kind=="ManaDiscard"),"月亮不进入2／3费池");
        var custom=Card<CustomLegendarySpell>();custom.Configure(3,[new("DamageSeven",4)]);hp=enemy.CurrentHp;await Use(custom);
        Check(enemy.CurrentHp==hp-20&&custom.Type==CardType.Attack,"恶魔旧存档3费配方兼容为4伤5次");
        custom=Card<CustomLegendarySpell>();custom.Configure(2,[new("RetainAll",2)]);await Use(custom);
        var retain=player.Creature.GetPower<RetainHandPower>()!;
        Check(retain.Amount==2&&!retain.ShouldFlush(player)&&retain.ShouldFlush(other),"节制保留全部手牌，仅作用于本人");
        await retain.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);Check(retain.Amount==1,"两回合保留正确递减");
        await retain.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);Check(!player.Creature.HasPower<RetainHandPower>(),"第二回合后移除保留");
        await PowerCmd.Apply<StrengthPower>(context,player.Creature,3,player.Creature,null);
        await PowerCmd.Apply<DexterityPower>(context,player.Creature,2,player.Creature,null);
        custom=Card<CustomLegendarySpell>();custom.Configure(3,[new("DoubleStats",1)]);await Use(custom);
        Check(player.Creature.GetPowerAmount<StrengthPower>()==6&&player.Creature.GetPowerAmount<DexterityPower>()==4&&custom.Keywords.Contains(CardKeyword.Exhaust),"女皇翻倍力量敏捷并消耗");
        custom=Card<CustomLegendarySpell>();custom.Configure(1,[new("ReduceStrength",2)]);await Use(custom);
        Check(enemy.GetPowerAmount<StrengthPower>()==-2&&custom.Keywords.Contains(CardKeyword.Exhaust),"教皇永久减敌方力量并消耗");
        var slayer=await PowerCmd.Apply<FrierenTheSlayerPower>(context,player.Creature,1,player.Creature,null);
        selector.PrepareToSelect(new[]{0});await slayer!.AfterPlayerTurnStart(context,player);
        selector.PrepareToSelect(new[]{0});await slayer.AfterPlayerTurnStart(context,player);
        Check(slayer.ChosenSpellIds.Count==2,"先古牌两次选择不会重复");
        var second=await PowerCmd.Apply<FrierenTheSlayerPower>(context,other.Creature,1,other.Creature,null);
        Check(second!.ChosenSpellIds.Count==0,"不同玩家的已选魔法独立");
        foreach(var model in new CardModel[]{Card<OrdinaryOffensiveMagic>(),Card<BloodSpell>(),Card<MetalPetalSpell>(),Card<IceArrows>(),Card<HighSpeedMovement>(),Card<BlackGoldWings>()})
            Check(SpellReadings.Reading(model).Length>0,"作者提供读音："+model.GetType().Name);
        Check(SealingOreMine.ManaForRemoved(Card<PaperPlaneSpell>())==3&&SealingOreMine.ManaForRemoved(Card<MultipleOffensiveMagic>())==0&&SealingOreMine.ManaForRemoved(Card<UpsideDown>())==0,"矿山按费用转换，排除X费与无法打出牌");
        deck=MagicDeckStore.Read(player);int ore=deck.AddProp("SealingOre").InstanceId;MagicDeckStore.Write(player,deck);
        await SealingOreCmd.Refresh(player);await ManaCmd.Generate(player,PileType.Draw,10);
        Check(player.Creature.HasPower<SealingOrePower>()&&ChantCmd.Options(player,1).Count==0,"持矿获得减益并阻止咏唱");
        Check(ChantCmd.Options(other,1).Count==0&&!SealingOreCmd.BlocksChant(other),"矿石不影响其他玩家库存");
        deck=MagicDeckStore.Read(player);deck.Remove(ore);MagicDeckStore.Write(player,deck);await SealingOreCmd.Refresh(player);
        Check(!player.Creature.HasPower<SealingOrePower>()&&ChantCmd.Options(player,1).Count>0,"丢弃矿石解除禁咏唱");
        CombatTestState.SetInProgress(false);
        deck=MagicDeckStore.Read(player);deck.AddProp("SealingOre");MagicDeckStore.Write(player,deck);int gold=player.Gold;
        await SealingOreCmd.Visit(player,new MerchantRoom());await SealingOreCmd.Visit(player,new MerchantRoom());
        Check(player.Gold==gold+300&&!SealingOreCmd.HasOre(player),"商店自动售出300金币，重复通知不重复付款");
        deck=MagicDeckStore.Read(player);deck.AddProp("SealingOre");MagicDeckStore.Write(player,deck);gold=player.Gold;
        await SealingOreCmd.Visit(player,new EventRoom(ModelDb.Event<MegaCrit.Sts2.Core.Models.Events.FakeMerchant>()));
        Check(player.Gold==gold+300&&!SealingOreCmd.HasOre(player),"假商人同样买矿石");
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        var food=(FoodEvent)ModelDb.Event<FoodEvent>().ToMutable();await CombatTestState.BeginEvent(food,player);
        player.Creature.SetCurrentHpInternal(1);gold=player.Gold;await food.CurrentOptions[2].Chosen();
        Check(player.GetRelic<MercuryPudding>()!=null&&player.Gold==gold-150&&player.Creature.CurrentHp==1+(int)Math.Floor(player.Creature.MaxHp*.5),"150金币水星布丁加50%治疗");
        food=(FoodEvent)ModelDb.Event<FoodEvent>().ToMutable();await CombatTestState.BeginEvent(food,player);
        food.DynamicVars.AddTo(food.CurrentOptions[2].Description);
        Check(food.CurrentOptions[2].Description.GetFormattedText().Contains("草莓")&&!food.CurrentOptions[2].Description.GetFormattedText().Contains("布丁"),"已持有布丁只显示草莓奖励");
        player.Creature.SetCurrentHpInternal(1);await food.CurrentOptions[3].Chosen();
        Check(player.GetRelic<Mango>()!=null&&player.Creature.CurrentHp==player.Creature.MaxHp,"250金币获得芒果后回复至新上限");
        var removed=run.CreateCard<PaperPlaneSpell>(player);await CardPileCmd.Add(removed,PileType.Deck);
        var mining=(SealingOreMine)ModelDb.Event<SealingOreMine>().ToMutable();await CombatTestState.BeginEvent(mining,player);
        int manaBefore=player.Deck.Cards.OfType<Mana>().Count();selector.PrepareToSelect(new[]{player.Deck.Cards.ToList().IndexOf(removed)});
        await mining.CurrentOptions[0].Chosen();
        Check(!player.Deck.Cards.Contains(removed)&&player.Deck.Cards.OfType<Mana>().Count()==manaBefore+3,"矿山实际移除3费牌并将3张魔力加入主牌组");
        mining=(SealingOreMine)ModelDb.Event<SealingOreMine>().ToMutable();await CombatTestState.BeginEvent(mining,player);
        await mining.CurrentOptions[1].Chosen();
        Check(SealingOreCmd.HasOre(player)&&!player.Deck.Cards.OfType<SealingOre>().Any(),"矿山获得封魔矿只进入魔法牌组");
        foreach(var language in new[]{"zhs","eng"}){
            MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage(language);
            var mine=(SealingOreMine)ModelDb.Event<SealingOreMine>().ToMutable();await CombatTestState.BeginEvent(mine,player);
            Check(mine.CurrentOptions.All(o=>!o.Description.GetFormattedText().Contains("LOC_ERR")),language+"矿山选项完整");
            Check(mine.CurrentOptions[1].HoverTips.OfType<MegaCrit.Sts2.Core.HoverTips.CardHoverTip>().Any(),language+"封魔矿选择前原生卡面预览");
            foreach(var effect in new LegendaryEffect[]{new("RetainAll",2),new("DamageQuad",4),new("DamageSeven",4),new("DoubleStats",1),new("ReduceStrength",4)}){
                custom=Card<CustomLegendarySpell>();custom.Configure(2,[effect]);
                Check(!custom.GetDescriptionForPile(PileType.None).Contains("LOC_ERR"),language+"新自定义选项文本："+effect.Kind);
            }
        }
        if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))await Snapshot(root,player,Check);
        RunManager.Instance.CleanUp();
        foreach(int ascension in new[]{0,9}){
            var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var r=RunState.CreateForTest([p],ascensionLevel:ascension,seed:"AURA0120");
            RunManager.Instance.SetUpTest(r,new NetSingleplayerGameService());
            var state=new CombatState(runState:r);state.AddPlayer(p);
            var aura=(Aura)ModelDb.Monster<Aura>().ToMutable();var boss=state.CreateCreature(aura,CombatSide.Enemy,null);state.AddCreature(boss);
            var soldier=(AuraSoldier)ModelDb.Monster<AuraSoldier>().ToMutable();
            CombatManager.Instance.SetUpCombat(state);CombatTestState.SetInProgress(true);
            Check(aura.BarrageDamage==(ascension==0?6:7)&&aura.GuillotineDamage==(ascension==0?25:28)&&aura.SubmissionEnergy==(ascension==0?1:2),"阿乌拉进阶伤害与减能量："+ascension);
            Check(soldier.AttackDamage==(ascension==0?8:10)&&soldier.GuardAmount==(ascension==0?10:12),"士兵进阶数值："+ascension);
            aura.RollMove([p.Creature]);await aura.AfterAddedToRoom();aura.RefreshOrdinaryIntent();await aura.PerformMove();
            Check(aura.Mana==200&&state.Enemies.Count(c=>c.Monster is AuraSoldier)==2,"召唤两士兵不消耗魔力："+ascension);
            var submission=await PowerCmd.Apply<AuraSubmissionPower>(context,p.Creature,aura.SubmissionEnergy,boss,null);p.PlayerCombatState!.Energy=3;
            await submission!.AfterEnergyReset(p);
            Check(p.PlayerCombatState.Energy==3-aura.SubmissionEnergy&&!p.Creature.HasPower<AuraSubmissionPower>(),"服从只在本人重置能量后扣除一次："+ascension);
            CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();
        }
        return count;
    }

    private static async Task Snapshot(Node root, Player player, Action<bool,string> check)
    {
        MegaCrit.Sts2.Core.Localization.LocManager.Instance.SetLanguage("zhs");
        using var atlas=new MegaCrit.Sts2.Core.Assets.AtlasResourceLoader();ResourceLoader.AddResourceFormatLoader(atlas,true);
        var canvas=new Control();root.AddChild(canvas);
        canvas.AddChild(new ColorRect{Size=new Vector2(1920,1080),Color=new Color("17202c")});
        var cards=new CardModel[]{ModelDb.Card<OrdinaryOffensiveMagic>(),ModelDb.Card<BloodSpell>(),ModelDb.Card<MetalPetalSpell>(),ModelDb.Card<IceArrows>(),ModelDb.Card<HighSpeedMovement>(),ModelDb.Card<BlackGoldWings>(),ModelDb.Card<SealingOre>(),ModelDb.Card<HellFireSummoning>()};
        var scene=GD.Load<PackedScene>("res://scenes/cards/card.tscn");var nodes=new List<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();
        for(int i=0;i<cards.Length;i++){
            var card=(CardModel)cards[i].ToMutable();card.Owner=player;
            var node=scene.Instantiate<MegaCrit.Sts2.Core.Nodes.Cards.NCard>();
            HarmonyLib.AccessTools.Property(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard),"Model").SetValue(node,card);
            canvas.AddChild(node);node.Position=new Vector2(270+i%4*455,280+i/4*520);
            node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);
            if(SpellReadings.Reading(card).Length>0)check(node.FindChildren("FrierenSpellReading","Label",true,false).Count==1,"卡面显示独立读音："+card.GetType().Name);
            if(card is BlackGoldWings or SealingOre)check(node.GetNode<TextureRect>("%Portrait").Texture.GetSize()==new Vector2(500,380),"新增插画原生画框比例："+card.GetType().Name);
        }
        for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        foreach(var node in nodes){var label=node.GetNode<RichTextLabel>("%DescriptionLabel");check(label.GetContentHeight()<=label.Size.Y+2,"卡面正文无溢出："+node.Model!.GetType().Name);}
        root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/author-review-20261106/native-cards.png"));
        canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
        ResourceLoader.RemoveResourceFormatLoader(atlas);
    }
}

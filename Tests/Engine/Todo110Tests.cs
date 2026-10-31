using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Relics;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using System.Text.Json;
using HarmonyLib;

public static class Todo110Tests
{
    private static bool SkipTalk() => false;
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string text) { if(!ok)throw new Exception(text); count++; GD.Print("PASS 110: "+text); }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"TODO110");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(1000);enemy.SetCurrentHpInternal(1000);
        var survivor=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(survivor);
        survivor.SetMaxHpInternal(1000);survivor.SetCurrentHpInternal(1000);
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        T New<T>(bool up=false)where T:CardModel { var c=combat.CreateCard<T>(player);if(up)CardCmd.Upgrade(c);return c; }
        ResourceInfo Resources()=>new(){EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0};
        async Task Use(CardModel c) {await CardPileCmd.Add(c,PileType.Hand);await c.OnPlayWrapper(context,enemy,false,Resources(),true);}
        async Task Reset() {
            if (!combat.Creatures.Contains(enemy)) {enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(1000);}
            foreach(var creature in combat.Creatures) {foreach(var power in creature.Powers.ToArray())await PowerCmd.Remove(power);creature.LoseBlockInternal(creature.Block);creature.SetCurrentHpInternal(creature.MaxHp);}
            foreach(var p in combat.Players)foreach(var pile in p.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);
            player.PlayerCombatState!.Energy=3;
        }
        foreach(bool up in new[]{false,true}) {
            await Reset();await Use(New<AirSlash>(up));
            Check(enemy.CurrentHp==1000-2*(up?3:2),"空气斩普通2次升级3次："+up);
            await Reset();await Use(New<OilSpell>(up));
            Check(enemy.CurrentHp==(up?978:980)&&enemy.GetPowerAmount<VulnerablePower>()==2,"油渍分段伤害及固定2易伤："+up);
            await Reset();enemy.SetCurrentHpInternal(10);await Use(New<OilSpell>(up));
            Check(enemy.IsDead&&survivor.CurrentHp==990&&!survivor.HasPower<VulnerablePower>(),"首段击杀时存活敌人不再受第二段及易伤："+up);
        }
        await Reset();await Use(New<Underestimated>());await Use(New<Underestimated>(true));
        Check(player.Creature.GetPowerAmount<ShrinkPower>()==2,"连续使用始终叠加缩小期限");
        await player.Creature.GetPower<ShrinkPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);
        Check(player.Creature.GetPowerAmount<ShrinkPower>()==1,"跨回合保留尚未结束的缩小");
        await Reset();
        var deck=new MagicDeckState();for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);}deck.GrantStartingProps();
        deck.Equip(deck.Add("BuildUpBody").InstanceId,ManaSlot.Draw);
        deck.Equip(deck.Add("BuildUpBody").InstanceId,ManaSlot.Hand);
        deck.Equip(deck.Add("BuildUpBody").InstanceId,ManaSlot.Discard);MagicDeckStore.Write(player,deck);
        // 第一个槽无魔力，只有第三个槽可支付。直接运行原生耳环完整自动打牌入口。
        await ManaCmd.Generate(player,PileType.Discard,2);var chant=New<RapidChant>();await CardPileCmd.Add(chant,PileType.Hand);
        var earring=await RelicCmd.Obtain<WhisperingEarring>(player);
        var talk=new Harmony("frieren.tests.todo110.talk");
        talk.Patch(AccessTools.Method(typeof(TalkCmd),nameof(TalkCmd.Play)),prefix:new HarmonyMethod(typeof(Todo110Tests),nameof(SkipTalk)));
        try {await earring.AfterAutoPrePlayPhaseEnteredLate(context,player);}
        finally {talk.UnpatchAll(talk.Id);}
        Check(player.Creature.GetPowerAmount<StrengthPower>()==1&&ManaCmd.InPile(player,PileType.Discard).Count()==1,"低语耳环原生入口跳过不可用前两槽并支付弃牌魔力");
        await Reset();await ManaCmd.Generate(player,PileType.Hand,3);
        using(CardSelectCmd.PushSelector(new VakuuCardSelector()))await Use(New<PreciseChant>());
        Check(player.Creature.GetPowerAmount<StrengthPower>()==3&&!ManaCmd.InPile(player,PileType.Hand).Any(),"原生耳环选择器精准咏唱消耗可用手牌槽全部3魔力");
        await Reset();var hair=await RelicCmd.Obtain<OldHairAccessory>(player);await hair.BeforeCombatStart();
        CardPlay Play(CardModel c)=>new(){Card=c,Resources=Resources(),Target=enemy,ResultPile=PileType.Discard,IsAutoPlay=false,PlayIndex=0,PlayCount=1};
        await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Draw);
        await hair.AfterCardPlayed(context,Play(New<Mana>()));await hair.AfterCardPlayed(context,Play(New<HighDensityMana>()));
        await hair.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);await hair.AfterPlayerTurnStart(context,player);
        Check(hair.Counter==2,"可掉落发饰的2次同调计数跨回合保留");
        await hair.AfterCardPlayed(context,Play(combat.CreateCard<Mana>(other)));await hair.AfterCardPlayed(context,Play(New<StrikeFrieren>()));
        Check(hair.Counter==2,"发饰不计其他玩家与非魔力卡");
        await hair.AfterCardPlayed(context,Play(New<Mana>()));await hair.AfterCardPlayed(context,Play(New<Mana>()));
        Check(hair.Counter==0&&player.PlayerCombatState!.Hand.Cards.Count==1,"跨回合累计4次后抽1张并归零");
        Check(!SpellCatalog.Available.Any(f=>f.Id==nameof(TransferToAlchemist))&&SpellCatalog.Get(nameof(TransferToAlchemist))!=null,"弃用因子排除获取但仍能解析旧档");
        Check(!ModelDb.Card<TransferToAlchemist>().ShouldShowInCardLibrary&&!ModelDb.Card<BreaksBarriersSpell>().ShouldShowInCardLibrary,"弃用因子与解析结界从百科隐藏");
        Check(Enumerable.Range(0,30).All(i=>!MagicRewardRules.Offer(5,n=>(i*7)%n).Choices.Contains(nameof(TransferToAlchemist))),"新战利品不同随机路径不掉落弃用因子");
        var option=ChantRules.Preview(ManaSlot.Draw,SpellCatalog.Get("GrossedOutLook").Rules,3);
        Check(option.Reward==3,"嫌弃脸只降低X力量，没有额外2点");
        await Reset();var source=New<RapidChant>();await FactorEffects.Apply(context,Play(source),option);
        Check(enemy.GetPowerAmount<StrengthPower>()==-3,"嫌弃脸实际结算减3力量");
        foreach(var type in new[]{typeof(DarkPower),typeof(SealPower),typeof(SleepInPower),typeof(AuraSoulPower),typeof(AuraArrogancePower)})
            Check(ModelDb.GetById<PowerModel>(ModelDb.GetId(type)).Type==PowerType.Debuff,"负面能力分类："+type.Name);
        Check(ModelDb.Power<AuraScalesPower>().Type==PowerType.None&&ModelDb.Power<Factor001Power>().Type==PowerType.None,"双方判定机制与谜题线索为中性");
        await Reset();var sealedCard=New<HellFireSummoning>();await CardPileCmd.Add(sealedCard,PileType.Draw);await sealedCard.BeforeCombatStart();
        Check(player.Creature.HasPower<SealPower>()&&sealedCard.Pile==null,"地狱业火封印后暂存卡牌");
        await PowerCmd.Remove(player.Creature.GetPower<SealPower>()!);
        Check(sealedCard.Pile?.Type==PileType.Hand,"封印被净化也归还卡牌，不丢失暂存牌");
        var dir=ProjectSettings.GlobalizePath("res://../../artifacts/todo110");Directory.CreateDirectory(dir);
        var exports=new List<object>();
        foreach(string lang in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(lang);
            var previews=DerivedCardPreviews.For(New<DomainExpansion>()).ToArray();
            Check(previews.Length==1&&previews[0] is CardHoverTip tip&&tip.Card is HighDensityMana,"领域展开只预览高密度魔力："+lang);
            var models=new CardModel[]{New<AirSlash>(),New<AirSlash>(true),New<OilSpell>(),New<OilSpell>(true),New<Underestimated>(),New<PreciseChant>(),New<TrueColours>(),FactorCard.Create(player,"GrossedOutLook"),New<DomainExpansion>()};
            foreach(var c in models) {var text=c.GetDescriptionForPile(PileType.None);Check(!text.Contains('{'),"模板完整："+lang+"/"+c.Id);if(lang=="zhs")exports.Add(new{model=c.GetType().Name,upgraded=c.IsUpgraded,title=c.Title,description=text});}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")) {
                var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});
                for(int i=0;i<models.Length;i++){var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,models[i]);canvas.AddChild(node);node.Position=new(205+i%5*370,270+i/5*520);node.Scale=Vector2.One*.85f;node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);}
                for(int i=0;i<5;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
                root.GetViewport().GetTexture().GetImage().SavePng(Path.Combine(dir,"cards-"+lang+".png"));canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            }
        }
        LocManager.Instance.SetLanguage("zhs");
        var options=new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
        File.WriteAllText(Path.Combine(dir,"cards.json"),JsonSerializer.Serialize(exports,options));
        File.WriteAllText(Path.Combine(dir,"powers.json"),JsonSerializer.Serialize(typeof(FrierenPower).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(FrierenPower).IsAssignableFrom(t)).Select(t=>ModelDb.GetById<PowerModel>(ModelDb.GetId(t))).Select(p=>new{model=p.GetType().Name,name=p.Title.GetFormattedText(),type=p.Type.ToString()}),options));
        RunManager.Instance.CleanUp();return count;
    }
}

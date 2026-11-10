using System.Text.Json;
using System.Text.RegularExpressions;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Author01214Tests
{
    private sealed class Selector:TestCardSelector,MegaCrit.Sts2.Core.TestSupport.ICardSelector
    {
        public List<CardModel[]> Seen {get;}=[];
        async Task<IEnumerable<CardModel>> MegaCrit.Sts2.Core.TestSupport.ICardSelector.GetSelectedCards(IEnumerable<CardModel> options,int min,int max)
        {var cards=options.ToArray();Seen.Add(cards);return await base.GetSelectedCards(cards,min,max);}
    }
    public static async Task<int> Run(Node root)
    {
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;GD.Print("PASS 0.12.14："+text);}
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var p=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([p,other],seed:"AUTHOR-01214");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(p);combat.AddPlayer(other);
        var enemies=Enumerable.Range(0,2).Select(_=>combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null)).ToArray();
        foreach(var enemy in enemies){combat.AddCreature(enemy);enemy.SetMaxHpInternal(99999);enemy.SetCurrentHpInternal(99999);}
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();var selector=new Selector();using var choices=CombatTestState.UseSelector(selector);
        T New<T>(bool up=false)where T:CardModel{var c=combat.CreateCard<T>(p);if(up)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel c){await CardPileCmd.Add(c,PileType.Hand);await c.OnPlayWrapper(context,enemies[0],false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);}
        async Task Reset(){
            foreach(var player in new[]{p,other}){foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);player.PlayerCombatState.Energy=20;player.Creature.LoseBlockInternal(player.Creature.Block);}
            foreach(var e in enemies){foreach(var power in e.Powers.ToArray())await PowerCmd.Remove(power);e.SetCurrentHpInternal(e.MaxHp);e.LoseBlockInternal(e.Block);}
            p.Deck.Clear(silent:true);CombatManager.Instance.History.Clear();selector.Cleanup();selector.Seen.Clear();
        }
        MagicDeckState Deck(string spell){var d=MagicDeckState.CreatePrototypeLoadout();for(int i=0;i<3;i++)d.Equip(d.Add(spell).InstanceId,(ManaSlot)i);return MagicDeckState.FromJson(JsonSerializer.Serialize(new MagicDeckSave(3,d.Items.ToArray(),true,3,StartingPropsGranted:true)));}
        foreach(bool up in new[]{false,true}){
            await Reset();var c=New<Imagination>(up);await Use(c);Check(!c.Keywords.Contains(CardKeyword.Exhaust)&&c.Pile?.Type==PileType.Discard,"想象力普通／升级打出后进入弃牌堆："+up);
            Check(!up||c.Keywords.Contains(CardKeyword.Retain),"想象力升级保留仍在");
        }
        await Reset();await CardPileCmd.Add(run.CreateCard<FairyTaleEnd>(p),PileType.Deck);await CardPileCmd.Add(run.CreateCard<FairyTaleEnd>(p),PileType.Deck);
        var fairy=New<FairyTaleEnd>();fairy.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],fairy.DynamicVars);Check(fairy.DynamicVars.Damage.PreviewValue==6,"两张童话自身均不增加倍率");
        await CardPileCmd.Add(run.CreateCard<LightningMagic>(p),PileType.Deck);await CardPileCmd.Add(run.CreateCard<LightningMagic>(p),PileType.Deck);
        await CardPileCmd.Add(New<MoldSpell>(),PileType.Hand);fairy.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],fairy.DynamicVars);
        Check(fairy.DynamicVars.Damage.PreviewValue==12,"同名其他传说只翻倍一次，临时手牌不计入");int hp=enemies[0].CurrentHp;await Use(fairy);Check(hp-enemies[0].CurrentHp==12,"童话实际命中与预览一致");
        foreach(bool up in new[]{false,true}){
            await Reset();var ready=New<ReadyStance>(up);var high=New<Imagination>();var low=New<RapidChant>();await CardPileCmd.Add(high,PileType.Draw);await CardPileCmd.Add(low,PileType.Draw);hp=enemies[0].CurrentHp;await Use(ready);
            Check(hp-enemies[0].CurrentHp==(up?13:10)&&low.Pile?.Type==PileType.Hand&&high.Pile?.Type==PileType.Draw,"准备架势伤害及最低耗能技能："+up);
        }
        foreach(bool xFirst in new[]{false,true}){
            await Reset();var x=New<TreasureVaultSpell>();var zero=New<FiveMinutesMore>();
            foreach(var c in xFirst?new CardModel[]{x,zero}:new CardModel[]{zero,x})await CardPileCmd.Add(c,PileType.Draw);
            await Use(New<ReadyStance>());Check((xFirst?(CardModel)x:zero).Pile?.Type==PileType.Hand,"X费和0费同优先级、按牌顶顺序："+xFirst);
        }
        await Reset();var discounted=New<Imagination>();discounted.EnergyCost.AddThisCombat(-2);await CardPileCmd.Add(New<DefendFrieren>(),PileType.Draw);await CardPileCmd.Add(discounted,PileType.Draw);await Use(New<ReadyStance>());
        Check(discounted.Pile?.Type==PileType.Hand,"准备架势按当前修正后耗能选牌");
        await Reset();var discard=New<FiveMinutesMore>();var attack=New<StrikeFrieren>();await CardPileCmd.Add(discard,PileType.Discard);await CardPileCmd.Add(attack,PileType.Draw);await Use(New<ReadyStance>());
        Check(discard.Pile?.Type==PileType.Discard&&attack.Pile?.Type==PileType.Draw,"没有技能时不抽其他类型，也不检索弃牌堆");
        await Reset();var cry=New<Crying>(true);await Use(cry);Check(p.Creature.Block==9&&cry.CurrentWeak==9,"嗷嗷哭升级实际获得9格挡并交换到虚弱层数");
        Check(FrierenCardAvailability.IsRetired(ModelDb.Card<RedAppleSpell>())&&!ModelDb.Card<RedAppleSpell>().ShouldShowInCardLibrary&&!FrierenCardAvailability.IsRandomCandidate(ModelDb.Card<RedAppleSpell>()),"青苹果魔法废弃、隐藏百科并退出新获取");
        Check(CardModel.FromSerializable(New<RedAppleSpell>().ToSerializable()) is RedAppleSpell,"废弃卡保留旧档读取");
        foreach(bool up in new[]{false,true}){
            await Reset();MagicDeckStore.Write(p,Deck("BuildUpBody"));await ManaCmd.Generate(p,PileType.Draw,1);selector.PrepareToSelect(new[]{0});selector.PrepareToSelect(new[]{0});
            var prospect=New<ProspectingSpell>(up);await Use(prospect);var options=selector.Seen.Last();var generated=p.PlayerCombatState!.Hand.Cards.Single();
            Check(options.Length==(up?3:2)&&options.Select(c=>c.Id).Distinct().Count()==options.Length&&options.All(c=>c.Pool is ColorlessCardPool),"探矿原生无色选项数量与去重："+up);
            Check(generated.Pool is ColorlessCardPool&&!generated.IsUpgraded&&generated.EnergyCost.GetWithModifiers(CostModifiers.Local)==generated.EnergyCost.Canonical&&prospect.Pile?.Type==PileType.Discard,"探矿只将选中牌加入手牌，不额外升级、减费或消耗："+up);
            Check(p.Creature.GetPowerAmount<StrengthPower>()==1&&ManaCmd.InPile(p,PileType.Draw).Count()==0,"探矿先完成咏唱1并实际支付魔力");
        }
        foreach(var slot in Enum.GetValues<ManaSlot>()){
            await Reset();MagicDeckStore.Write(p,Deck("CopyMagic"));await ManaCmd.Generate(p,ManaCmd.Pile(slot),4);
            var option=ChantCmd.Options(p,2).Single(o=>o.Slot==slot);await ChantCmd.Resolve(context,CombatTestState.NewPlay(New<RapidChant>(),null,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0}),option);
            var cards=p.PlayerCombatState!.AllPiles.SelectMany(pile=>pile.Cards).Where(c=>c.Pool is ColorlessCardPool).ToArray();
            Check(option.Cost==4&&option.Reward==2&&cards.Length==2&&cards.All(c=>c.IsUpgraded&&c.Pile?.Type==ManaCmd.Pile(slot)),"复制魔法2X费用、升级与目标牌堆："+slot);
            Check(other.PlayerCombatState!.AllPiles.All(pile=>pile.Cards.Count==0),"复制魔法不影响队友库存："+slot);
        }
        await Reset();var net=New<ArcaneNet>();await CardPileCmd.Add(net,PileType.Hand);Check(net.EnergyCost.GetWithModifiers(CostModifiers.All)==10,"天罗地网初始10费");
        void RecordMana(Player owner){var mana=combat.CreateCard<Mana>(owner);CombatManager.Instance.History.CardPlayStarted(combat,CombatTestState.NewPlay(mana,null,PileType.Exhaust,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0}));}
        RecordMana(p);RecordMana(other);RecordMana(p);Check(net.EnergyCost.GetWithModifiers(CostModifiers.All)==8,"天罗仅计算本人同调，不计算队友");
        AccessTools.Property(typeof(CombatState),nameof(CombatState.RoundNumber)).SetValue(combat,2);Check(net.EnergyCost.GetWithModifiers(CostModifiers.All)==8,"跨回合保留本场同调计数");
        for(int i=0;i<12;i++)RecordMana(p);Check(net.EnergyCost.GetWithModifiers(CostModifiers.All)==0,"天罗费用下限0");
        foreach(bool up in new[]{false,true}){var c=New<ArcaneNet>(up);var before=enemies.Select(e=>e.CurrentHp).ToArray();await Use(c);Check(enemies.Select((e,i)=>before[i]-e.CurrentHp).All(n=>n==(up?24:19)),"天罗群伤普通／升级："+up);}
        foreach(bool up in new[]{false,true}){
            await Reset();var target=New<Imagination>();await CardPileCmd.Add(target,PileType.Hand);selector.PrepareToSelect(new[]{0});var memory=New<MemoryTransferSpell>(up);hp=enemies[0].CurrentHp;await Use(memory);
            Check(hp-enemies[0].CurrentHp==14&&target.EnergyCost.GetWithModifiers(CostModifiers.All)==(up?0:1),"记忆让渡14伤害与单张本回合减费："+up);
            target.EnergyCost.AfterCardPlayedCleanup();Check(target.EnergyCost.GetWithModifiers(CostModifiers.All)==(up?0:1),"减费保留整个当前回合");target.EnergyCost.EndOfTurnCleanup();Check(target.EnergyCost.GetWithModifiers(CostModifiers.All)==2,"回合结束后恢复费用");
        }
        Check(LegendaryCards.Pool.Contains(typeof(MemoryTransferSpell))&&FrierenCardAvailability.IsRandomCandidate(ModelDb.Card<ArcaneNet>())&&FrierenCardAvailability.IsRandomCandidate(ModelDb.Card<ProspectingSpell>()),"新牌进入对应正常获取与传说生成池");
        await Reset();
        var types=new[]{typeof(ProspectingSpell),typeof(CopyMagic),typeof(ArcaneNet),typeof(MemoryTransferSpell),typeof(ReadyStance),typeof(Crying),typeof(Imagination),typeof(FairyTaleEnd),typeof(ValuableCargo),typeof(SleepIn),typeof(WakeUp),typeof(FiveMinutesMore)};
        var exported=new List<object>();
        foreach(string language in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(language);var models=new List<CardModel>();
            foreach(var type in types){
                var c=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();c.Owner=p;string normal=c.GetDescriptionForPile(PileType.None);models.Add(c);
                string upgraded="";if(c.MaxUpgradeLevel>0){var u=(CardModel)c.MutableClone();CardCmd.Upgrade(u);models.Add(u);upgraded=u.GetDescriptionForPile(PileType.None);}
                exported.Add(new{model=type.Name,language,title=c.Title,normal,upgraded,id=c.Id.Entry});
            }
            if(language=="zhs"){
                Check(models.OfType<SleepIn>().All(c=>c.GetDescriptionForPile(PileType.None).Contains("。[gold]起床！[/gold]被")),"赖床两处起床引用均为金色");
                Check(models.OfType<WakeUp>().All(c=>c.GetDescriptionForPile(PileType.None).Contains("[gold]赖床[/gold]")),"起床正文赖床金色");
                Check(models.OfType<FiveMinutesMore>().All(c=>c.GetDescriptionForPile(PileType.None).Contains("[gold]懒懒散散[/gold]")),"再睡五分钟普通升级均有金色懒懒散散");
            }
            foreach(var (batch,page) in models.Chunk(12).Select((cards,i)=>(cards,i))){
                var host=new Control();root.AddChild(host);host.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});var nodes=new List<NCard>();
                for(int i=0;i<batch.Length;i++){var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,batch[i]);host.AddChild(node);node.Position=new(160+i%6*320,270+i/6*520);node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);nodes.Add(node);}
                for(int i=0;i<4;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
                foreach(var node in nodes){var body=node.GetNode<RichTextLabel>("%DescriptionLabel");Check(body.GetContentHeight()<=body.Size.Y+2&&body.GetThemeFontSize("normal_font_size")>=16,"原生卡面无溢出且字号可读："+language+"/"+node.Model!.Id+" height="+body.GetContentHeight()+"/"+body.Size.Y+" font="+body.GetThemeFontSize("normal_font_size"));}
                if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/author01214-{language}-{page}.png"));
                host.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            }
        }
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/cards-01214-text.json"),JsonSerializer.Serialize(exported,new JsonSerializerOptions{WriteIndented=true}));
        CombatTestState.SetInProgress(false);RunManager.Instance.CleanUp();return checks;
    }
}

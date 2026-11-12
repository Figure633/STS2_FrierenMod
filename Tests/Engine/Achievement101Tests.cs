using System.Text.Json;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Shared.Compatibility;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class Achievement101Tests
{
    private static bool SkipEndTurn()=>false;
    public static async Task<int> Run()
    {
        int checks=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;GD.Print("PASS 成就101："+name);}
        SaveManager.Instance.InitPrefsDataForTest();
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"ACHIEVEMENTS101");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;PresentationSettings.Achievements=true;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
        Creature Spawn<T>()where T:MonsterModel {var c=combat.CreateCreature(ModelDb.Monster<T>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(c);return c;}
        var target=Spawn<Axebot>();target.SetMaxHpInternal(99999);target.SetCurrentHpInternal(99999);CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        using var selection=CombatTestState.UseSelector(new MegaCrit.Sts2.Core.TestSupport.TestCardSelector());
        bool Has(string id)=>SlotBackStore.Read().Unlocked.Contains(id);
        void Clear(string id)=>SlotBackStore.Read().Unlocked.Remove(id);
        async Task Reset(){foreach(var p in player.Creature.Powers.ToArray())await PowerCmd.Remove(p);CombatManager.Instance.History.Clear();player.Creature.LoseBlockInternal(player.Creature.Block);player.Creature.SetCurrentHpInternal(player.Creature.MaxHp);}
        MagicDeckState Deck(params string[] ids){var d=new MagicDeckState();for(int i=0;i<3;i++){d.PrepareDraft(_=>0);d.SelectInitial(d.Draft[0]);}foreach(var x in d.Items.ToArray())d.Remove(x.InstanceId);for(int i=0;i<ids.Length;i++)d.Equip(d.Add(ids[i]).InstanceId,(ManaSlot)i);return d;}
        CardPlay Play(CardModel c,Creature? e=null)=>CombatTestState.NewPlay(c,e??target,PileType.Discard,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0});
        async Task Use(CardModel c,Creature? e=null)=>await c.OnPlayWrapper(context,e??target,false,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);
        foreach(string id in new[]{"4004","4005","4006","5001","5007","5009","5010","5012","6001","7001","7002"})Clear(id);
        MagicDeckStore.Write(player,Deck("BluePrint","BluePrint","BluePrint"));await ManaCmd.Generate(player,PileType.Draw,100);FrierenAchievements.TurnStarted(player);
        Check(!Has("5001")&&!Has("5007")&&!Has("7001"),"100魔力、蓝图和原手牌上限条件不再误触发新成就");
        foreach(var type in new[]{typeof(Byrdpip),typeof(Byrdonis),typeof(OwlMagistrate),typeof(HunterKiller),typeof(ThievingHopper),typeof(Ovicopter),typeof(Exoskeleton)}) {
            Clear("4004");var e=combat.CreateCreature(ModelDb.GetById<MonsterModel>(ModelDb.GetId(type)).ToMutable(),CombatSide.Enemy,type==typeof(Exoskeleton)?"first":null);combat.AddCreature(e);
            e.Monster!.SetUpForCombat();e.Monster.RollMove([player.Creature]);
            await Use(combat.CreateCard<BirdCapturingSpell>(player),e);
            Check(Has("4004")== (type==typeof(HunterKiller)) && e.HasPower<BirdCapturedPower>(),"捕鸟仍可眩晕原有目标，仅猎人杀手解锁成就："+type.Name);
        }
        foreach(int kinds in new[]{2,3}) {
            await Reset();Clear("4006");await PowerCmd.Apply<WeakPower>(context,player.Creature,99,target,null);await PowerCmd.Apply<VulnerablePower>(context,player.Creature,1,target,null);
            if(kinds==3)await PowerCmd.Apply<FrailPower>(context,player.Creature,1,target,null);
            await PowerCmd.Apply<StrengthPower>(context,player.Creature,3,player.Creature,null);await Use(combat.CreateCard<RustCleanMagic>(player));
            Check(!Has("4006")&&player.Creature.GetPowerAmount<StrengthPower>()==3,"除锈不再解锁4006，也不移除增益："+kinds);
        }
        foreach(int kinds in new[]{0,1,3}) {
            await Reset();Clear("4006");
            if(kinds>0)await PowerCmd.Apply<WeakPower>(context,player.Creature,3,target,null);
            if(kinds==3) {await PowerCmd.Apply<VulnerablePower>(context,player.Creature,2,target,null);await PowerCmd.Apply<FrailPower>(context,player.Creature,2,target,null);}
            await PowerCmd.Apply<StrengthPower>(context,player.Creature,3,player.Creature,null);
            var spell=combat.CreateCard<CustomLegendarySpell>(player);spell.Configure(3,[new LegendaryEffect("RemoveDebuffs",1)]);
            await Use(spell);
            Check(Has("4006")&&!player.Creature.Powers.Any(p=>p.TypeForCurrentAmount==PowerType.Debuff)&&player.Creature.GetPowerAmount<StrengthPower>()==3,
                "自定义魔法清除自身减益即解锁4006，无三种门槛并保留增益："+kinds);
        }
        Clear("4006");var remoteSpell=combat.CreateCard<CustomLegendarySpell>(other);remoteSpell.Configure(3,[new LegendaryEffect("RemoveDebuffs",1)]);await Use(remoteSpell);
        Check(!Has("4006"),"其他玩家的自定义魔法不解锁本机卡背");
        await Reset();var aura=Spawn<Aura>();await CreatureCmd.Kill(aura);Check(Has("5001"),"实际死亡钩子在击败阿乌拉后解锁");
        foreach(bool damaged in new[]{false,true}) {
            await Reset();Clear("6001");var mimic=Spawn<Mimic>();player.Creature.GainBlockInternal(10);
            await GameApi.Damage(context,player.Creature,damaged?11:10,ValueProp.Unpowered,target,null);await CreatureCmd.Heal(player.Creature,99);
            await CreatureCmd.Kill(mimic);Check(Has("6001")==!damaged,"宝箱怪无伤按整场生命伤害记录判定，治疗不抹除受伤："+damaged);
        }
        foreach(var (up,kinds,expected) in new[]{(false,8,768),(false,9,1536),(true,7,512),(true,8,1024)}) {
            Clear("4005");foreach(var c in player.Deck.Cards.ToArray())player.Deck.RemoveInternal(c);
            var types=new[]{typeof(FairyTaleEnd)}.Concat(LegendaryCards.Pool.Where(t=>t!=typeof(FairyTaleEnd)).Take(kinds-1));
            foreach(var type in types) {var c=ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();run.AddCard(c,player);await CardPileCmd.Add(c,PileType.Deck);}
            // 同名重复牌不会继续增加倍率。
            var duplicate=ModelDb.Card<FairyTaleEnd>().ToMutable();run.AddCard(duplicate,player);await CardPileCmd.Add(duplicate,PileType.Deck);
            var fairy=combat.CreateCard<FairyTaleEnd>(player);if(up)fairy.UpgradeInternal();int hp=target.CurrentHp;await Use(fairy);
            Check(hp-target.CurrentHp==expected&&Has("4005")== (expected>=1000),"童话实际伤害／成就：升级="+up+" 种类="+kinds+" 伤害="+expected);
        }
        async Task Puzzle(int[] order,bool equipped=true) {
            await Reset();Clear("7001");Clear("7002");MagicDeckStore.Write(player,equipped?Deck("Factor100","Factor010","Factor001"):Deck("BluePrint","BluePrint","BluePrint"));
            var ids=new[]{"Factor100","Factor010","Factor001"};var card=combat.CreateCard<RapidChant>(player);
            foreach(int i in order)await PuzzleCmd.Chant(context,Play(card),ChantRules.Preview((ManaSlot)i,SpellCatalog.Get(ids[i]).Rules,1));
        }
        await Puzzle([2,1,0]);Check(Has("7001")&&!Has("7002")&&target.IsAlive,"正确装槽后反序只解锁反序成就，不触发正序击杀");
        await Puzzle([2,1,0],false);Check(!Has("7001"),"未正确装槽时不能靠反序选项解锁");
        await Puzzle([1,2,0]);Check(!Has("7001"),"其他乱序不解锁反序成就");
        await Puzzle([2,2,1,0]);Check(!Has("7001"),"重复谜题因子不冒充完整反序");
        await Puzzle([2,1]);foreach(var p in player.Creature.Powers.OfType<TurnOnlyPower>().ToArray())await p.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);
        await PuzzleCmd.Chant(context,Play(combat.CreateCard<RapidChant>(player)),ChantRules.Preview(ManaSlot.Draw,SpellCatalog.Get("Factor100").Rules,1));Check(!Has("7001"),"反序进度不能跨回合拼接");
        foreach(var (rarity,time,total) in new[]{(FactorRarity.Uncommon,3600L,19),(FactorRarity.Rare,3601L,20)}) {
            foreach(string id in new[]{"5009","5010","5012"})Clear(id);
            var ids=SpellCatalog.All.Where(f=>f.Rarity==rarity&&!f.Puzzle).Take(3).Select(f=>f.Id).ToArray();var d=Deck(ids);while(d.Items.Count+d.Props.Count<total)d.AddProp("MysteryBox");MagicDeckStore.Write(player,d);
            AccessTools.Property(typeof(RunManager),nameof(RunManager.WinTime)).SetValue(RunManager.Instance,time);FrierenAchievements.RunEnded(run,player,true);
            Check(Has("5010")== (rarity==FactorRarity.Rare)&&Has("5012")== (time>3600)&&Has("5009")== (total>=20),"通关稀有度、1小时和20件边界："+rarity+"/"+time+"/"+total);
        }
        await Reset();Clear("5007");var eater=Spawn<Mimic>();await PowerCmd.Apply<MimicPower>(context,eater,5,eater,null);var countdown=eater.GetPower<MimicPower>()!;
        var patch=new Harmony("frieren.tests.eating101");patch.Patch(AccessTools.Method(typeof(PlayerCmd),nameof(PlayerCmd.EndTurn)),prefix:new HarmonyMethod(typeof(Achievement101Tests),nameof(SkipEndTurn)));
        try {
            for(int i=0;i<4;i++)await countdown.AfterCardPlayedLate(context,Play(combat.CreateCard<StrikeFrieren>(player)));
            Check(!Has("5007"),"吃人前四张牌不提前解锁");await countdown.AfterCardPlayedLate(context,Play(combat.CreateCard<StrikeFrieren>(player)));
            Check(Has("5007")&&player.Creature.HasPower<DarkPower>(),"第五张触发宝箱怪吃人并解锁");
        }finally{patch.UnpatchAll(patch.Id);}
        var totals=new List<int>();for(uint seed=0;seed<200;seed++) {
            var rng=GameApi.CreateRng(seed);var d=Deck("BuildUpBody","MeleeMage","HighSpeedMovement");d.GrantStartingProps();
            for(int fight=0;fight<24;fight++) {
                bool drop=MagicRewardRules.ShouldDrop(rng.NextInt(100),d.RewardPity,fight%4==3,false);d.RewardPity+=drop?-10:10;
                if(drop) {var offer=MagicRewardRules.Offer(d.RewardRarityOffset,GameApi.CreateRng((uint)rng.NextInt()).NextInt);d.RewardRarityOffset=offer.RarityOffset;string id=offer.Choices.Contains("MysteryBox")?"MysteryBox":offer.Choices[0];if(MagicPropRules.All.Any(p=>p.Id==id)){var p=d.AddProp(id);if(id=="MysteryBox")d=MagicPropRules.Apply(d,p.InstanceId,[],false,rng.NextInt)!.Deck;}else d.Add(id);}
                if(fight%4==3){var p=d.AddProp("MysteryBox");d.AddProp("MysticBox");d=MagicPropRules.Apply(d,p.InstanceId,[],false,rng.NextInt)!.Deck;}
            }
            totals.Add(d.Items.Count+d.Props.Count);
        }
        Check(totals.Any(n=>n>=30),"24战6营地保留物品路线可达到30件（规则模拟，不是实机通关）");
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo101/feasibility.json"),JsonSerializer.Serialize(new{legendaryNormalKinds=8,legendaryUpgradedKinds=7,runs=totals.Count,min=totals.Min(),max=totals.Max(),average=totals.Average(),reached30=totals.Count(n=>n>=30)}));
        RunManager.Instance.CleanUp();return checks;
    }
}

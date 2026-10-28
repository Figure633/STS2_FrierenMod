using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Shared.Compatibility;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class VersionCompatibilityTests
{
    public static async Task<int> Run()
    {
        int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;GD.Print("PASS 双版本："+name);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"VERSION-COMPAT");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();
        ResourceInfo resources=new(){EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0};
        async Task Hit(CardModel card){if(card.Pile?.Type!=PileType.Hand)await CardPileCmd.Add(card,PileType.Hand);await card.OnPlayWrapper(context,enemy,false,resources,true);}
        async Task<CardModel> PreviewStrike(int expected) {
            var card=combat.CreateCard<StrikeFrieren>(player);
            await CardPileCmd.Add(card,PileType.Hand);
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,card.DynamicVars);
            if(card.DynamicVars.Damage.PreviewValue!=expected)throw new Exception("原生伤害预览错误："+card.DynamicVars.Damage.PreviewValue);
            return card;
        }
        async Task Clear(){foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);enemy.SetCurrentHpInternal(enemy.MaxHp);}
        await PowerCmd.Apply<DivinityPower>(context,player.Creature,1,player.Creature,null);
        int before=enemy.CurrentHp;await Hit(await PreviewStrike(18));
        Check(before-enemy.CurrentHp==18,"神格在原生伤害链中将6伤害变为18");await Clear();
        await PowerCmd.Apply<StrikeUpPower>(context,player.Creature,3,player.Creature,null);
        before=enemy.CurrentHp;await Hit(await PreviewStrike(9));
        Check(before-enemy.CurrentHp==9,"打击加成在原生伤害链中生效一次");await Clear();
        await PowerCmd.Apply<PaperPlanePower>(context,player.Creature,1,player.Creature,null);
        var paper=player.Creature.GetPower<PaperPlanePower>()!;
        for(int i=0;i<4;i++)CombatManager.Instance.History.CardPlayStarted(combat,CombatTestState.NewPlay(card: combat.CreateCard<Mana>(player), target: null, resultPile: PileType.Exhaust, resources: resources, playCount: 1, playIndex: 0, isAutoPlay: false));
        before=enemy.CurrentHp;await Hit(await PreviewStrike(12));
        Check(before-enemy.CurrentHp==12 && paper.DisplayAmount==4,"纸飞机增伤在预览和结算中不重复叠加，并保留本回合最高记录");await Clear();
        var collapse=combat.CreateCard<ManaCollapse>(player);await Hit(collapse);
        Check(collapse.Pile?.Type==PileType.Draw,"魔力崩坏经原生去向钩子返回抽牌堆");
        var mana=combat.CreateCard<HighDensityMana>(player);int energy=player.PlayerCombatState!.Energy;await Hit(mana);
        Check(mana.Pile?.Type==PileType.Exhaust && player.PlayerCombatState.Energy==energy+1,"高密度魔力原生消耗并获得能量");
        await PowerCmd.Apply<SynchroWithoutManaPower>(context,player.Creature,1,player.Creature,null);
        mana=combat.CreateCard<HighDensityMana>(player);energy=player.PlayerCombatState.Energy;await Hit(mana);
        Check(mana.Pile?.Type==PileType.Hand && player.PlayerCombatState.Energy==energy+1,"节俭改变去向后仍保留高密度魔力收益");
        await CreatureCmd.GainBlock(enemy,12,ValueProp.Unpowered,null);
        await GameApi.LoseBlock(context,enemy,7,player.Creature);
        Check(enemy.Block==5,"失去格挡命令在两版均按指定量结算");
        var rng1=GameApi.CreateRng(123);var rng2=GameApi.CreateRng(123);
        Check(Enumerable.Range(0,20).All(_=>rng1.NextInt(100)==rng2.NextInt(100)),"本版同种子随机序列可复现");
        Check(GameApi.Seed(run.Rng)==Convert.ToUInt64(AccessTools.Property(run.Rng.GetType(),"Seed").GetValue(run.Rng)),"正确读取当前版本32位或64位种子");
        RunManager.Instance.CleanUp();return count;
    }
}

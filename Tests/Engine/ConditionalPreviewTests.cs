using System.Text.RegularExpressions;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class ConditionalPreviewTests
{
    public static async Task<int> Run()
    {
        int count=0;
        void Check(bool ok,string name) { if(!ok) throw new Exception(name); GD.Print("PASS 条件预览："+name); count++; }
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"FRIEREN-CONDITIONAL-PREVIEW");
        RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService()); LocalContext.NetId=1;
        var combat=new CombatState(runState:run); combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Axebot>().ToMutable(),CombatSide.Enemy,null);
        combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);
        AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        var context=new BlockingPlayerChoiceContext();
        async Task<T> Card<T>(bool upgraded=false) where T:CardModel {
            var card=combat.CreateCard<T>(player);
            if(upgraded){card.UpgradeInternal();card.FinalizeUpgradeInternal();}
            await CardPileCmd.Add(card,PileType.Hand);return card;
        }
        CardPlay Play(CardModel card) => new() { Card=card,Target=enemy,ResultPile=PileType.Discard,PlayCount=1,PlayIndex=0,IsAutoPlay=false,
            Resources=new ResourceInfo {EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0} };
        string Text(CardModel card) {
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,card.DynamicVars);
            var text=card.GetDescriptionForPile(PileType.Hand,enemy);
            return text[(text.LastIndexOf('\n')+1)..];
        }
        string Plain(string s)=>Regex.Replace(s,@"\[[^\]]*\]","");
        async Task ClearPowers() {foreach(var p in player.Creature.Powers.ToArray())await PowerCmd.Remove(p);}
        foreach(string language in new[]{"zhs","eng"}) {
            LocManager.Instance.SetLanguage(language);
            foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var card in pile.Cards.ToArray())pile.RemoveInternal(card);
            CombatManager.Instance.History.Clear();await ClearPowers();
            var finisher=await Card<MageStyleFinisher>();var tea=await Card<HotTeaSpell>();
            var ordinary=await Card<OrdinaryOffensiveMagic>();var locked=await Card<LockTarget>();
            Check(Text(finisher).Contains(language=="zhs"?"5点伤害0次":"5 damage 0 times"),language+"零同调显示零次");
            for(int i=0;i<3;i++)CombatManager.Instance.History.CardPlayStarted(combat,Play(combat.CreateCard<Mana>(player)));
            CombatManager.Instance.History.CardPlayStarted(combat,Play(combat.CreateCard<StrikeFrieren>(player)));
            Check(Text(finisher).Contains(language=="zhs"?"5点伤害3次":"5 damage 3 times"),language+"只统计本回合同调");
            var strength=await PowerCmd.Apply<StrengthPower>(context,player.Creature,2,player.Creature,null);
            Check(Text(finisher).Contains("[green]7[/green]"),language+"力量只让伤害数字变绿");
            int hp=enemy.CurrentHp;
            await finisher.OnPlayWrapper(context,enemy,false,Play(finisher).Resources,skipCardPileVisuals:true);
            Check(hp-enemy.CurrentHp==21,language+"三次伤害与括号预览一致");
            await CardPileCmd.Add(finisher,PileType.Hand);
            await PowerCmd.Remove(strength!);
            var weak=await PowerCmd.Apply<WeakPower>(context,player.Creature,1,enemy,null);
            Check(Text(finisher).Contains("[red]3[/red]"),language+"虚弱伤害显示红色");
            await PowerCmd.Remove(weak!);
            var upgraded=await Card<MageStyleFinisher>(true);
            Check(Text(upgraded).Contains(language=="zhs"?"7点伤害3次":"7 damage 3 times"),language+"升级伤害正确");
            CombatManager.Instance.History.Clear();
            Check(Text(upgraded).Contains(language=="zhs"?"0次":"0 times"),language+"回合记录清空后次数归零");
            foreach(var pile in new[]{PileType.Draw,PileType.Hand,PileType.Discard,PileType.Exhaust}) {
                var mana=combat.CreateCard<Mana>(player);await CardPileCmd.Add(mana,pile);
            }
            await CardPileCmd.Add(combat.CreateCard<HighDensityMana>(player),PileType.Hand);
            Check(Plain(Text(tea)).Contains("16") && !Text(tea).Contains("[green]"),language+"格挡包含三堆及高密度魔力，不计消耗堆且无增益不变绿");
            var dex=await PowerCmd.Apply<DexterityPower>(context,player.Creature,3,player.Creature,null);
            Check(Text(tea).Contains("[green]19[/green]"),language+"总格挡的敏捷只计算一次");
            int block=player.Creature.Block;
            await tea.OnPlayWrapper(context,null,false,Play(tea).Resources,skipCardPileVisuals:true);
            Check(player.Creature.Block-block==19,language+"实际获得格挡与预览一致");
            await CardPileCmd.Add(tea,PileType.Hand);
            var frail=await PowerCmd.Apply<FrailPower>(context,player.Creature,1,enemy,null);
            Check(Text(tea).Contains("[red]14[/red]"),language+"脆弱按总格挡取整并变红");
            await PowerCmd.Remove(frail!);await PowerCmd.Remove(dex!);
            var upgradedTea=await Card<HotTeaSpell>(true);
            Check(Plain(Text(upgradedTea)).Contains("19"),language+"升级格挡正确");
            Check(Plain(Text(ordinary)).Contains("2") && !Text(ordinary).Contains("[green]"),language+"消耗堆魔力伤害无增益时保持普通颜色");
            strength=await PowerCmd.Apply<StrengthPower>(context,player.Creature,2,player.Creature,null);
            Check(Text(ordinary).Contains("[green]4[/green]"),language+"动态伤害复用原生力量预览");
            await PowerCmd.Remove(strength!);
            Check(Plain(Text(locked)).Contains(language=="zhs"?"8点伤害2次":"8 damage 2 times"),language+"锁定目标相等时显示两次");
            await CardPileCmd.Add(combat.CreateCard<Mana>(player),PileType.Discard);
            Check(Plain(Text(locked)).Contains(language=="zhs"?"8点伤害1次":"8 damage 1 times"),language+"牌堆变化后显示一次");
            Check(new CardModel[]{finisher,tea,ordinary,locked}.All(c=>
                !ModelDb.GetById<CardModel>(c.Id).GetDescriptionForPile(PileType.None).Contains(language=="zhs"?"（":"(")),
                language+"百科只保留一般规则，不显示战斗括号");
        }
        return count;
    }
}

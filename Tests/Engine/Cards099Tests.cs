using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

public static class Cards099Tests
{
 public static async Task<int> Run(Node root)
 {
  int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 099："+text);}
  var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
  var run=RunState.CreateForTest([player,other],seed:"CARDS099");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
  var combat=new CombatState(runState:run);combat.AddPlayer(player);combat.AddPlayer(other);
  var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
  CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);var context=new ThrowingPlayerChoiceContext();
  T New<T>(bool up=false)where T:CardModel{var c=combat.CreateCard<T>(player);if(up)c.UpgradeInternal();return c;}
  ResourceInfo resources=new(){EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0};
  CardPlay Play(CardModel c)=>CombatTestState.NewPlay(card:c,target:enemy,resultPile:PileType.Discard,resources:resources);
  async Task Use(CardModel c)=>await c.OnPlayWrapper(context,enemy,false,resources,true);
  int Mana(PileType pile)=>ManaCmd.InPile(player,pile).Count();
  async Task Reset(){foreach(var c in combat.Creatures){foreach(var p in c.Powers.ToArray())await PowerCmd.Remove(p);c.LoseBlockInternal(c.Block);c.SetCurrentHpInternal(c.MaxHp);}foreach(var p in new[]{player,other})foreach(var pile in p.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);CombatManager.Instance.History.Clear();}
  Check(SpellCatalog.Get("BloodSpell").Rarity==FactorRarity.Rare&&ModelDb.Card<BloodSpell>().Rarity==CardRarity.Rare,"驭血目录和展示模型均为金因子");
  Check(!SpellCatalog.Offer(FactorRarity.Uncommon,_=>0).Contains("BloodSpell")&&SpellCatalog.Offer(FactorRarity.Rare,_=>0).Contains("BloodSpell"),"驭血移入金因子抽取池");
  var deck=new MagicDeckState();for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);}deck.Equip(deck.Add("BloodSpell").InstanceId,ManaSlot.Draw);var restored=MagicDeckState.FromJson(deck.ToJson());MagicDeckStore.Write(player,restored);
  Check(restored.InSlot(ManaSlot.Draw)!.Id=="BloodSpell","旧魔法书ID不变，存读仍识别驭血");
  int hp=player.Creature.CurrentHp;var option=ChantCmd.Options(player,2).Single(o=>o.Slot==ManaSlot.Draw);await ChantCmd.Resolve(context,Play(New<RapidChant>()),option);
  Check(player.Creature.CurrentHp==hp-2&&Mana(PileType.Draw)==2,"改稀有度不改变普通咏唱扣血与生成魔力");
  await Reset();await Use(New<AirSlash>());Check(enemy.CurrentHp==9993&&player.Creature.HasPower<AccelerateFlowPower>()&&!player.Creature.HasPower<BanManaGainPower>(),"普通空气斩2伤3次，获得加速同调，无新禁魔");await ManaCmd.Generate(player,PileType.Hand,2);Check(Mana(PileType.Hand)==2,"空气斩之后仍可生成魔力");
  await Reset();await Use(New<AirSlash>(true));Check(enemy.CurrentHp==9991,"升级空气斩2伤4次");
  await Reset();await PowerCmd.Apply<StrengthPower>(context,player.Creature,3,player.Creature,null);await Use(New<AirSlash>(true));Check(enemy.CurrentHp==9979,"空气斩每段正常受力量加成");
  await Reset();await PowerCmd.Apply<BanManaGainPower>(context,player.Creature,1,enemy,null);await Use(New<AirSlash>());Check(player.Creature.HasPower<BanManaGainPower>(),"空气斩不会移除其他来源已有的禁魔");
  foreach(bool up in new[]{false,true})foreach(int available in new[]{0,1,3,5}) {
   await Reset();await ManaCmd.Generate(player,PileType.Draw,available);int requested=up?4:3;await Use(New<SwampSpell>(up));int moved=Math.Min(available,requested),missing=requested-moved;
   Check(Mana(PileType.Hand)==moved&&Mana(PileType.Draw)==available-moved&&player.Creature.GetPowerAmount<SwampManaNextTurnPower>()==missing,"沼泽普通/升级，抽牌堆魔力="+available+"/"+up);
   if(missing>0) {
    var delayed=player.Creature.GetPower<SwampManaNextTurnPower>()!;
    await delayed.AfterPlayerTurnStart(context,player);Check(Mana(PileType.Hand)==moved,"本回合施加的缺额不立即生成");
    delayed.AmountOnTurnStart=delayed.Amount;await delayed.AfterPlayerTurnStart(context,other);Check(delayed.Amount==missing&&Mana(PileType.Hand)==moved,"其他玩家回合不会结算自己的缺额");
    await delayed.AfterPlayerTurnStart(context,player);Check(Mana(PileType.Hand)==requested&&!player.Creature.HasPower<SwampManaNextTurnPower>(),"下个自己回合补足后移除能力");
   }
  }
  await Reset();await Use(New<SwampSpell>());await Use(New<SwampSpell>(true));Check(player.Creature.GetPowerAmount<SwampManaNextTurnPower>()==7,"两张沼泽的缺额累加");
  var pending=player.Creature.GetPower<SwampManaNextTurnPower>()!;pending.AmountOnTurnStart=3;await pending.AfterPlayerTurnStart(context,player);Check(Mana(PileType.Hand)==3&&pending.Amount==4,"仅结算本回合开始前的部分，新缺额继续等待");await pending.AfterPlayerTurnStart(context,player);Check(Mana(PileType.Hand)==3&&pending.Amount==4,"同一回合重复通知不会重复补发");
  await Reset();for(int i=0;i<10;i++)await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Hand);await ManaCmd.Generate(player,PileType.Draw,2);await Use(New<SwampSpell>());
  Check(Mana(PileType.Draw)==0&&Mana(PileType.Discard)==2&&player.Creature.GetPowerAmount<SwampManaNextTurnPower>()==1,"满手牌按原生溢入弃牌堆，缺额按实际从抽牌堆取出数计算");
  await Reset();await Use(New<SwampSpell>());pending=player.Creature.GetPower<SwampManaNextTurnPower>()!;pending.AmountOnTurnStart=pending.Amount;await PowerCmd.Apply<BanManaGainPower>(context,player.Creature,1,enemy,null);await pending.AfterPlayerTurnStart(context,player);Check(Mana(PileType.Hand)==0&&!player.Creature.HasPower<SwampManaNextTurnPower>(),"延迟生成仍遵守届时的禁止生成魔力效果");
  var lure=New<LureTheEnemyInDeep>();Check(lure.EnergyCost.Canonical==2&&lure.DynamicVars["Intensity"].IntValue==2,"诱敌深入普通版保持2费咏唱2");lure.UpgradeInternal();Check(lure.EnergyCost.GetWithModifiers(CostModifiers.Local)==1&&lure.DynamicVars["Intensity"].IntValue==1,"诱敌深入升级保持1费咏唱1");
  var export=new List<object>();foreach(var id in new[]{"AirSlash","BloodSpell","SwampSpell","LureTheEnemyInDeep"}) {
   var type=typeof(Mana).Assembly.GetType("FrierenMod.Scripts.Cards."+id)!;var c=combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)),player);LocManager.Instance.SetLanguage("zhs");var description=c.GetDescriptionForPile(PileType.None);int cost=c.EnergyCost.Canonical;var rarity=c.Rarity.ToString();if(c.MaxUpgradeLevel>0)c.UpgradeInternal();export.Add(new{model=id,cost,rarity,upgradedCost=c.EnergyCost.GetWithModifiers(CostModifiers.Local),description,upgradedDescription=c.GetDescriptionForPile(PileType.None)});
   foreach(var lang in new[]{"zhs","eng"}){LocManager.Instance.SetLanguage(lang);Check(!c.GetDescriptionForPile(PileType.None).Contains("{"),"卡面格式完整："+id+"/"+lang);}
  }
  File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/cards099/card-snapshot.json"),System.Text.Json.JsonSerializer.Serialize(export,new System.Text.Json.JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));RunManager.Instance.CleanUp();return count;
 }
}

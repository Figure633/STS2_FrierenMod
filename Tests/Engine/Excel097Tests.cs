using BaseLib.Utils;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using Godot;
using HarmonyLib;
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
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Excel097Tests
{
 public static async Task<int> Run()
 {
  int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS Excel097："+text);}
  var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
  var run=RunState.CreateForTest([player],seed:"EXCEL097");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());
  var combat=new CombatState(runState:run);combat.AddPlayer(player);
  var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
  CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
  var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var choice=CombatTestState.UseSelector(selector);
  T New<T>(bool up=false) where T:CardModel {var c=combat.CreateCard<T>(player);if(up)c.UpgradeInternal();return c;}
  CardPlay Play(CardModel c)=>CombatTestState.NewPlay(card:c,target:enemy,resultPile:PileType.Discard,resources:new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},playCount:1,playIndex:0,isAutoPlay:false);
  async Task Use(CardModel c)=>await c.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);
  int Mana(PileType pile)=>ManaCmd.InPile(player,pile).Count();
  async Task Reset(){selector.Cleanup();if(!combat.ContainsCreature(enemy)){enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);}foreach(var c in combat.Creatures){foreach(var p in c.Powers.ToArray())await PowerCmd.Remove(p);c.LoseBlockInternal(c.Block);c.SetCurrentHpInternal(c.MaxHp);}foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);CombatManager.Instance.History.Clear();}
  await ManaCmd.Generate(player,PileType.Hand,2);await ManaCmd.Generate(player,PileType.Draw,2);await ManaCmd.Generate(player,PileType.Discard,1);
  await Use(New<ManaExpansion>());Check(Mana(PileType.Hand)==2&&Mana(PileType.Draw)==4&&Mana(PileType.Discard)==2,"普通膨胀只翻倍抽弃牌堆");
  await Use(New<ManaExpansion>(true));Check(Mana(PileType.Hand)==4&&Mana(PileType.Draw)==8&&Mana(PileType.Discard)==4,"升级膨胀才包含手牌");
  await Reset();selector.PrepareToSelect(Array.Empty<int>());await Use(New<ClearMind>());Check(Mana(PileType.Hand)==2&&player.Creature.HasPower<AccelerateFlowPower>(),"加速流转可不弃牌，仍生成2魔力");
  await Reset();await ManaCmd.Generate(player,PileType.Hand,2);selector.PrepareToSelect(new[]{0});await Use(New<ClearMind>(true));Check(Mana(PileType.Hand)==4&&Mana(PileType.Discard)==1,"只选1张弃牌仍生成3魔力");
  await Reset();await Use(New<OilSpell>());Check(enemy.CurrentHp==9972&&enemy.GetPowerAmount<VulnerablePower>()==2,"油魔法11伤后施易伤，第二次16伤，易伤共2");
  await Reset();await Use(New<OilSpell>(true));Check(enemy.CurrentHp==9972&&enemy.GetPowerAmount<VulnerablePower>()==4,"升级两次各施2易伤，伤害仍11");
  await Reset();var other=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(other);other.SetMaxHpInternal(9999);other.SetCurrentHpInternal(9999);enemy.SetCurrentHpInternal(5);
  await Use(New<OilSpell>());Check(other.CurrentHp==9988&&other.GetPowerAmount<VulnerablePower>()==1,"第一轮击杀任何敌人均停止重复");combat.RemoveCreature(other);
  await Reset();await Use(New<PajamasForm>(true));await Use(New<PajamasForm>());await ManaCmd.Generate(player,PileType.Draw,1);await ManaCmd.Generate(player,PileType.Hand,1);await ManaCmd.Generate(player,PileType.Discard,1);
  var pajamas=player.Creature.GetPower<PajamasFormPower>()!;await pajamas.BeforeSideTurnEnd(context,CombatSide.Player,[player.Creature]);Check(pajamas.DamagePerMana==8&&enemy.CurrentHp==9975&&Mana(PileType.Hand)+Mana(PileType.Draw)+Mana(PileType.Discard)==0,"普通和升级睡衣叠加为每魔力8伤，三堆全消耗");
  await Reset();await ManaCmd.Generate(player,PileType.Hand,2);await ManaCmd.Generate(player,PileType.Draw,2);await Use(New<TongueTwisterSpell>());Check(player.Creature.Block==8,"绕口令每张2格挡，统计原有手牌与抽到的魔力");
  await Reset();await PowerCmd.Apply<DexterityPower>(context,player.Creature,1,player.Creature,null);await ManaCmd.Generate(player,PileType.Hand,2);await ManaCmd.Generate(player,PileType.Draw,3);await Use(New<TongueTwisterSpell>(true));Check(player.Creature.Block==20,"升级绕口令每张3格挡，逐次受敏捷影响");
  await Reset();var regular=New<PreciseChant>();var rapid=New<RapidChant>();await CardPileCmd.Add(regular,PileType.Draw);await CardPileCmd.Add(rapid,PileType.Draw);await Use(New<ReadyStance>());Check(rapid.Pile?.Type==PileType.Hand&&regular.Pile?.Type==PileType.Draw,"准备架势抽耗能更低的高速咏唱");
  await Reset();await PowerCmd.Apply<PaperPlanePower>(context,player.Creature,1,player.Creature,null);var paper=player.Creature.GetPower<PaperPlanePower>()!;
  CombatManager.Instance.History.CardPlayStarted(combat,Play(New<Mana>()));CombatManager.Instance.History.CardPlayStarted(combat,Play(New<Mana>()));
  AccessTools.Property(typeof(PlayerCombatState),nameof(PlayerCombatState.TurnNumber)).SetValue(player.PlayerCombatState,player.PlayerCombatState!.TurnNumber+1);
  Check(paper.PreviousRecord==2&&paper.CurrentRecord==0,"纸飞机从原生历史读取上回合最高2，本回合归零");
  var strike=New<StrikeFrieren>();await CardPileCmd.Add(strike,PileType.Hand);
  void Preview(int expected){strike.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,strike.DynamicVars);Check(strike.DynamicVars.Damage.PreviewValue==expected,"纸飞机原生预览="+expected);}
  Preview(6);for(int i=0;i<2;i++)CombatManager.Instance.History.CardPlayStarted(combat,Play(New<Mana>()));Preview(6);
  paper.AllowEqual=true;Preview(12);paper.AllowEqual=false;CombatManager.Instance.History.CardPlayStarted(combat,Play(New<Mana>()));Preview(12);
  CombatManager.Instance.History.CardPlayStarted(combat,Play(strike));await Use(strike);Check(enemy.CurrentHp==9987&&paper.CurrentRecord==3,"打出攻击后最高记录保留，实际伤害翻倍");
  var chantAttack=New<RapidChant>();await DamageCmd.Attack(4).FromCardCompatibility(chantAttack,Play(chantAttack)).Targeting(enemy).Execute(context);Check(enemy.CurrentHp==9979,"纸飞机沿用原生攻击判定，咏唱产生的攻击也翻倍");
  foreach(var lang in new[]{"zhs","eng"}){LocManager.Instance.SetLanguage(lang);Check(!paper.Description.GetFormattedText().Contains("{")&&!pajamas.Description.GetFormattedText().Contains("{"),"能力记录动态文本："+lang);}
  Check(!FrierenCardAvailability.IsRandomCandidate(New<ViolentExorcism>())&&FrierenCardAvailability.IsRandomCandidate(New<PaperPlaneSpell>()),"弃用驱魔保留模型，纸飞机加入正常池");
  await Reset();
  var ids=new[]{"BabySleeping","ClearMind","Confrontation","SustainedBarrage","Famehameha","ViolentExorcism","GrandCross","ManaExpansion","MoldSpell","OilSpell","PajamasForm","PancakeSpell","PaperPlaneSpell","PerfectDefensiveMagic","ReadyStance","RestraintMagic","RitesPreparation","TongueTwisterSpell","BirdCapturingSpell"};
  var export=new List<object>();
  foreach(var id in ids){var type=typeof(Mana).Assembly.GetType("FrierenMod.Scripts.Cards."+id)!;var c=combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)),player);LocManager.Instance.SetLanguage("zhs");var desc=c.GetDescriptionForPile(PileType.None);int cost=c.EnergyCost.Canonical;var keys=c.Keywords.ToArray();c.UpgradeInternal();var upgraded=c.GetDescriptionForPile(PileType.None);export.Add(new{model=id,cost,upgradedCost=c.EnergyCost.Canonical,type=c.Type.ToString(),rarity=c.Rarity.ToString(),description=desc,upgradedDescription=upgraded,keywords=keys.Select(x=>x.ToString()),upgradedKeywords=c.Keywords.Select(x=>x.ToString())});foreach(var lang in new[]{"zhs","eng"}){LocManager.Instance.SetLanguage(lang);Check(!c.GetDescriptionForPile(PileType.None).Contains("{"),"升级描述完整："+id+"/"+lang);}}
  File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo097/card-snapshot.json"),System.Text.Json.JsonSerializer.Serialize(export,new System.Text.Json.JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
  RunManager.Instance.CleanUp();return count;
 }
}

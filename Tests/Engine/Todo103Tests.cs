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
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;

public static class Todo103Tests
{
    public static async Task<int> RunFlowPreview()
    {
        int checks = 0;
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FLOW-PREVIEW");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); MegaCrit.Sts2.Core.Context.LocalContext.NetId = 1;
        var combat = new CombatState(runState: run); combat.AddPlayer(player);
        var enemy = combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(), CombatSide.Enemy, null);
        combat.AddCreature(enemy); enemy.SetMaxHpInternal(999); enemy.SetCurrentHpInternal(999);
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        var context = new ThrowingPlayerChoiceContext();
        var card = combat.CreateCard<EndlessFlow>(player); await CardPileCmd.Add(card, PileType.Hand);
        await ManaCmd.Generate(player, PileType.Draw, 7);
        void Preview(int expected) {
            card.UpdateDynamicVarPreview(CardPreviewMode.Normal, null, card.DynamicVars);
            foreach (var lang in new[] { "zhs", "eng" }) {
                LocManager.Instance.SetLanguage(lang);
                string text = card.GetDescriptionForPile(PileType.Hand);
                string plain = System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", "");
                if (decimal.Floor(card.DynamicVars.CalculatedDamage.PreviewValue) != expected ||
                    !plain.Contains(lang == "zhs" ? $"（造成{expected}点伤害）" : $"(Deal {expected} damage)"))
                    throw new Exception("生生流转预览不符：" + text);
                checks++;
            }
        }
        Preview(10);
        var strength = await PowerCmd.Apply<StrengthPower>(context, player.Creature, 2, player.Creature, null); Preview(12);
        await PowerCmd.Remove(strength!);
        var weak = await PowerCmd.Apply<WeakPower>(context, player.Creature, 1, enemy, null); Preview(7);
        await PowerCmd.Remove(weak!);
        await PowerCmd.Apply<BanManaGainPower>(context, player.Creature, 1, player.Creature, null); Preview(7);
        GD.Print("PASS 生生流转：中英提示包含生成后的魔力，并识别力量、虚弱和禁止生成魔力");
        return checks;
    }

    public static async Task<int> Run()
    {
        int count=0; void Check(bool ok,string label){if(!ok)throw new Exception(label);count++;GD.Print("PASS TODO103: "+label);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO103");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());MegaCrit.Sts2.Core.Context.LocalContext.NetId=1;
        var flow=player.Deck.Cards.OfType<Flow>().FirstOrDefault() ?? run.CreateCard<Flow>(player);
        if(flow.Pile?.Type!=PileType.Deck)await CardPileCmd.Add(flow,PileType.Deck);CardCmd.Upgrade(flow);
        var tooth=(ArchaicTooth)ModelDb.Relic<ArchaicTooth>().ToMutable();
        Check(tooth.SetupForPlayer(player),"先古之牙识别流转");
        Check(tooth.AncientCard!.Id==ModelDb.Card<EndlessFlow>().Id && tooth.AncientCard.CurrentUpgradeLevel==1,"原生升华预览保留升级");
        await RelicCmd.Obtain(tooth,player);
        Check(!player.Deck.Cards.Contains(flow) && player.Deck.Cards.Any(c=>c is EndlessFlow && c.Keywords.Contains(CardKeyword.Innate)),"原生获取将流转替换为固有生生流转");
        var tome=(DustyTome)ModelDb.Relic<DustyTome>().ToMutable();tome.SetupForPlayer(player);
        Check(tome.AncientCard==ModelDb.Card<FrierenTheSlayer>().Id,"尘封魔典排除升华牌，选择先古能力");
        await RelicCmd.Obtain(tome,player);
        Check(player.Deck.Cards.Any(c=>c is FrierenTheSlayer && c.IsUpgraded),"原生魔典发升级先古能力牌");
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemies=Enumerable.Range(0,2).Select(_=>combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null)).ToArray();
        foreach(var e in enemies){combat.AddCreature(e);e.SetMaxHpInternal(9999);e.SetCurrentHpInternal(9999);}
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var selection=CombatTestState.UseSelector(selector);
        T New<T>() where T:CardModel=>combat.CreateCard<T>(player);
        async Task Use(CardModel card){if(card.Pile==null)await CardPileCmd.Add(card,PileType.Hand);await card.OnPlayWrapper(context,enemies[0],false,new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},true);}
        async Task Reset(){selector.Cleanup();foreach(var p in player.Creature.Powers.ToArray())await PowerCmd.Remove(p);foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);foreach(var e in enemies){e.SetCurrentHpInternal(9999);e.LoseBlockInternal(e.Block);}CombatManager.Instance.History.Clear();}
        await ManaCmd.Generate(player,PileType.Hand,2);await ManaCmd.Generate(player,PileType.Draw,1);
        await Use(New<EndlessFlow>());
        Check(ManaCmd.Total(player)==6 && enemies.All(e=>e.CurrentHp==9993),"先三堆各生成魔力，再按总数6群伤");
        await Reset();await PowerCmd.Apply<BanManaGainPower>(context,player.Creature,1,player.Creature,null);await CardPileCmd.Add(New<Mana>(),PileType.Hand);await Use(New<EndlessFlow>());
        Check(ManaCmd.Total(player)==1&&enemies.All(e=>e.CurrentHp==9998),"禁止生成魔力时按现有魔力造成伤害");
        await Reset();await PowerCmd.Apply<StrengthPower>(context,player.Creature,2,player.Creature,null);await Use(New<EndlessFlow>());
        Check(enemies.All(e=>e.CurrentHp==9994),"群伤沿用原版力量加成");
        var baseCard=New<EndlessFlow>();Check(!baseCard.Keywords.Contains(CardKeyword.Innate),"普通生生流转不固有");CardCmd.Upgrade(baseCard);Check(baseCard.Keywords.Contains(CardKeyword.Innate),"升级生生流转增加固有");
        foreach(var card in new CardModel[]{New<EndlessFlow>(),New<FrierenTheSlayer>()})Check(!FrierenCardAvailability.IsRandomCandidate(card),"先古牌不进入普通随机池："+card.Id);
        await Reset();var spellPool=FrierenTheSlayerPower.SpellPool().ToArray();
        LocManager.Instance.SetLanguage("zhs");Check(spellPool.Length>20 && spellPool.All(c=>c.Title.Contains("魔法")&&c is not FactorCard and not MagicPropCard),"完整中文魔法牌池排除因子和魔具");
        var list=spellPool.Select(c=>new{model=c.GetType().Name,name=c.Title,id=c.Id.Entry}).ToArray();
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo103/spell-pool.json"),System.Text.Json.JsonSerializer.Serialize(list,new System.Text.Json.JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
        LocManager.Instance.SetLanguage("eng");Check(FrierenTheSlayerPower.SpellPool().Select(c=>c.Id).SequenceEqual(spellPool.Select(c=>c.Id)),"英文客户端牌池和顺序一致");
        int spellIndex=Array.FindIndex(spellPool,c=>c is FlyingMagic);Check(spellIndex>=0,"飞行魔法可选");
        var source=New<FrierenTheSlayer>();CardCmd.Upgrade(source);await Use(source);
        var power=player.Creature.GetPower<FrierenTheSlayerPower>()!;
        selector.PrepareToSelect(new[]{spellIndex});await power.AfterPlayerTurnStart(context,player);
        var chosen=player.PlayerCombatState!.Hand.Cards.Single();
        Check(chosen is FlyingMagic && chosen.IsUpgraded && chosen.EnergyCost.GetWithModifiers(CostModifiers.All)==3,"升级能力从完整网格获得原费用升级魔法");
        chosen.EnergyCost.EndOfTurnCleanup();Check(chosen.EnergyCost.GetWithModifiers(CostModifiers.All)==3,"选中的魔法本回合和下回合均保留原费用");
        Check(source.Keywords.Contains(CardKeyword.Innate)&&New<FrierenTheSlayer>().Keywords.Contains(CardKeyword.Innate),"普通和升级葬送的芙莉莲均固有");
        Check(power.UpgradedChoices==1,"升级来源记录一次升级选牌");
        selector.Cleanup();await power.AfterPlayerTurnStart(context,Player.CreateForNewRun<Frieren>(UnlockState.all,2));Check(player.PlayerCombatState.Hand.Cards.Count==1,"其他玩家回合不触发选牌");
        foreach(var lang in new[]{"zhs","eng"}){LocManager.Instance.SetLanguage(lang);Check(!power.Description.GetFormattedText().Contains("{"),"能力描述格式完整："+lang);foreach(var card in new CardModel[]{baseCard,source})Check(!card.GetDescriptionForPile(PileType.None).Contains("{"),"先古升级描述完整："+lang+card.Id);}
        await Reset();
        CardPlay Play(CardModel c)=>CombatTestState.NewPlay(card:c,target:null,resultPile:PileType.Exhaust,resources:new ResourceInfo{EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},playCount:1,playIndex:0,isAutoPlay:false);
        void Record(int n){for(int i=0;i<n;i++)CombatManager.Instance.History.CardPlayStarted(combat,Play(New<Mana>()));}
        void Next()=>AccessTools.Property(typeof(PlayerCombatState),nameof(PlayerCombatState.TurnNumber)).SetValue(player.PlayerCombatState,player.PlayerCombatState!.TurnNumber+1);
        Record(4);Next();Record(2);Next();
        Check(PaperPlanePower.BattleRecord(player)==4 && PaperPlanePower.TurnRecord(player)==0,"纸飞机保留更早回合4的记录，不只比较上一回合2");
        await PowerCmd.Apply<PaperPlanePower>(context,player.Creature,1,player.Creature,null);var paper=player.Creature.GetPower<PaperPlanePower>()!;
        var strike=New<StrikeFrieren>();await CardPileCmd.Add(strike,PileType.Hand);Record(3);strike.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],strike.DynamicVars);Check(strike.DynamicVars.Damage.PreviewValue==6,"未超过历史4时不翻倍");
        Record(1);paper.AllowEqual=true;strike.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],strike.DynamicVars);Check(strike.DynamicVars.Damage.PreviewValue==12,"升级追平此前最高时翻倍");paper.AllowEqual=false;
        Record(1);strike.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemies[0],strike.DynamicVars);Check(strike.DynamicVars.Damage.PreviewValue==12,"普通破历史纪录时翻倍");
        Check(New<PaperPlaneSpell>().EnergyCost.Canonical==2,"纸飞机费用改为2");
        Next();Check(PaperPlanePower.BattleRecord(player)==5,"下一回合更新此前最高为5");
        var statistics=ModelDb.AllCharacters.Select(character=>{
            var cards=character.CardPool.AllCards.Where(c=>c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare &&
                (character is not Frieren || FrierenCardAvailability.IsRandomCandidate(c))).ToArray();
            return new {character=character.Id.Entry,total=cards.Length,
                rarities=cards.GroupBy(c=>c.Rarity.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),
                types=cards.GroupBy(c=>c.Type.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),
                costs=cards.GroupBy(c=>c.EnergyCost.CostsX?"X":c.EnergyCost.Canonical.ToString()).ToDictionary(g=>g.Key,g=>g.Count())};
        }).ToArray();
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo103/card-statistics.json"),System.Text.Json.JsonSerializer.Serialize(statistics,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
        RunManager.Instance.CleanUp();return count;
    }
}

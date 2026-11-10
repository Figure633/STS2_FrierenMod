using BaseLib.Utils;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Events;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class Todo098Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS TODO098："+text);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var run=RunState.CreateForTest([player],seed:"TODO098");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        SaveManager.Instance.InitPrefsDataForTest();LocManager.Instance.SetLanguage("zhs");
        var combat=new CombatState(runState:run);combat.AddPlayer(player);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);
        enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);
        var context=new ThrowingPlayerChoiceContext();var selector=new TestCardSelector();using var choices=CombatTestState.UseSelector(selector);
        T New<T>(bool up=false)where T:CardModel{var c=combat.CreateCard<T>(player);if(up)c.UpgradeInternal();return c;}
        ResourceInfo resources=new(){EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0};
        CardPlay Play(CardModel c)=>CombatTestState.NewPlay(card:c,target:enemy,resultPile:PileType.Discard,resources:resources);
        async Task Use(CardModel c){await CardPileCmd.Add(c,PileType.Hand);await c.OnPlayWrapper(context,enemy,false,resources,true);}
        async Task Reset(){selector.Cleanup();foreach(var c in combat.Creatures){foreach(var p in c.Powers.ToArray())await PowerCmd.Remove(p);c.LoseBlockInternal(c.Block);c.SetCurrentHpInternal(c.MaxHp);}foreach(var pile in player.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);CombatManager.Instance.History.Clear();}
        var pool=ModelDb.CardPool<FrierenCardPool>().AllCards.ToArray();
        for(int i=0;i<40;i++)foreach(var type in new[]{CardType.Attack,CardType.Skill,CardType.Power}) {
            var result=CardFactory.CreateForMerchant(player,pool,type);
            Check(FrierenCardAvailability.IsRandomCandidate(result.Card),"原生商店按类型取牌无衍生牌/因子："+type+"/"+i);
        }
        foreach(var rarity in new[]{CardRarity.Common,CardRarity.Uncommon,CardRarity.Rare})for(int i=0;i<12;i++)
            Check(FrierenCardAvailability.IsRandomCandidate(CardFactory.CreateForMerchant(player,pool,rarity).Card),"商店按稀有度重载也过滤："+rarity+"/"+i);
        var shop=MerchantInventory.CreateForNormalMerchant(player);
        Check(shop.CharacterCardEntries.All(e=>FrierenCardAvailability.IsRandomCandidate(e.CreationResult!.Card)),"真实商店库存不出售寿命论和魔法因子");
        await Reset();var draw=New<RapidChant>();var discard=New<RapidChant>(true);await CardPileCmd.Add(draw,PileType.Draw);await CardPileCmd.Add(discard,PileType.Discard);
        await Use(New<ReadyStance>());Check(draw.Pile?.Type==PileType.Hand&&discard.Pile?.Type==PileType.Discard,"两堆都有高速咏唱时优先抽牌堆");
        await Use(New<ReadyStance>());Check(discard.Pile?.Type==PileType.Discard,"准备架势重做后不检索弃牌堆");
        for(int upgrade=0;upgrade<2;upgrade++)foreach(var slot in Enum.GetValues<ManaSlot>()) {
            await Reset();var deck=new MagicDeckState();for(int i=0;i<3;i++){deck.PrepareDraft(_=>0);deck.SelectInitial(deck.Draft[0]);deck.Equip(deck.Add("BuildUpBody").InstanceId,(ManaSlot)i);}MagicDeckStore.Write(player,deck);
            await ManaCmd.Generate(player,ManaCmd.Pile(slot),2);selector.PrepareToSelect(new[]{0});await Use(New<ManaBarricade>(upgrade==1));
            Check(player.Creature.Block==(upgrade==1?10:8)+(slot==ManaSlot.Draw?6:0),"魔力壁垒普通/升级，槽位"+slot+"/"+upgrade);
        }
        await Reset();await Use(New<OpenTheWaygate>());await Use(New<OpenTheWaygate>());var gate=player.Creature.GetPower<OpenTheWaygatePower>()!;
        Check(gate.RemainingOpportunities==2,"两张时空之门分别保存机会，不把门槛加成10");
        async Task ManaPlays(int n){for(int i=0;i<n;i++)await gate.AfterCardPlayed(context,Play(New<Mana>()));}
        await ManaPlays(5);Check(gate.PendingTurns==1&&gate.RemainingOpportunities==1,"首个5连获得额外回合，还剩一次5连机会");
        await gate.AfterTakingExtraTurn(player);Check(player.Creature.HasPower<OpenTheWaygatePower>()&&gate.PendingTurns==0,"第一机会使用后图标保留");
        await gate.AfterPlayerTurnStart(context,player);await ManaPlays(4);Check(!gate.ShouldTakeExtraTurn(player),"第二机会4连不提前触发");await ManaPlays(1);
        Check(gate.ShouldTakeExtraTurn(player),"第二次5连独立获得额外回合");await gate.AfterTakingExtraTurn(player);Check(!player.Creature.HasPower<OpenTheWaygatePower>(),"所有机会及额外回合耗尽才移除图标");
        await Use(New<OpenTheWaygate>(true));gate=player.Creature.GetPower<OpenTheWaygatePower>()!;await Use(New<OpenTheWaygate>());
        await ManaPlays(4);Check(gate.PendingTurns==1&&gate.RemainingOpportunities==1,"升级机会4连与普通5连各自保存");await ManaPlays(5);Check(gate.PendingTurns==2&&gate.RemainingOpportunities==0,"同回合可积累两次额外回合，不互相覆盖");
        await gate.AfterTakingExtraTurn(player);Check(gate.ShouldTakeExtraTurn(player),"消耗一次额外回合后仍保留另一次");await gate.AfterTakingExtraTurn(player);
        var a=(OpenTheWaygatePower)ModelDb.Power<OpenTheWaygatePower>().ToMutable();var b=(OpenTheWaygatePower)ModelDb.Power<OpenTheWaygatePower>().ToMutable();a.AddOpportunity(5);Check(b.RemainingOpportunities==0,"不同玩家/实例的时空门队列相互独立");
        await Reset();var model=ModelDb.Monster<SpiegelFrieren>().ToMutable();var boss=combat.CreateCreature(model,CombatSide.Enemy,null);combat.AddCreature(boss);model.SetUpForCombat();await model.AfterAddedToRoom();
        var strike=New<StrikeFrieren>();await DamageCmd.Attack(10).FromCardCompatibility(strike,Play(strike)).Targeting(boss).WithHitCount(3).Execute(context);
        Check(boss.CurrentHp==470&&boss.Block==10&&boss.GetPowerAmount<MegaCrit.Sts2.Core.Models.Powers.RitualPower>()==3,"复制体完整吃到三段10伤后统一施法，格挡不拦截中间段");
        var caster=boss.GetPower<SpellCasterPower>()!;await PowerCmd.ModifyAmount(context,boss.GetPower<EnemyManaPower>()!,170,boss,null);await caster.Resolve(context);Check(caster.SpellsCast==8&&boss.GetPowerAmount<IntangiblePower>()==1,"第一轮正常获得一次无实体");
        await PowerCmd.Remove(boss.GetPower<IntangiblePower>()!);await PowerCmd.ModifyAmount(context,boss.GetPower<EnemyManaPower>()!,200,boss,null);await caster.Resolve(context);
        Check(boss.GetPowerAmount<IntangiblePower>()==1&&caster.SpellsCast==16&&boss.GetPower<EnemyManaPower>()!.StoredMana==0,"第二轮再次给予无实体并扣50魔力");
        foreach(var m in new MonsterModel[]{ModelDb.Monster<ThievingHopper>(),ModelDb.Monster<Ovicopter>(),ModelDb.Monster<Exoskeleton>()})Check(BirdCapturingSpell.IsBird(m),"捕鸟新增作者指定目标："+m.GetType().Name);
        foreach(var lang in new[]{"zhs","eng"}) {LocManager.Instance.SetLanguage(lang);foreach(var c in new CardModel[]{New<DefensiveMagic>(true),New<FlowerFieldSpell>(true),New<FireSpell>(true),New<RedAppleSpell>(true)})Check(!c.GetDescriptionForPile(PileType.None).Contains("[green]"+""+"额外")&&!System.Text.RegularExpressions.Regex.IsMatch(c.GetDescriptionForPile(PileType.None),@"\[green\][^\[]*\+\[/green\]"),"升级衍生牌名和+号保持金色："+c.GetType().Name+"/"+lang);}
        CombatTestState.SetInProgress(false);
        var well=(AnimalWell)ModelDb.Event<AnimalWell>().ToMutable();await CombatTestState.BeginEvent(well,player);Check(well.CurrentOptions[2].HoverTips.OfType<CardHoverTip>().Any(t=>t.Card is BirdCapturingSpell),"动物井赠卡选项在选择前就有捕鸟预览");
        var food=(FoodEvent)ModelDb.Event<FoodEvent>().ToMutable();await CombatTestState.BeginEvent(food,player);Check(food.CurrentOptions[1].HoverTips.OfType<CardHoverTip>().Any(t=>t.Card is EmergencyFood),"打包选项预览应急食物");
        var mimic=(MimicFight)ModelDb.Event<MimicFight>().ToMutable();await CombatTestState.BeginEvent(mimic,player);await mimic.CurrentOptions[0].Chosen();Check(mimic.CurrentOptions[1].HoverTips.OfType<CardHoverTip>().Any(t=>t.Card is RingletForm),"宝箱怪事件预览卷发形态");
        var snapshot=new List<object>();
        foreach(var id in new[]{"ReadyStance","ManaBarricade","OpenTheWaygate","BirdCapturingSpell"}) {
            var type=typeof(Mana).Assembly.GetType("FrierenMod.Scripts.Cards."+id)!;
            var c=combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)),player);LocManager.Instance.SetLanguage("zhs");
            var description=c.GetDescriptionForPile(PileType.None);int cost=c.EnergyCost.Canonical;var keywords=c.Keywords.Select(k=>k.ToString()).ToArray();
            c.UpgradeInternal();snapshot.Add(new{model=id,cost,upgradedCost=c.EnergyCost.Canonical,description,upgradedDescription=c.GetDescriptionForPile(PileType.None),keywords,upgradedKeywords=c.Keywords.Select(k=>k.ToString()).ToArray()});
        }
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/todo-current/card-snapshot.json"),System.Text.Json.JsonSerializer.Serialize(snapshot,new System.Text.Json.JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping}));
        RunManager.Instance.CleanUp();return count;
    }
}

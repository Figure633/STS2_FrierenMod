using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Rewards;
using FrierenMod.Scripts.Shared.Compatibility;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using System.Text.Json;

public static class Cards105Tests
{
    public static async Task<int> Run(Node root)
    {
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;GD.Print("PASS 105: "+text);}
        var player=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var other=Player.CreateForNewRun<Frieren>(UnlockState.all,2);
        var run=RunState.CreateForTest([player,other],seed:"CARDS105");RunManager.Instance.SetUpTest(run,new NetSingleplayerGameService());LocalContext.NetId=1;
        var room=new CombatRoom(ModelDb.Encounter<TurretOperatorWeak>().ToMutable(),run);run.PushRoom(room);
        var combat=room.CombatState;combat.AddPlayer(player);combat.AddPlayer(other);
        var enemy=combat.CreateCreature(ModelDb.Monster<Axebot>().ToMutable(),CombatSide.Enemy,null);combat.AddCreature(enemy);enemy.SetMaxHpInternal(9999);enemy.SetCurrentHpInternal(9999);
        CombatManager.Instance.SetUpCombat(combat);CombatTestState.SetInProgress(true);var context=new ThrowingPlayerChoiceContext();
        var selector=new TestCardSelector();using var selection=CombatTestState.UseSelector(selector);
        T New<T>(bool up=false)where T:CardModel{var c=combat.CreateCard<T>(player);if(up)CardCmd.Upgrade(c);return c;}
        async Task Use(CardModel c,int x=0){if(c.Pile?.Type!=PileType.Hand)await CardPileCmd.Add(c,PileType.Hand);if(c.EnergyCost.CostsX)c.EnergyCost.CapturedXValue=x;await c.OnPlayWrapper(context,enemy,false,new ResourceInfo{EnergySpent=x,EnergyValue=x,StarsSpent=0,StarValue=0},true);}
        async Task Reset(){selector.Cleanup();foreach(var creature in combat.Creatures){foreach(var p in creature.Powers.ToArray())await PowerCmd.Remove(p);creature.LoseBlockInternal(creature.Block);creature.SetCurrentHpInternal(creature.MaxHp);}foreach(var p in new[]{player,other})foreach(var pile in p.PlayerCombatState!.AllPiles)foreach(var c in pile.Cards.ToArray())pile.RemoveInternal(c);player.PlayerCombatState!.Energy=3;CombatManager.Instance.History.Clear();}
        int Mana(PileType pile)=>ManaCmd.InPile(player,pile).Count();
        await Use(New<AirSlash>());
        Check(enemy.CurrentHp==9995&&!player.Creature.HasPower<AccelerateFlowPower>()&&player.Creature.HasPower<AccelerateFlowNextTurnPower>(),"空气斩2伤2次，本回合不加速");
        var delayed=player.Creature.GetPower<AccelerateFlowNextTurnPower>()!;await delayed.AfterPlayerTurnStart(context,other);Check(!player.Creature.HasPower<AccelerateFlowPower>(),"空气斩等待本人下回合");
        await delayed.AfterPlayerTurnStart(context,player);Check(player.Creature.HasPower<AccelerateFlowPower>()&&!player.Creature.HasPower<AccelerateFlowNextTurnPower>(),"下回合获得加速同调并移除等待状态");
        await Reset();player.Creature.GainBlockInternal(17);await Use(New<ButtSlam>(true));Check(enemy.CurrentHp==9982&&Mana(PileType.Discard)==2&&Mana(PileType.Draw)==0,"全身撞姬按17格挡单次攻击，弃牌堆生成2魔力");
        await Reset();await Use(New<ButtSlam>());Check(enemy.CurrentHp==9999&&Mana(PileType.Discard)==2,"零格挡仍生成2魔力");
        await Reset();await Use(New<Confrontation>(true));Check(player.Creature.Block==8,"对峙未咏唱时8格挡");
        await Reset();var magicDeck=new MagicDeckState();for(int i=0;i<3;i++){magicDeck.PrepareDraft(_=>0);magicDeck.SelectInitial(magicDeck.Draft[0]);}magicDeck.Equip(magicDeck.Add("AdvancedDefensiveMagic").InstanceId,ManaSlot.Draw);MagicDeckStore.Write(player,magicDeck);await ManaCmd.Generate(player,PileType.Draw,3);selector.PrepareToSelect(new[]{0});await Use(New<RapidChant>());int before=player.Creature.Block;var confrontation=New<Confrontation>(true);await CardPileCmd.Add(confrontation,PileType.Hand);Check(confrontation.ShouldGlowGold,"对峙咏唱达标时金光提示");await Use(confrontation);Check(player.Creature.Block==before+16,"升级对峙咏唱后8加8格挡");
        await Reset();await Use(New<SustainedBarrage>(true));Check(enemy.CurrentHp==9987,"持续散射回调为4伤3次");
        await Reset();await Use(New<PajamasForm>(true));await ManaCmd.Generate(player,PileType.Hand,2);await player.Creature.GetPower<PajamasFormPower>()!.BeforeSideTurnEnd(context,CombatSide.Player,[player.Creature]);Check(enemy.CurrentHp==9991&&New<PajamasForm>(true).Keywords.Contains(CardKeyword.Retain),"睡衣升级保留且仍每魔力4伤");
        await Reset();await Use(New<RestraintMagic>(true));Check(enemy.GetPowerAmount<StrengthPower>()==-15&&enemy.HasPower<SlowPower>()&&New<RestraintMagic>(true).EnergyCost.GetWithModifiers(CostModifiers.Local)==3,"拘束魔法升级3费，临时减15力量并迟缓");
        await enemy.GetPower<PiercingWailPower>()!.AfterSideTurnEnd(context,CombatSide.Enemy,[enemy]);Check(enemy.GetPowerAmount<StrengthPower>()==0,"敌方回合结束恢复临时力量");
        await Reset();int gold=player.Gold;await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Draw);await PowerCmd.Apply<WeakPower>(context,player.Creature,2,enemy,null);await PowerCmd.Apply<FrailPower>(context,player.Creature,1,enemy,null);await PowerCmd.Apply<StrengthPower>(context,player.Creature,2,player.Creature,null);await Use(New<RustCleanMagic>(true));
        Check(player.Gold==gold&&player.Creature.HasPower<WeakPower>()&&player.PlayerCombatState!.DrawPile.Cards.OfType<Mana>().Single().Enchantment is FrierenMod.Scripts.Enchantments.ManaSource {Amount:2},"除锈重做：升级生成魔力源2魔力，不再移除减益或发金币");
        gold=player.Gold;int energy=player.PlayerCombatState!.Energy;await Use(New<RustCleanMagic>());Check(player.Gold==gold&&player.PlayerCombatState.Energy==energy,"除锈重做为0费，不额外返能量");
        await Reset();await Use(New<Recycle>());await ManaCmd.Generate(player,PileType.Hand,1);await ManaCmd.Generate(player,PileType.Draw,1);var hand=ManaCmd.InPile(player,PileType.Hand).Single();var draw=ManaCmd.InPile(player,PileType.Draw).Single();await CardPileCmd.Add(hand,PileType.Discard);await CardPileCmd.Add(draw,PileType.Discard);Check(player.Creature.Block==6,"再循环统计手牌与抽牌堆魔力进入弃牌堆");
        await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Discard);await ManaCmd.Generate(player,PileType.Discard,1);Check(player.Creature.Block==6,"再循环排除非魔力和直接生成到弃牌堆");
        await CombatTestState.EndTurn(combat,CombatSide.Player,[player.Creature]);Check(player.Creature.HasPower<RecyclePower>(),"再循环作为持续能力跨回合保留");
        await Reset();await Use(New<Recycle>(true));for(int i=0;i<10;i++)await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Hand);await ManaCmd.Generate(player,PileType.Draw,1);await CardPileCmd.Add(ManaCmd.InPile(player,PileType.Draw).Single(),PileType.Hand);Check(player.Creature.Block==4&&Mana(PileType.Discard)==1,"满手牌定向取魔力溢出也触发再循环");
        await Reset();var cry=New<Crying>();await PowerCmd.Apply<DexterityPower>(context,player.Creature,1,player.Creature,null);await Use(cry);Check(player.Creature.Block==7&&enemy.GetPowerAmount<WeakPower>()==1&&cry.CurrentBlock==1&&cry.CurrentWeak==7,"嗷嗷哭1敏捷：7格挡1虚弱交换为1和7");
        await Use(cry);Check(player.Creature.Block==9&&enemy.GetPowerAmount<WeakPower>()==8&&cry.CurrentBlock==7&&cry.CurrentWeak==2,"嗷嗷哭第二次2格挡7虚弱后继续交换");
        var restored=(Crying)CardModel.FromSerializable(cry.ToSerializable());Check(restored.CurrentBlock==7&&restored.CurrentWeak==2,"嗷嗷哭当前互换数值可序列化");
        await Reset();cry=New<Crying>(true);await PowerCmd.Apply<FrailPower>(context,player.Creature,1,enemy,null);await Use(cry);Check(player.Creature.Block==6&&cry.CurrentWeak==6,"嗷嗷哭交换脆弱修正后的格挡值");
        await Reset();await Use(New<Underestimated>(true));await Use(New<Underestimated>());Check(player.Creature.GetPowerAmount<ShrinkPower>()==2&&player.Creature.GetPowerAmount<EnergyNextTurnPower>()==5&&player.Creature.GetPowerAmount<DrawCardsNextTurnPower>()==2,"真是被看扁了啊叠加缩小期限，次回合奖励叠加");
        await player.Creature.GetPower<ShrinkPower>()!.AfterSideTurnEnd(context,CombatSide.Player,[player.Creature]);Check(player.Creature.GetPowerAmount<ShrinkPower>()==1,"缩小每回合递减一次");
        await Reset();await ManaCmd.Generate(player,PileType.Hand,2);await Use(New<EnchantedStrike>(true));Check(enemy.CurrentHp==9983&&New<EnchantedStrike>().Rarity==CardRarity.Common,"附魔打击普通卡，升级8加4乘2魔力造成16伤");
        foreach(bool up in new[]{false,true})foreach(int x in new[]{0,2}){await Reset();await PowerCmd.Apply<DexterityPower>(context,player.Creature,1,player.Creature,null);await Use(New<TreasureVaultSpell>(up),x);Check(player.Creature.Block==9*x&&Mana(PileType.Draw)==x+(up?1:0),"X格挡逐次受敏捷影响，升级只多1魔力："+up+"/"+x);}
        await Reset();await RelicCmd.Obtain<ChemicalX>(player);await Use(New<TreasureVaultSpell>(true),0);Check(player.Creature.Block==16&&Mana(PileType.Draw)==3,"化学物X：0能量2次格挡及3魔力");await RelicCmd.Remove(player.GetRelic<ChemicalX>()!);
        await Reset();await Use(New<DomainExpansion>(true));var domain=player.Creature.GetPower<DomainExpansionPower>()!;await domain.AfterPlayerTurnStart(context,other);Check(!player.PlayerCombatState!.Hand.Cards.OfType<HighDensityMana>().Any(),"领域展开只响应本人回合");await domain.AfterPlayerTurnStart(context,player);Check(player.PlayerCombatState.Hand.Cards.OfType<HighDensityMana>().Single().IsUpgraded==false&&New<DomainExpansion>(true).EnergyCost.GetWithModifiers(CostModifiers.Local)==2,"领域展开升级降为2费，每回合普通高密度魔力");
        await Reset();await Use(New<SleepIn>());var sleep=player.Creature.GetPower<SleepInPower>()!;Check(player.Creature.Block==99&&player.Creature.HasPower<BarricadePower>()&&player.PlayerCombatState!.DrawPile.Cards.OfType<WakeUp>().Count()==1,"赖床99格挡、堡垒与抽牌堆起床");
        Check(!Hook.ShouldPlay(combat,New<StrikeFrieren>(),out _,AutoPlayType.None)&&Hook.ShouldPlay(combat,New<WakeUp>(),out _,AutoPlayType.None),"赖床禁止其他牌但允许起床");
        foreach(int remaining in new[]{2,3}){player.PlayerCombatState!.Energy=remaining;await sleep.BeforeSideTurnEndVeryEarly(context,CombatSide.Player,[player.Creature]);}player.PlayerCombatState!.Energy=1;
        Check(sleep.StoredEnergy==5,"赖床累计各回合结束剩余能量");await Use(New<WakeUp>());Check(player.PlayerCombatState.Energy==6&&!player.Creature.HasPower<BarricadePower>()&&!player.Creature.HasPower<SleepInPower>(),"起床返还5能量并解除限制和堡垒");await Use(New<WakeUp>());Check(player.PlayerCombatState.Energy==6,"重复起床不重复领取累计能量");
        await Reset();await Use(New<SleepIn>(true));sleep=player.Creature.GetPower<SleepInPower>()!;player.PlayerCombatState!.Energy=2;await sleep.BeforeSideTurnEndVeryEarly(context,CombatSide.Player,[player.Creature]);var wake=player.PlayerCombatState.DrawPile.Cards.OfType<WakeUp>().Single();await GameApi.Exhaust(context,wake);Check(!player.Creature.HasPower<SleepInPower>()&&player.PlayerCombatState.Energy==4,"消耗起床也解除赖床并返还能量");
        await Reset();var luck=New<GoodLuckSpell>(true);selector.PrepareToSelect(new[]{2});await Use(luck);Check(Mana(PileType.Hand)==10&&(luck.Pile==null||luck.Pile.Type==PileType.None),"变好运的魔法原生三选一，升级仅减费并填满10张手牌并作为能力移除");
        Check(New<GoodLuckSpell>().EnergyCost.Canonical==3&&New<GoodLuckSpell>(true).EnergyCost.GetWithModifiers(CostModifiers.Local)==2,"好运3费升级2费，升级不增加选项效果");
        await Reset();await PowerCmd.Apply<FrierenSuitcasePower>(context,player.Creature,1,player.Creature,null);
        await CardPileCmd.Add(New<StrikeFrieren>(),PileType.Hand);selector.PrepareToSelect(new[]{2});await Use(New<GoodLuckSpell>());
        Check(player.PlayerCombatState!.Hand.Cards.Count==13&&Mana(PileType.Hand)==12,"好运按行李箱13张上限填满且保留已有牌");
        await Reset();var originalDeck=player.Deck.Cards.ToArray();foreach(var c in originalDeck)player.Deck.RemoveInternal(c);var target=run.CreateCard<StrikeFrieren>(player);await CardPileCmd.Add(target,PileType.Deck);selector.PrepareToSelect(new[]{0});await Use(New<GoodLuckSpell>());Check(target.IsUpgraded,"好运选项永久升级随机牌组卡");
        selector.PrepareToSelect(new[]{0});await Use(New<GoodLuckSpell>());Check(player.Deck.Cards.Single()==target,"无可升级牌时不生成或替换其他卡");
        await Reset();selector.PrepareToSelect(new[]{1});await Use(New<GoodLuckSpell>());var d=MagicDeckStore.Read(player);d.RewardPity=100;MagicDeckStore.Write(player,d);
        await Hook.AfterCombatEnd(run,combat,room);player.AfterCombatEnd();other.AfterCombatEnd();await Hook.AfterCombatVictory(run,combat,room);Check(room.ExtraRewards[player].OfType<FactorReward>().Count()==2,"胜利清空能力后仍追加奖励，正常掉落和额外掉落都保留");
        int rewards=room.ExtraRewards[player].Count;await Hook.AfterCombatVictory(run,combat,room);Check(room.ExtraRewards[player].Count==rewards,"胜利重复通知不重复发奖");
        CombatTestState.SetInProgress(true);
        Check(!FrierenTheSlayerPower.SpellPool().Any(c=>c is CustomLegendarySpell or Imagination or AuraDispel or LegendaryCraftChoice or MageStyleFinisher or SecondDefensiveMagic),"葬送的芙莉莲排除作者指定六种魔法");
        Check(!FrierenCardAvailability.IsRandomCandidate(New<WakeUp>())&&!FrierenCardAvailability.IsRandomCandidate(New<ManaWish>()),"衍生起床和选项卡不进入普通获取池");
        var changed=new Type[]{typeof(AirSlash),typeof(ButtSlam),typeof(Confrontation),typeof(Famehameha),typeof(SustainedBarrage),typeof(MoldSpell),typeof(PajamasForm),typeof(Recycle),typeof(RestraintMagic),typeof(RustCleanMagic),typeof(OpenTheWaygate),typeof(ManaAffinity),typeof(Crying),typeof(Underestimated),typeof(EnchantedStrike),typeof(TreasureVaultSpell),typeof(DomainExpansion),typeof(SleepIn),typeof(WakeUp),typeof(GoodLuckSpell),typeof(StrongerWish),typeof(RewardWish),typeof(ManaWish)};
        var exports=new List<object>();
        foreach(string lang in new[]{"zhs","eng"}){
            LocManager.Instance.SetLanguage(lang);var models=new List<CardModel>();
            foreach(var type in changed){var c=combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)),player);models.Add(c);if(c.IsUpgradable){var up=combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(type)),player);CardCmd.Upgrade(up);models.Add(up);}}
            foreach(var c in models){c.UpdateDynamicVarPreview(CardPreviewMode.Normal,enemy,c.DynamicVars);var text=c.GetDescriptionForPile(PileType.Hand);Check(!text.Contains('{'),"卡面模板完整："+lang+"/"+c.Id+"/"+c.IsUpgraded);if(lang=="zhs")exports.Add(new{model=c.GetType().Name,c.Title,description=text,upgraded=c.IsUpgraded,cost=c.EnergyCost.CostsX?"X":c.EnergyCost.GetWithModifiers(CostModifiers.Local).ToString(),rarity=c.Rarity.ToString(),type=c.Type.ToString()});}
            if(OS.GetCmdlineUserArgs().Contains("--ui-snapshot")){
                int page=0;foreach(var batch in models.Chunk(10)){
                    var canvas=new Control();root.AddChild(canvas);canvas.AddChild(new ColorRect{Size=new(1920,1080),Color=new("17202c")});
                    for(int i=0;i<batch.Length;i++){var node=GD.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();AccessTools.Property(typeof(NCard),nameof(NCard.Model)).SetValue(node,batch[i]);canvas.AddChild(node);node.Position=new(205+(i%5)*370,270+(i/5)*520);node.Scale=Vector2.One*.85f;node.UpdateVisuals(PileType.None,CardPreviewMode.Normal);}
                    for(int i=0;i<3;i++)await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
                    foreach(var c in canvas.GetChildren().OfType<NCard>()){var label=c.GetNode<RichTextLabel>("%DescriptionLabel");Check(label.GetContentHeight()<=label.Size.Y+2,"卡面排版无溢出："+lang+"/"+c.Model!.Id);}
                    root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath($"res://../../artifacts/card-work/cards-{lang}-{page++}.png"));canvas.QueueFree();await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
                }
            }
        }
        var options=new JsonSerializerOptions{WriteIndented=true,Encoder=System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-work/card-snapshot.json"),JsonSerializer.Serialize(exports,options));
        var pool=ModelDb.CardPool<FrierenCardPool>().AllCards.Where(FrierenCardAvailability.IsRandomCandidate).ToArray();
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-work/statistics.json"),JsonSerializer.Serialize(new{total=pool.Length,rarities=pool.GroupBy(c=>c.Rarity.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),types=pool.GroupBy(c=>c.Type.ToString()).ToDictionary(g=>g.Key,g=>g.Count()),costs=pool.GroupBy(c=>c.EnergyCost.CostsX?"X":c.EnergyCost.Canonical.ToString()).ToDictionary(g=>g.Key,g=>g.Count())},options));
        LocManager.Instance.SetLanguage("zhs");File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-work/spell-pool.json"),JsonSerializer.Serialize(FrierenTheSlayerPower.SpellPool().Select(c=>new{model=c.GetType().Name,name=c.Title,id=c.Id.Entry}),options));
        File.WriteAllText(ProjectSettings.GlobalizePath("res://../../artifacts/card-work/power-icons.json"),JsonSerializer.Serialize(typeof(Mana).Assembly.GetTypes().Where(t=>!t.IsAbstract&&typeof(FrierenPower).IsAssignableFrom(t)).Select(t=>(FrierenPower)ModelDb.GetById<PowerModel>(ModelDb.GetId(t))).Select(p=>new{model=p.GetType().Name,name=p.Title.GetFormattedText(),icon=p.CustomPackedIconPath,bigIcon=p.CustomBigIconPath}),options));
        RunManager.Instance.CleanUp();return count;
    }
}

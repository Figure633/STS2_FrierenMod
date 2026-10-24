using BaseLib.Patches.Content;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
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
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using RitualPower = MegaCrit.Sts2.Core.Models.Powers.RitualPower;

public static class SpiegelBossTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS 复制体Boss：" + text); }
        AddActContent.Patch(new Harmony("frieren.tests.boss.act_content"));
        Check(ModelDb.Act<Glory>().AllBossEncounters.Any(e => e is SpiegelFrierenEncounter) && !ModelDb.Act<Overgrowth>().AllBossEncounters.Any(e => e is SpiegelFrierenEncounter),
            "通过BaseLib进入第三幕原生Boss池，第一幕不出现");
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-SPIEGEL-BOSS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1; SaveManager.Instance.InitPrefsDataForTest();
        player.Creature.SetMaxHpInternal(5000); player.Creature.SetCurrentHpInternal(5000);
        var encounter = ModelDb.Encounter<SpiegelFrierenEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
        var combat = new CombatState(encounter, run); combat.AddPlayer(player);
        var monster = (SpiegelFrieren)encounter.MonstersWithSlots.Single().Item1;
        var boss = combat.CreateCreature(monster, CombatSide.Enemy, "Boss"); combat.AddCreature(boss);
        CombatManager.Instance.SetUpCombat(combat); AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
        monster.RegisterSceneConversions(); var visual = monster.CreateVisuals(); root.AddChild(visual);
        Check(visual.Bounds.Size == new Vector2(250,352) && encounter.Slots.SequenceEqual(new[] { "Boss","Bird1","Bird2" }), "原图比例、血条范围和召唤槽位正确"); visual.QueueFree();
        Check(encounter.MapNodeAssetPaths.All(path => ResourceLoader.Exists(path)) && GD.Load<AudioStream>("res://FrierenMod/audio/Frieren_The_Slayer.mp3").GetLength() > 0,
            "一代Boss地图图标、轮廓和专属音乐可加载");
        monster.RollMove([player.Creature]); await monster.AfterAddedToRoom();
        Check(boss.MaxHp == 500 && monster.NextMove.Id == "MANA_SUPPRESSION" && boss.GetPower<EnemyManaPower>()!.StoredMana == 0,
            "500生命、零魔力和初始魔力压制");
        var context = new BlockingPlayerChoiceContext();
        await CreatureCmd.GainBlock(player.Creature, 7, ValueProp.Unpowered, null);
        Check(boss.Block == 7, "原生格挡钩子复制玩家获得的格挡");
        boss.LoseBlockInternal(boss.Block);
        await CreatureCmd.Damage(context, boss, 10, ValueProp.Unpowered, player.Creature, null);
        Check(boss.CurrentHp == 490 && boss.HasPower<BarricadePower>() && boss.Block == 10 && boss.GetPower<EnemyManaPower>()!.StoredMana == 0,
            "实际受伤积累魔力，10点自动触发第一种法术且准确支付");
        var caster = boss.GetPower<SpellCasterPower>()!;
        await PowerCmd.ModifyAmount(context, boss.GetPower<EnemyManaPower>()!, 190, boss, null); await caster.Resolve(context);
        Check(caster.SpellsCast == 8 && boss.GetPower<EnemyManaPower>()!.StoredMana == 0 && boss.GetPower<RitualPower>()!.Amount == 5 && boss.HasPower<ArtifactPower>(),
            "连续足量魔力按顺序结算第一轮八种法术，余量保留");
        Check(player.PlayerCombatState!.DrawPile.Cards.OfType<Burn>().Count() == 2 && player.Creature.GetPowerAmount<WeakPower>() == 2 &&
            boss.GetPowerAmount<BufferPower>() == 1 && boss.HasPower<IntangiblePower>() && combat.Enemies.Count(c => c.Monster is DampCultist) == 1,
            "灼伤、减益、防御、无实体和原生召唤生效");
        foreach (var language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            Check(!caster.Description.GetFormattedText().Contains("LOC_ERR") && !boss.Name.Contains("FRIERENMOD-") &&
                !boss.GetPower<EnemyManaPower>()!.Description.GetFormattedText().Contains("LOC_ERR"), language + "法术清单与魔力文本可显示");
        }
        await PowerCmd.ModifyAmount(context, boss.GetPower<EnemyManaPower>()!, 200, boss, null); await caster.Resolve(context);
        Check(caster.SpellsCast == 16 && boss.GetPower<SpellCasterPower>() == null && boss.GetPower<EnemyManaPower>()!.StoredMana == 0 &&
            combat.Enemies.Count(c => c.Monster is DampCultist) == 2, "只执行两轮共16种法术，准确消耗总计400魔力");
        // 检查随机压制顺序与后续递增攻击，不用等待完整十几个回合。
        monster.ResetStateMachine(); monster.SetUpForCombat(); monster.RollMove([player.Creature]);
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        Check(player.Creature.HasPower<BanManaGainPower>() && monster.NextMove.Id == "OFFENSIVE_MAGIC", "首回合压制后接普通攻击");
        for(int i=0;i<3;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        for(int i=0;i<4;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Discard);
        for(int i=0;i<2;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Hand);
        var seen = new HashSet<string>();
        for (int i=0;i<3;i++) {
            await monster.PerformMove(); monster.RollMove([player.Creature]); seen.Add(monster.NextMove.Id);
            await monster.PerformMove(); monster.RollMove([player.Creature]);
        }
        Check(seen.SetEquals(["MANA_POLLUTION","UPSIDE_DOWN","CHANT_SUPPRESSION"]) && monster.NextMove.Id == "OFFENSIVE_MAGIC",
            "三种压制随机不重复，各穿插一次攻击");
        Check(ManaCmd.InPile(player,PileType.Draw).Count()==0 && ManaCmd.InPile(player,PileType.Discard).Count()==2 && ManaCmd.InPile(player,PileType.Hand).Count()==2,
            "魔力污染先抽牌堆后弃牌堆，只替换五张，保留手牌魔力");
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        int repeat = ((MultiAttackIntent)monster.NextMove.Intents.Single()).Repeats;
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        Check(repeat == 2 && ((MultiAttackIntent)monster.NextMove.Intents.Single()).Repeats == 3, "后期多重攻击从两次起逐次递增");
        boss.SetCurrentHpInternal(110); boss.LoseBlockInternal(boss.Block);
        foreach(var power in new PowerModel?[] { boss.GetPower<IntangiblePower>(), boss.GetPower<BufferPower>() }) if(power!=null) await PowerCmd.Remove(power);
        await CreatureCmd.Damage(context, boss, 10, ValueProp.Unpowered, player.Creature, null);
        Check(monster.NextMove.Id == "APEX_PREPARATION", "100生命立即改为全力准备意图");
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        var intent = (MultiAttackIntent)monster.NextMove.Intents.Single();
        Check(!boss.HasPower<IntangiblePower>() && intent.Repeats == 999, "准备阶段移除无实体，下一回合999次攻击");
        var curse = combat.CreateCard<UpsideDown>(player); await CardPileCmd.Add(curse, PileType.Hand);
        foreach(var card in player.PlayerCombatState.DrawPile.Cards.Concat(player.PlayerCombatState.DiscardPile.Cards).ToArray()) card.RemoveFromCurrentPile();
        var draw = combat.CreateCard<StrikeFrieren>(player); var discard = combat.CreateCard<DefendFrieren>(player);
        await CardPileCmd.Add(draw, PileType.Draw); await CardPileCmd.Add(discard, PileType.Discard);
        var hp = player.Creature.CurrentHp;
        var play = new CardPlay { Card = combat.CreateCard<Mana>(player), Target=null, ResultPile=PileType.Discard, IsAutoPlay=false, PlayIndex=0, PlayCount=1,
            Resources=new ResourceInfo { EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0 } };
        await curse.AfterCardPlayed(context, play);
        Check(draw.Pile?.Type == PileType.Discard && discard.Pile?.Type == PileType.Draw && player.Creature.CurrentHp == hp-2,
            "颠倒在手牌中同调时交换原卡对象并失去2生命");
        await monster.AfterDeath(context, boss, false, 0);
        Check(combat.Enemies.All(c => c.Monster is not DampCultist || !c.IsAlive), "Boss死亡时召唤物离场");
        RunManager.Instance.CleanUp();
        var hardPlayer=Player.CreateForNewRun<Frieren>(UnlockState.all,1);
        var hardRun=RunState.CreateForTest([hardPlayer],ascensionLevel:9,seed:"SPIEGEL-HARD");
        RunManager.Instance.SetUpTest(hardRun,new NetSingleplayerGameService());LocalContext.NetId=1;
        var hardCombat=new CombatState(ModelDb.Encounter<SpiegelFrierenEncounter>().ToMutable(),hardRun);hardCombat.AddPlayer(hardPlayer);
        var hardMonster=(SpiegelFrieren)ModelDb.Monster<SpiegelFrieren>().ToMutable();var hardBoss=hardCombat.CreateCreature(hardMonster,CombatSide.Enemy,"Boss");hardCombat.AddCreature(hardBoss);
        CombatManager.Instance.SetUpCombat(hardCombat);AccessTools.Property(typeof(CombatManager),nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance,true);
        await hardMonster.AfterAddedToRoom();
        await PowerCmd.ModifyAmount(context,hardBoss.GetPower<EnemyManaPower>()!,130,hardBoss,null);await hardBoss.GetPower<SpellCasterPower>()!.Resolve(context);
        Check(hardMonster.Enhanced && hardBoss.GetPowerAmount<RitualPower>()==8 && hardBoss.GetPowerAmount<BufferPower>()==2 && hardBoss.GetPowerAmount<SpiegelMalleablePower>()==2,
            "二代敌人强化档正确启用一代高进阶仪式、缓冲及柔韧");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return count;
    }
}

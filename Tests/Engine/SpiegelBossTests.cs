using FrierenMod.Scripts.Shared.Compatibility;
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
        var cachedAct = ModelDb.Act<Glory>();
        foreach (var character in ModelDb.AllCharacters.Where(c => c is not Frieren)) {
            var other = Player.CreateForNewRun(character, UnlockState.all, 1);
            var otherRun = RunState.CreateForTest([other], seed: "OTHER-BOSS");
            RunManager.Instance.SetUpTest(otherRun, new NetSingleplayerGameService());
            Check(!cachedAct.AllBossEncounters.Any(e => e is SpiegelFrierenEncounter), character.Id + "的Boss池排除复制体");
            var savedAct = cachedAct.ToMutable();
            savedAct.SetBossEncounter(ModelDb.Encounter<SpiegelFrierenEncounter>());
            savedAct.ValidateRoomsAfterLoad(otherRun.Rng.UpFront);
            Check(savedAct.BossEncounter is not SpiegelFrierenEncounter, "其他角色旧存档替换复制体Boss");
            savedAct.SetSecondBossEncounter(ModelDb.Encounter<SpiegelFrierenEncounter>());
            savedAct.ValidateRoomsAfterLoad(otherRun.Rng.UpFront);
            Check(savedAct.SecondBossEncounter is not SpiegelFrierenEncounter && savedAct.SecondBossEncounter?.Id != savedAct.BossEncounter.Id, "旧存档第二Boss替换且不重复");
            var events = new EventModel[] { ModelDb.Event<FrierenMod.Scripts.Events.FoodEvent>(), ModelDb.Event<FrierenMod.Scripts.Events.KraftGift>(), ModelDb.Event<FrierenMod.Scripts.Events.AnimalWell>(), ModelDb.Event<FrierenMod.Scripts.Events.MimicFight>() };
            for (int act = 0; act < 3; act++) {
                AccessTools.Property(typeof(RunState), nameof(RunState.CurrentActIndex)).SetValue(otherRun, act);
                Check(events.All(e => !e.IsAllowed(otherRun)), "其他角色第" + (act + 1) + "幕排除芙莉莲事件");
            }
            RunManager.Instance.CleanUp();
        }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "FRIEREN-SPIEGEL-BOSS");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1; SaveManager.Instance.InitPrefsDataForTest();
        Check(ModelDb.Act<Glory>().AllBossEncounters.Any(e => e is SpiegelFrierenEncounter) && !ModelDb.Act<Overgrowth>().AllBossEncounters.Any(e => e is SpiegelFrierenEncounter),
            "通过BaseLib进入第三幕原生Boss池，第一幕不出现");
        player.Creature.SetMaxHpInternal(5000); player.Creature.SetCurrentHpInternal(5000);
        var encounter = ModelDb.Encounter<SpiegelFrierenEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
        var combat = new CombatState(encounter, run); combat.AddPlayer(player);
        var monster = (SpiegelFrieren)encounter.MonstersWithSlots.Single().Item1;
        var boss = combat.CreateCreature(monster, CombatSide.Enemy, "Boss"); combat.AddCreature(boss);
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        monster.RegisterSceneConversions(); var visual = monster.CreateVisuals(); root.AddChild(visual);
        Check(visual.Bounds.Size.IsEqualApprox(new Vector2(275,387.2f)) && encounter.Slots.SequenceEqual(new[] { "Boss","Bird1","Bird2" }), "放大10%后的血条范围和召唤槽位正确"); visual.QueueFree();
        Check(encounter.MapNodeAssetPaths.All(path => ResourceLoader.Exists(path)) && GD.Load<AudioStream>("res://FrierenMod/audio/bgm/frieren_slayer.ogg").GetLength() > 0,
            "一代Boss地图图标、轮廓和专属音乐可加载");
        monster.RollMove([player.Creature]); await monster.AfterAddedToRoom();
        Check(boss.MaxHp == 500 && monster.NextMove.Id == "MANA_SUPPRESSION" && boss.GetPower<EnemyManaPower>()!.StoredMana == 0,
            "500生命、零魔力和初始魔力压制");
        var context = new BlockingPlayerChoiceContext();
        await CreatureCmd.GainBlock(player.Creature, 7, ValueProp.Unpowered, null);
        Check(boss.Block == 7, "原生格挡钩子复制玩家获得的格挡");
        boss.LoseBlockInternal(boss.Block);
        await GameApi.Damage(context, boss, 10, ValueProp.Unpowered, player.Creature, null, null);
        Check(boss.CurrentHp == 490 && boss.HasPower<BarricadePower>() && boss.Block == 10 && boss.GetPower<EnemyManaPower>()!.StoredMana == 0,
            "实际受伤积累魔力，10点自动触发第一种法术且准确支付");
        var caster = boss.GetPower<SpellCasterPower>()!;
        await PowerCmd.ModifyAmount(context, boss.GetPower<EnemyManaPower>()!, 190, boss, null); await caster.Resolve(context);
        Check(caster.SpellsCast == 8 && boss.GetPower<EnemyManaPower>()!.StoredMana == 0 && boss.GetPower<RitualPower>()!.Amount == 3 && boss.HasPower<ArtifactPower>(),
            "连续足量魔力按顺序结算第一轮八种法术，余量保留");
        Check(player.PlayerCombatState!.DrawPile.Cards.OfType<Burn>().Count() == 2 && player.Creature.GetPowerAmount<WeakPower>() == 2 &&
            boss.GetPowerAmount<BufferPower>() == 1 && !boss.HasPower<MetallicizePower>() && !boss.HasPower<SpiegelMalleablePower>() && boss.HasPower<IntangiblePower>() && combat.Enemies.Count(c => c.Monster is DampCultist) == 1,
            "灼伤、减益、防御、无实体和原生召唤生效");
        foreach (var language in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(language);
            Check(!caster.Description.GetFormattedText().Contains("LOC_ERR") && !boss.Name.Contains("FRIERENMOD-") &&
                !boss.GetPower<EnemyManaPower>()!.Description.GetFormattedText().Contains("LOC_ERR"), language + "法术清单与魔力文本可显示");
        }
        await PowerCmd.ModifyAmount(context, boss.GetPower<EnemyManaPower>()!, 200, boss, null); await caster.Resolve(context);
        Check(caster.SpellsCast == 16 && boss.GetPower<SpellCasterPower>() == null && boss.GetPower<EnemyManaPower>()!.StoredMana == 0 &&
            boss.GetPowerAmount<IntangiblePower>()==2 && combat.Enemies.Count(c => c.Monster is DampCultist) == 2, "两轮各施放一次无实体并各扣50魔力");
        // 检查随机压制顺序与后续递增攻击，不用等待完整十几个回合。
        monster.ResetStateMachine(); monster.SetUpForCombat(); monster.RollMove([player.Creature]);
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        Check(player.Creature.HasPower<BanManaGainPower>() && monster.NextMove.Id == "OFFENSIVE_MAGIC", "首回合压制后接普通攻击");
        foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        player.Creature.LoseBlockInternal(player.Creature.Block);
        Check(((SingleAttackIntent)monster.NextMove.Intents.Single()).GetTotalDamage([player.Creature],boss)==18,"低进阶普通攻击意图18伤害");
        for(int i=0;i<3;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Draw);
        for(int i=0;i<4;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Discard);
        for(int i=0;i<2;i++) await CardPileCmd.Add(combat.CreateCard<Mana>(player), PileType.Hand);
        var seen = new HashSet<string>();
        for (int i=0;i<3;i++) {
            int before=player.Creature.CurrentHp;await monster.PerformMove(); monster.RollMove([player.Creature]); seen.Add(monster.NextMove.Id);
            Check(player.Creature.CurrentHp==before-18,"低进阶普通攻击实际18伤害");
            await monster.PerformMove(); monster.RollMove([player.Creature]);
        }
        Check(seen.SetEquals(["MANA_POLLUTION","UPSIDE_DOWN","CHANT_SUPPRESSION"]) && monster.NextMove.Id == "OFFENSIVE_MAGIC",
            "三种压制随机不重复，各穿插一次攻击");
        Check(ManaCmd.InPile(player,PileType.Draw).Count()==0 && ManaCmd.InPile(player,PileType.Discard).Count()==4 && ManaCmd.InPile(player,PileType.Hand).Count()==2,
            "低进阶魔力污染最多替换三张，保留手牌魔力");
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        for(int i=0;i<6;i++) {
            var multi=(MultiAttackIntent)monster.NextMove.Intents.Single();int hits=2+i/2;
            Check(multi.Repeats==hits&&multi.GetTotalDamage([player.Creature],boss)==10*hits,"低进阶第"+(i+1)+"次多重意图");
            int before=player.Creature.CurrentHp;await monster.PerformMove();monster.RollMove([player.Creature]);
            Check(player.Creature.CurrentHp==before-10*hits,"低进阶多重实际结算第"+(i+1)+"次");
        }
        boss.SetCurrentHpInternal(110); boss.LoseBlockInternal(boss.Block);
        foreach(var power in new PowerModel?[] { boss.GetPower<IntangiblePower>(), boss.GetPower<BufferPower>() }) if(power!=null) await PowerCmd.Remove(power);
        await GameApi.Damage(context, boss, 10, ValueProp.Unpowered, player.Creature, null, null);
        Check(monster.NextMove.Id == "APEX_MAGIC" && ((MultiAttackIntent)monster.NextMove.Intents.Single()).Repeats == 999, "100生命立即显示999乘999攻击意图");
        foreach(var power in player.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        player.Creature.SetMaxHpInternal(2000);player.Creature.SetCurrentHpInternal(2000);player.Creature.LoseBlockInternal(player.Creature.Block);
        await PowerCmd.Apply<BufferPower>(context,player.Creature,998,player.Creature,null);
        int apexHp=player.Creature.CurrentHp;
        await monster.PerformMove(); monster.RollMove([player.Creature]);
        var intent = (MultiAttackIntent)monster.NextMove.Intents.Single();
        Check(!boss.Powers.Any() && boss.Block == 0 && !player.Creature.HasPower<BufferPower>() && player.Creature.CurrentHp==apexHp-999, "清空能力和格挡的同回合攻击999次，998层缓冲抵挡后最后一次恰好造成999伤害");
        var curse = combat.CreateCard<UpsideDown>(player); await CardPileCmd.Add(curse, PileType.Hand);
        foreach(var card in player.PlayerCombatState.DrawPile.Cards.Concat(player.PlayerCombatState.DiscardPile.Cards).ToArray()) card.RemoveFromCurrentPile();
        var draw = combat.CreateCard<StrikeFrieren>(player); var discard = combat.CreateCard<DefendFrieren>(player);
        await CardPileCmd.Add(draw, PileType.Draw); await CardPileCmd.Add(discard, PileType.Discard);
        var hp = player.Creature.CurrentHp;
        var play = CombatTestState.NewPlay(card: combat.CreateCard<Mana>(player), target: null, resultPile: PileType.Discard, isAutoPlay: false, playIndex: 0, playCount: 1, resources: new ResourceInfo { EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0 });
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
        CombatManager.Instance.SetUpCombat(hardCombat);CombatTestState.SetInProgress(true);
        await hardMonster.AfterAddedToRoom();
        await PowerCmd.ModifyAmount(context,hardBoss.GetPower<EnemyManaPower>()!,130,hardBoss,null);await hardBoss.GetPower<SpellCasterPower>()!.Resolve(context);
        Check(hardMonster.Enhanced && hardBoss.GetPowerAmount<RitualPower>()==5 && hardBoss.GetPowerAmount<BufferPower>()==2 && !hardBoss.HasPower<SpiegelMalleablePower>() && !hardBoss.HasPower<MetallicizePower>(),
            "高进阶SP只获得2层缓冲，不获得柔韧或金属化");
        hardPlayer.Creature.SetMaxHpInternal(5000);hardPlayer.Creature.SetCurrentHpInternal(5000);
        foreach(var power in hardPlayer.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        hardMonster.RollMove([hardPlayer.Creature]);await hardMonster.PerformMove();hardMonster.RollMove([hardPlayer.Creature]);
        Check(((SingleAttackIntent)hardMonster.NextMove.Intents.Single()).GetTotalDamage([hardPlayer.Creature],hardBoss)==25,"高进阶普通攻击保持25伤害");
        for(int i=0;i<3;i++)await CardPileCmd.Add(hardCombat.CreateCard<Mana>(hardPlayer),PileType.Draw);
        for(int i=0;i<4;i++)await CardPileCmd.Add(hardCombat.CreateCard<Mana>(hardPlayer),PileType.Discard);
        for(int i=0;i<2;i++)await CardPileCmd.Add(hardCombat.CreateCard<Mana>(hardPlayer),PileType.Hand);
        for(int i=0;i<3;i++){
            int before=hardPlayer.Creature.CurrentHp;await hardMonster.PerformMove();hardMonster.RollMove([hardPlayer.Creature]);
            Check(hardPlayer.Creature.CurrentHp==before-25,"高进阶普通攻击实际25伤害");
            await hardMonster.PerformMove();hardMonster.RollMove([hardPlayer.Creature]);
        }
        Check(ManaCmd.InPile(hardPlayer,PileType.Draw).Count()==0&&ManaCmd.InPile(hardPlayer,PileType.Discard).Count()==2&&ManaCmd.InPile(hardPlayer,PileType.Hand).Count()==2,"高进阶污染最多5张并保留手牌");
        await hardMonster.PerformMove();hardMonster.RollMove([hardPlayer.Creature]);
        foreach(var power in hardPlayer.Creature.Powers.ToArray())await PowerCmd.Remove(power);
        for(int i=0;i<6;i++){
            var multi=(MultiAttackIntent)hardMonster.NextMove.Intents.Single();int hits=2+i/2;
            Check(multi.Repeats==hits&&multi.GetTotalDamage([hardPlayer.Creature],hardBoss)==12*hits,"高进阶第"+(i+1)+"次多重意图");
            int before=hardPlayer.Creature.CurrentHp;await hardMonster.PerformMove();hardMonster.RollMove([hardPlayer.Creature]);
            Check(hardPlayer.Creature.CurrentHp==before-12*hits,"高进阶多重实际结算第"+(i+1)+"次");
        }
        foreach(bool upgraded in new[]{false,true}){
            foreach(var pile in new[]{PileType.Draw,PileType.Hand,PileType.Discard})foreach(var card in CardPile.Get(pile,hardPlayer).Cards.ToArray())await CardPileCmd.RemoveFromCombat(card);
            hardPlayer.Creature.LoseBlockInternal(hardPlayer.Creature.Block);
            if(upgraded)await PowerCmd.Apply<DexterityPower>(context,hardPlayer.Creature,1,hardPlayer.Creature,null);
            for(int i=0;i<2;i++)await CardPileCmd.Add(hardCombat.CreateCard<Mana>(hardPlayer),PileType.Hand);
            for(int i=0;i<(upgraded?3:2);i++)await CardPileCmd.Add(hardCombat.CreateCard<Mana>(hardPlayer),PileType.Draw);
            var tongue=hardCombat.CreateCard<TongueTwisterSpell>(hardPlayer);if(upgraded)CardCmd.Upgrade(tongue);
            await tongue.OnPlayWrapper(context,null,false,new ResourceInfo{EnergySpent=1,EnergyValue=1,StarsSpent=0,StarValue=0},true);
            Check(tongue.DynamicVars.Block.BaseValue==(upgraded?3:2)&&tongue.DynamicVars.Cards.BaseValue==(upgraded?3:2),"绕口令仅削格挡，保持抽牌："+upgraded);
            Check(hardPlayer.Creature.Block==(upgraded?20:8),"绕口令逐张受敏捷修正："+upgraded);
        }
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp(); return count;
    }
}

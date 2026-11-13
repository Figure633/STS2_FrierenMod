using FrierenMod.Scripts.Shared.Compatibility;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Powers;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.UI;
using Godot;
using MegaCrit.Sts2.Core.Models.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

public static class AuraBossTests
{
    public static async Task<int> RunActions()
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 阿乌拉行动：" + text); }
        SaveManager.Instance.InitPrefsDataForTest();
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "AURA-COORDINATION");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        var encounter = ModelDb.Encounter<AuraEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
        var combat = new CombatState(encounter, run); combat.AddPlayer(player);
        foreach (var (model, slot) in encounter.MonstersWithSlots) combat.AddCreature(combat.CreateCreature(model, CombatSide.Enemy, slot));
        CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
        foreach (var enemy in combat.Enemies.ToArray()) { enemy.Monster!.RollMove([player.Creature]); await enemy.Monster.AfterAddedToRoom(); }
        var aura = (Aura)combat.Enemies.Single(c => c.Monster is Aura).Monster!;
        var soldiers = combat.Enemies.Where(c => c.Monster is AuraSoldier).ToArray();
        var context = new ThrowingPlayerChoiceContext();
        void Set(string field, object value) => AccessTools.Field(typeof(Aura), field).SetValue(aura, value);
        Set("_meditateNext", true); aura.RefreshOrdinaryIntent();
        foreach (var soldier in soldiers) soldier.Monster!.RollMove([player.Creature]);
        Check(aura.NextMove.Id == "MEDITATE" && soldiers.All(c => c.Monster!.NextMove.Id == "STRIKE"), "冥想时两名士兵实际意图都是攻击");
        int hp = player.Creature.CurrentHp;
        foreach (var soldier in soldiers) await soldier.Monster!.PerformMove();
        Check(player.Creature.CurrentHp == hp - 16, "两名士兵均实际攻击");
        Set("_actions", 2); aura.RefreshOrdinaryIntent(); aura.NoticeDispel();
        Check(aura.NextMove.Id == "TAUNT", "即使是第三回合，首次解除魔法也立即嘲讽");
        await aura.PerformMove(); aura.RollMove([player.Creature]);
        Set("_tauntUsed", false); Set("_tauntPending", false); Set("_verdict", "GUILLOTINE"); aura.RefreshOrdinaryIntent();
        var dispel = combat.CreateCard<AuraDispel>(player); await CardPileCmd.Add(dispel, PileType.Hand);
        await dispel.OnPlayWrapper(context, soldiers[0], false,
            new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, true);
        Check(soldiers[0].IsDead && aura.NextMove.Id == "GUILLOTINE", "断头台期间解除魔法只杀士兵，保留攻击");
        hp = player.Creature.CurrentHp; await aura.PerformMove(); aura.RollMove([player.Creature]);
        Check(player.Creature.CurrentHp == hp - 25 && aura.NextMove.Id != "TAUNT", "断头台照常结算25伤害，后续不补嘲讽");
        Set("_meditateNext", true); aura.RefreshOrdinaryIntent();
        Check(aura.NextMove.Id == "BARRAGE", "仅剩一名士兵时不再冥想");
        aura.NoticeDispel(); Check(aura.NextMove.Id != "TAUNT", "首次嘲讽机会已经永久消耗");
        var parcel = combat.CreateCard<ManaParcel>(player); CardCmd.Upgrade(parcel);
        parcel.EnergyCost.AddThisCombat(3); await CardPileCmd.Add(parcel, PileType.Hand);
        Check(parcel.EnergyCost.GetWithModifiers(CostModifiers.All) < 0 && !parcel.CanPlay(), "升级魔力包裹加费后仍隐藏费用且不可打出");
        await parcel.AfterCardDrawn(context, parcel, false);
        Check(parcel.Pile?.Type == PileType.Exhaust && ManaCmd.InPile(player, PileType.Hand).Count() == 4, "魔力包裹仍正常消耗并生成4张魔力");
        RunManager.Instance.CleanUp();
        checks += await RunDeathIntentChecks();
        return checks;
    }

    private static async Task<int> RunDeathIntentChecks()
    {
        int checks = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; GD.Print("PASS 阿乌拉死亡意图：" + text); }
        foreach (var (planned, summonAvailable) in new[] { ("BARRAGE", true), ("MEDITATE", true), ("GUILLOTINE", true), ("BARRAGE", false) })
        {
            var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
            var run = RunState.CreateForTest([player], seed: "AURA-DEATH-INTENT");
            RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
            var encounter = ModelDb.Encounter<AuraEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
            var combat = new CombatState(encounter, run); combat.AddPlayer(player);
            foreach (var (model, slot) in encounter.MonstersWithSlots) combat.AddCreature(combat.CreateCreature(model, CombatSide.Enemy, slot));
            CombatManager.Instance.SetUpCombat(combat); CombatTestState.SetInProgress(true);
            foreach (var enemy in combat.Enemies.ToArray()) { enemy.Monster!.RollMove([player.Creature]); await enemy.Monster.AfterAddedToRoom(); }
            var aura = (Aura)combat.Enemies.Single(c => c.Monster is Aura).Monster!;
            var soldiers = combat.Enemies.Where(c => c.Monster is AuraSoldier).ToArray();
            void Set(string field, object? value) => AccessTools.Field(typeof(Aura), field).SetValue(aura, value);
            Set("_actions", 3); Set("_summonedCycle", summonAvailable ? 0 : 1);
            Set("_meditateNext", planned == "MEDITATE"); Set("_verdict", planned == "GUILLOTINE" ? planned : null);
            aura.RefreshOrdinaryIntent();
            var announced = aura.NextMove;
            Check(announced.Id == planned, "先公布" + planned + "意图");
            int mana = aura.Mana;
            await CreatureCmd.Kill(soldiers[0]);
            Check(ReferenceEquals(aura.NextMove, announced), planned + "：第一名士兵死亡不重选意图");
            await CreatureCmd.Kill(soldiers[1]);
            Check(ReferenceEquals(aura.NextMove, announced), planned + "：清空士兵不改为召唤");
            Check(aura.Mana == mana - 30 && ManaCmd.InPile(player, PileType.Discard).Count() == 20,
                planned + "：两次死亡仍各削15魔力并生成10张魔力");
            int hp = player.Creature.CurrentHp; mana = aura.Mana;
            await aura.PerformMove();
            Check(planned == "MEDITATE" ? aura.Mana == mana + 20 && aura.Creature.GetPowerAmount<StrengthPower>() == 2
                    : player.Creature.CurrentHp == hp - (planned == "BARRAGE" ? 18 : 25),
                planned + "：已公布的行动照常实际结算");
            aura.RollMove([player.Creature]);
            Check(aura.NextMove.Id == (summonAvailable ? "SUMMON" : "BARRAGE"),
                summonAvailable ? "完成原行动后，正常选行动才允许召唤" : "士兵死亡不绕过本周期召唤次数限制");
            RunManager.Instance.CleanUp();
        }
        return checks;
    }

    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool ok, string text) { if (!ok) throw new Exception(text); count++; GD.Print("PASS 阿乌拉：" + text); }
        foreach (var character in ModelDb.AllCharacters.Where(c => c is not Frieren))
        {
            var other = Player.CreateForNewRun(character, UnlockState.all, 1);
            var otherRun = RunState.CreateForTest([other], seed: "AURA-EXCLUDE");
            RunManager.Instance.SetUpTest(otherRun, new NetSingleplayerGameService());
            Check(!ModelDb.Act<Hive>().AllBossEncounters.Any(e => e is AuraEncounter), character.Id + "不遇到阿乌拉");
            var old = ModelDb.Act<Hive>().ToMutable(); old.SetBossEncounter(ModelDb.Encounter<AuraEncounter>());
            old.ValidateRoomsAfterLoad(otherRun.Rng.UpFront);
            Check(old.BossEncounter is not AuraEncounter, "其他角色旧存档恢复原生Boss");
            RunManager.Instance.CleanUp();
        }
        var player = Player.CreateForNewRun<Frieren>(UnlockState.all, 1);
        var run = RunState.CreateForTest([player], seed: "AURA-COMBAT");
        RunManager.Instance.SetUpTest(run, new NetSingleplayerGameService()); LocalContext.NetId = 1;
        SaveManager.Instance.InitPrefsDataForTest();
        PresentationSettings.AllowOtherActTwoBosses = false;
        Check(ModelDb.Act<Hive>().AllBossEncounters.Any(e => e is AuraEncounter), "芙莉莲二层候选含阿乌拉");
        RunManager.Instance.GenerateRooms();
        var act = run.Acts.OfType<Hive>().Single();
        Check(act.BossEncounter is AuraEncounter, "原生发现顺序不能覆盖固定Boss");
        PresentationSettings.AllowOtherActTwoBosses = true;
        Check(ModelDb.Act<Hive>().AllBossEncounters.Any(e => e is AuraEncounter) && ModelDb.Act<Hive>().AllBossEncounters.Count() > 1,
            "开启设置后恢复混合Boss池，缓存不会污染");
        PresentationSettings.AllowOtherActTwoBosses = false;
        Check(!ModelDb.Act<Glory>().AllBossEncounters.Any(e => e is AuraEncounter), "阿乌拉不进入第三幕");

        player.Creature.SetMaxHpInternal(5000); player.Creature.SetCurrentHpInternal(5000);
        var encounter = ModelDb.Encounter<AuraEncounter>().ToMutable(); encounter.GenerateMonstersWithSlots(run);
        var combat = new CombatState(encounter, run); combat.AddPlayer(player);
        foreach (var (model, slot) in encounter.MonstersWithSlots) combat.AddCreature(combat.CreateCreature(model, CombatSide.Enemy, slot));
        CombatManager.Instance.SetUpCombat(combat);
        CombatTestState.SetInProgress(true);
        var aura = (Aura)combat.Enemies.Single(c => c.Monster is Aura).Monster;
        var boss = aura.Creature;
        var soldiers = combat.Enemies.Where(c => c.Monster is AuraSoldier).ToArray();
        foreach (var enemy in combat.Enemies.ToArray()) { enemy.Monster.RollMove([player.Creature]); await enemy.Monster.AfterAddedToRoom(); }
        Check(boss.CurrentHp == 200 && aura.Mana == 200 && soldiers.All(c => c.CurrentHp == 40), "200血200魔力与两名40血士兵");
        Check(soldiers[0].Monster.NextMove.Id == "STRIKE" && soldiers[1].Monster.NextMove.Id == "GUARD", "士兵错开起手");
        int hp = player.Creature.CurrentHp;
        await soldiers[0].Monster.PerformMove(); soldiers[0].Monster.RollMove([player.Creature]);
        Check(player.Creature.CurrentHp == hp - 8 && soldiers[0].Monster.NextMove.Id == "GUARD", "士兵斩击8后护卫");
        await soldiers[1].Monster.PerformMove(); soldiers[1].Monster.RollMove([player.Creature]);
        Check(boss.Block == 10 && soldiers[1].Monster.NextMove.Id == "STRIKE", "护卫给阿乌拉10甲后斩击");

        var context = new BlockingPlayerChoiceContext();
        await GameApi.Damage(context, boss, 30, ValueProp.Unpowered, player.Creature, null, null);
        Check(boss.CurrentHp == 200 && aura.Mana == 180 && boss.Block == 0, "先抵扣格挡，再用20魔力抵消剩余伤害");
        var elegance = boss.GetPower<AuraElegancePower>()!;
        elegance.ModifyHpLostAfterOstyLate(boss, 15, ValueProp.Unpowered, player.Creature, null);
        Check(aura.Mana == 180, "伤害预览不消费魔力");
        await aura.ChangeMana(-175);
        await GameApi.Damage(context, boss, 8, ValueProp.Unpowered, player.Creature, null, null);
        Check(aura.Mana == 0 && boss.CurrentHp == 197 && boss.HasPower<AuraElegancePower>(), "魔力不足溢出3点生命伤害，零魔力能力保留");
        await aura.ChangeMana(200);
        await aura.BeforeHandDraw(player, context, combat);
        await aura.BeforeHandDraw(player, context, combat);
        var dispel = player.PlayerCombatState!.Hand.Cards.OfType<AuraDispel>().Single();
        Check(dispel.IsValidTarget(soldiers[0]) && !dispel.IsValidTarget(boss) && dispel.Keywords.Contains(CardKeyword.Retain) && dispel.Keywords.Contains(CardKeyword.Exhaust),
            "开场只赠一次解除魔法，仅可选士兵，保留消耗");
        var play = CombatTestState.NewPlay(card: dispel, target: soldiers[0], resultPile: PileType.Exhaust, isAutoPlay: false, playIndex: 0, playCount: 1, resources: new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 });
        await (Task)AccessTools.Method(typeof(AuraDispel), "OnPlay").Invoke(dispel, [context, play])!;
        Check(soldiers[0].IsDead && aura.Mana == 185 && ManaCmd.Total(player) == 10 && aura.NextMove.Id == "TAUNT", "解除魔法立即解放士兵，削15／加10并改嘲讽");
        await soldiers[0].Monster.AfterDeath(context, soldiers[0], null, false, 0);
        Check(aura.Mana == 185 && ManaCmd.Total(player) == 10, "死亡奖励不会重复领取");
        // 后续原有完整行动循环使用两名士兵，另由RunActions核对减员场景。
        var replacement = combat.CreateCreature(ModelDb.Monster<AuraSoldier>().ToMutable(), CombatSide.Enemy, "Soldier1");
        combat.AddCreature(replacement); replacement.Monster!.SetUpForCombat(); replacement.Monster.RollMove([player.Creature]); await replacement.Monster.AfterAddedToRoom();
        await aura.PerformMove(); aura.RollMove([player.Creature]);
        Check(aura.NextMove.Id == "BARRAGE", "嘲讽空过后继续普通行动");
        async Task Step()
        {
            await aura.PerformMove();
            await aura.AfterSideTurnEnd(context, CombatSide.Enemy, combat.Enemies);
            aura.RollMove([player.Creature]);
        }
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 18 && aura.NextMove.Id == "MEDITATE" && aura.ScaleCount == 0,
            "第二回合连击，第3回合仍正常冥想，不单独消耗天平行动");
        aura.NoticeDispel(); Check(aura.NextMove.Id == "MEDITATE", "重复解除魔法不改普通意图");
        int mana = aura.Mana; await Step();
        Check(aura.Mana == mana + 20 && boss.GetPowerAmount<StrengthPower>() == 2 && aura.ScaleCount == 1 &&
            aura.NextMove.Id == "GUILLOTINE", "第3回合先判定天平，再冥想20魔力2力量");
        await aura.AfterSideTurnEnd(context, CombatSide.Enemy, combat.Enemies);
        Check(aura.ScaleCount == 1, "同一回合末不会重复判定");
        var submission = player.Creature.GetPower<AuraSubmissionPower>()!;
        player.PlayerCombatState.Energy = 3;
        Check(submission.ModifyHandDraw(player, 5) == 5, "服从不再减少抽牌");
        await submission.AfterEnergyReset(player);
        Check(player.PlayerCombatState.Energy == 2 && !player.Creature.HasPower<AuraSubmissionPower>(), "服从扣除1能量后立即移除");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 27, "断头台25加2力量");
        await Step();
        var extraMana = Enumerable.Range(0, 100).Select(_ => combat.CreateCard<Mana>(player)).ToArray();
        foreach (var card in extraMana) await CardPileCmd.Add(card, PileType.Discard);
        await aura.ChangeMana(40 - aura.Mana);
        await Step();
        Check(aura.Mana == 60 && boss.CurrentHp == 197 && aura.NextMove.Id == "STAGGER" && aura.ScaleCount == 2, "天平失败先眩晕，暂不转化生命");
        foreach (var card in extraMana) await CardPileCmd.RemoveFromCombat(card);
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp && boss.CurrentHp == 1 && boss.MaxHp == 1 && aura.Mana == 256, "眩晕空过后将196点生命转为魔力，生命上限降至1");
        Check(player.PlayerCombatState.Hand.Cards.OfType<AuraDispel>().Count()==1, "后续天平不再赠送解除魔法");
        int manaBeforeZeroSummon = aura.Mana + 100;
        await aura.ChangeMana(-aura.Mana);
        var announcedBeforeDeaths = aura.NextMove;
        await CreatureCmd.Kill(replacement);
        await CreatureCmd.Kill(soldiers[1]);
        Check(ReferenceEquals(aura.NextMove, announcedBeforeDeaths), "清空士兵保留已经公布的连击意图");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 30 && aura.NextMove.Id == "SUMMON" && aura.ScaleCount == 2,
            "第8回合先照常连击10×3，再于正常选行动时公布召唤");
        mana = aura.Mana; await aura.PerformMove();
        Check(aura.Mana == mana && combat.Enemies.Count(c => c.IsAlive && c.Monster is AuraSoldier) == 2,
            "零魔力仍按已公布意图召唤两名士兵且不扣魔力");
        await aura.ChangeMana(manaBeforeZeroSummon);
        await aura.AfterSideTurnEnd(context, CombatSide.Enemy, combat.Enemies); aura.RollMove([player.Creature]);
        Check(aura.ScaleCount == 3 && aura.NextMove.Id == "GUILLOTINE",
            "第9回合召唤后仍额外判定第三次天平");
        Check(((SingleAttackIntent)aura.NextMove.Intents.Single()).GetTotalDamage([player.Creature], boss) == 29,
            "第三次断头台意图仍25加4力量");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 29, "第三次断头台结算29伤害");
        await Step();
        await aura.ChangeMana(ManaCmd.Total(player) - aura.Mana);
        await Step();
        Check(aura.NextMove.Id is not "GUILLOTINE" and not "STAGGER", "冥想回合先按加魔力前的数值判平局，之后增加魔力不改变裁决");

        AccessTools.Field(typeof(Aura),"_actions").SetValue(aura,2);
        AccessTools.Field(typeof(Aura),"_meditateNext").SetValue(aura,true);
        AccessTools.Field(typeof(Aura),"_verdict").SetValue(aura,null);
        await aura.ChangeMana(ManaCmd.Total(player)-aura.Mana);aura.RefreshOrdinaryIntent();
        int beforeScale=aura.ScaleCount;int beforeMeditation=aura.Mana;
        Check(aura.NextMove.Id=="MEDITATE","构造天平回合双方魔力相同的冥想情形");
        await Step();
        Check(aura.ScaleCount==beforeScale+1&&aura.Mana==beforeMeditation+20&&aura.NextMove.Id is not "GUILLOTINE" and not "STAGGER","天平先按冥想前的魔力判平局，再加20魔力且不重复结算");
        foreach (var lang in new[] { "zhs", "eng" }) {
            LocManager.Instance.SetLanguage(lang);
            Check(!elegance.Description.GetFormattedText().Contains("LOC_ERR") && !boss.Name.Contains("FRIERENMOD"), lang + "名称和魔力文案可格式化");
        }
        var hud = new AuraScalesHud(); var panel = new Control { Size = new Vector2(1920,1080) }; root.AddChild(panel); panel.AddChild(hud);
        hud.SetProcess(false);
        for (int i = 0; i < 120; i++) hud.UpdateDisplay(200, 10, 12, 1d/60);
        Check(hud.CurrentAngle < -0.2f, "玩家魔力高：左盘下沉");
        for (int i = 0; i < 120; i++) hud.UpdateDisplay(10, 200, 12, 1d/60);
        Check(hud.CurrentAngle > 0.2f, "阿乌拉魔力高：右盘下沉");
        for (int i = 0; i < 180; i++) hud.UpdateDisplay(50, 50, 12, 1d/60);
        Check(Math.Abs(hud.CurrentAngle) < 0.002f, "魔力相同：天平动画回到水平");
        var pivot = hud.GetGlobalTransform() * hud.PivotOffset;
        hud.PlayJudgment();
        var judgment = (Tween)AccessTools.Field(typeof(AuraScalesHud), "_judgmentTween").GetValue(hud)!;
        judgment.Pause(); judgment.CustomStep(0.15);
        Check(hud.Scale.X > 1.27f && hud.Modulate.R > 1.7f, "天平判定明显放大28%并第一次闪亮");
        Check((hud.GetGlobalTransform() * hud.PivotOffset).DistanceTo(pivot) < 0.01f,
            "放大时悬挂点固定，天平不偏移");
        judgment.CustomStep(0.28);
        Check(hud.Modulate.IsEqualApprox(Colors.White), "第一次闪亮后恢复原色");
        judgment.CustomStep(0.33);
        Check(hud.Modulate.R > 1.5f, "判定期间第二次闪亮");
        judgment.CustomStep(0.85);
        Check(hud.Scale.IsEqualApprox(Vector2.One) && hud.Modulate.IsEqualApprox(Colors.White),
            "判定结束恢复原大小和原色");
        hud.PlayJudgment();
        ((Tween)AccessTools.Field(typeof(AuraScalesHud), "_judgmentTween").GetValue(hud)!).CustomStep(0.1);
        hud.PlayJudgment();
        Check(hud.Scale.IsEqualApprox(Vector2.One) &&
            hud.MouseBehaviorRecursive == Control.MouseBehaviorRecursiveEnum.Disabled,
            "重复判定重置而不叠加放大，提示不阻挡输入");
        ((Tween)AccessTools.Field(typeof(AuraScalesHud), "_judgmentTween").GetValue(hud)!).CustomStep(2);
        foreach (var model in new MonsterModel[] { aura, soldiers[0].Monster! }) {
            ((BaseLib.Abstracts.CustomMonsterModel)model).RegisterSceneConversions(); var visuals = model.CreateVisuals(); root.AddChild(visuals);
            Check(visuals.Bounds.Size.Y > 200, "战斗立绘和选中范围可加载"); visuals.QueueFree();
        }
        Check(encounter.MapNodeAssetPaths.All(path => ResourceLoader.Exists(path)), "Boss地图图标资源完整");
        if (OS.GetCmdlineUserArgs().Contains("--ui-snapshot"))
        {
            var bg = new ColorRect { Size = panel.Size, Color = new Color("202530"), MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddChild(bg); panel.MoveChild(bg, 0);
            var ground = new ColorRect { Position = new Vector2(0, 718), Size = new Vector2(1920, 2), Color = new Color("525362"), MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddChild(ground);
            foreach (var (path, pos) in new[] { ("aura_combat",new Vector2(1450,695)), ("aura_soldier_combat",new Vector2(1120,715)), ("aura_soldier_combat",new Vector2(1750,715)) }) {
                var sprite = GD.Load<PackedScene>($"res://FrierenMod/scenes/{path}.tscn").Instantiate<Node2D>();
                panel.AddChild(sprite); sprite.Position = pos;
            }
            hud.Visible = true;
            for (int i=0;i<180;i++) hud.UpdateDisplay(20,185,3,1d/60);
            for (int i=0;i<6;i++) await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/aura-layout.png"));
            hud.PlayJudgment();
            var pulse = (Tween)AccessTools.Field(typeof(AuraScalesHud), "_judgmentTween").GetValue(hud)!;
            pulse.Pause(); pulse.CustomStep(0.15); hud.UpdateDisplay(20,185,3,0);
            for (int i=0;i<3;i++) await root.ToSignal(root.GetTree(),SceneTree.SignalName.ProcessFrame);
            root.GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://../../artifacts/aura-judgment.png"));
        }
        panel.QueueFree();
        await aura.AfterDeath(context, boss, null, false, 0);
        Check(combat.Enemies.All(c => c.Monster is not AuraSoldier || !c.IsAlive), "阿乌拉死亡后士兵离场");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp();
        return count;
    }
}

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
        AccessTools.Property(typeof(CombatManager), nameof(CombatManager.IsInProgress)).SetValue(CombatManager.Instance, true);
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
        await CreatureCmd.Damage(context, boss, 30, ValueProp.Unpowered, player.Creature, null);
        Check(boss.CurrentHp == 200 && aura.Mana == 180 && boss.Block == 0, "先抵扣格挡，再用20魔力抵消剩余伤害");
        var elegance = boss.GetPower<AuraElegancePower>()!;
        elegance.ModifyHpLostAfterOstyLate(boss, 15, ValueProp.Unpowered, player.Creature, null);
        Check(aura.Mana == 180, "伤害预览不消费魔力");
        await aura.ChangeMana(-175);
        await CreatureCmd.Damage(context, boss, 8, ValueProp.Unpowered, player.Creature, null);
        Check(aura.Mana == 0 && boss.CurrentHp == 197 && boss.HasPower<AuraElegancePower>(), "魔力不足溢出3点生命伤害，零魔力能力保留");
        await aura.ChangeMana(200);
        await aura.AfterSideTurnStart(CombatSide.Player, [player.Creature], combat);
        await aura.AfterSideTurnStart(CombatSide.Player, [player.Creature], combat);
        var dispel = player.PlayerCombatState!.Hand.Cards.OfType<AuraDispel>().Single();
        Check(dispel.IsValidTarget(soldiers[0]) && !dispel.IsValidTarget(boss) && dispel.Keywords.Contains(CardKeyword.Retain) && dispel.Keywords.Contains(CardKeyword.Exhaust),
            "开场只赠一次解除魔法，仅可选士兵，保留消耗");
        var play = new CardPlay { Card = dispel, Target = soldiers[0], ResultPile = PileType.Exhaust, IsAutoPlay = false,
            PlayIndex = 0, PlayCount = 1, Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 } };
        await (Task)AccessTools.Method(typeof(AuraDispel), "OnPlay").Invoke(dispel, [context, play])!;
        Check(soldiers[0].IsDead && aura.Mana == 185 && ManaCmd.Total(player) == 10 && aura.NextMove.Id == "TAUNT", "解除魔法立即解放士兵，削15／加10并改嘲讽");
        await soldiers[0].Monster.AfterDeath(context, soldiers[0], false, 0);
        Check(aura.Mana == 185 && ManaCmd.Total(player) == 10, "死亡奖励不会重复领取");
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
            aura.NextMove.Id == "GUILLOTINE", "第3回合冥想20魔力2力量后额外判定天平");
        await aura.AfterSideTurnEnd(context, CombatSide.Enemy, combat.Enemies);
        Check(aura.ScaleCount == 1, "同一回合末不会重复判定");
        var submission = player.Creature.GetPower<AuraSubmissionPower>()!;
        submission.AmountOnTurnStart = submission.Amount;
        Check(submission.ModifyHandDraw(player, 5) == 3, "服从实际减少开场抽牌");
        await submission.AfterSideTurnStart(CombatSide.Player, [player.Creature], combat);
        Check(player.Creature.HasPower<AuraSubmissionPower>(), "选牌暂停抽牌时不能提前移除服从");
        await submission.AfterModifyingHandDraw();
        Check(submission.ModifyHandDraw(player, 5) == 5, "同回合不重复减少抽牌");
        await submission.AfterSideTurnEnd(context, CombatSide.Player, [player.Creature]);
        Check(!player.Creature.HasPower<AuraSubmissionPower>(), "抽牌后回合结束移除服从");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 27, "断头台25加2力量");
        await Step();
        var extraMana = Enumerable.Range(0, 100).Select(_ => combat.CreateCard<Mana>(player)).ToArray();
        foreach (var card in extraMana) await CardPileCmd.Add(card, PileType.Discard);
        await aura.ChangeMana(40 - aura.Mana);
        await Step();
        Check(aura.Mana == 30 && aura.NextMove.Id == "STAGGER" && aura.ScaleCount == 2, "第6回合先冥想20再因天平失败失去30魔力");
        foreach (var card in extraMana) await CardPileCmd.RemoveFromCombat(card);
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp, "失败后空过不攻击");
        await aura.ChangeMana(100);
        await CreatureCmd.Kill(soldiers[1]);
        Check(aura.NextMove.Id == "SUMMON", "清空士兵后可预判召唤");
        mana = aura.Mana; await Step();
        Check(aura.Mana == mana - 40 && combat.Enemies.Count(c => c.IsAlive && c.Monster is AuraSoldier) == 2,
            "召唤扣40，补两名士兵");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 30 && aura.ScaleCount == 3 && aura.NextMove.Id == "GUILLOTINE",
            "第9回合连击正常造成10×3后额外判定第三次天平");
        Check(((SingleAttackIntent)aura.NextMove.Intents.Single()).GetTotalDamage([player.Creature], boss) == 44,
            "第三次断头台意图40加4力量");
        hp = player.Creature.CurrentHp; await Step();
        Check(player.Creature.CurrentHp == hp - 44, "第三次断头台结算44伤害");
        await Step();
        await aura.ChangeMana(ManaCmd.Total(player) - aura.Mana);
        await Step();
        Check(aura.NextMove.Id is not "GUILLOTINE" and not "STAGGER", "天平平局无惩罚、无眩晕");

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
        }
        panel.QueueFree();
        await aura.AfterDeath(context, boss, false, 0);
        Check(combat.Enemies.All(c => c.Monster is not AuraSoldier || !c.IsAlive), "阿乌拉死亡后士兵离场");
        LocManager.Instance.SetLanguage("eng"); RunManager.Instance.CleanUp();
        return count;
    }
}

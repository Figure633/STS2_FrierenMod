using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Monsters;

public sealed class Aura : CustomMonsterModel
{
    private int _actions;
    private int _lastWeighedAction;
    private int _summonedCycle = -1;
    private bool _openingGranted, _tauntUsed, _tauntPending, _meditateNext;
    private string? _verdict;
    private Dictionary<string, MoveState> _moves = [];
    public int ScaleCount { get; private set; }
    public int UpcomingTurn => _actions + 1;
    public int Mana => Creature.GetPower<AuraElegancePower>()?.StoredMana ?? 0;
    public int PlayerCount => Math.Max(1, CombatState.Players.Count);
    public bool Enhanced => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 0) == 1;
    public int BarrageDamage => Enhanced ? 7 : 6;
    public int GuillotineDamage => Enhanced ? 28 : 25;
    public int SubmissionEnergy => Enhanced ? 2 : 1;
    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;
    public override string CustomVisualPath => "res://FrierenMod/scenes/aura_combat.tscn";

    public override async Task AfterAddedToRoom()
    {
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<AuraElegancePower>(context, Creature, 200 * PlayerCount + 1, Creature, null);
        await PowerCmd.Apply<AuraScalesPower>(context, Creature, 1, Creature, null);
        await PowerCmd.Apply<AuraArrogancePower>(context, Creature, 1, Creature, null);
    }

    public override async Task BeforeHandDraw(MegaCrit.Sts2.Core.Entities.Players.Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (_openingGranted) return;
        _openingGranted = true;
        await GiveDispel();
    }

    public Task ChangeMana(int delta) => Creature.GetPower<AuraElegancePower>() is { } power
        ? PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), power, Math.Max(-Mana, delta), Creature, null)
        : Task.CompletedTask;

    public void NoticeDispel()
    {
        if (_tauntUsed) return;
        _tauntUsed = true;
        if (NextMove.Id == "GUILLOTINE") return;
        _tauntPending = true;
        RefreshOrdinaryIntent();
    }

    // 首次解除魔法即时嘲讽；已经准备的断头台照常攻击并消耗唯一嘲讽机会。
    public void RefreshOrdinaryIntent()
    {
        if (!Creature.IsAlive || IsPerformingMove || _moves.Count == 0) return;
        SetMoveImmediate(_moves[ChooseMove()], forceTransition: true);
        CoordinateSoldiers();
    }

    internal void CoordinateSoldiers()
    {
        if (NextMove.Id != "MEDITATE") return;
        foreach (var soldier in CombatState.Enemies.Where(c => c.IsAlive).Select(c => c.Monster).OfType<AuraSoldier>())
            soldier.ForceAttack();
    }

    private string ChooseMove()
    {
        if (_tauntPending) return "TAUNT";
        if (_verdict != null) return _verdict;
        if (_summonedCycle != _actions / 3 && !CombatState.Enemies.Any(c => c.IsAlive && c.Monster is AuraSoldier)) return "SUMMON";
        return _meditateNext && CombatState.Enemies.Count(c => c.IsAlive && c.Monster is AuraSoldier) == 2
            ? "MEDITATE" : "BARRAGE";
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _moves = [];
        var next = new ConditionalBranchState("NEXT");
        void Add(string id, Func<Task> action, params AbstractIntent[] intents)
        {
            var move = new MoveState(id, async _ => { await action(); _actions++; }, intents) { FollowUpState = next };
            _moves.Add(id, move);
            next.AddState(move, () => ChooseMove() == id);
        }
        Add("GUILLOTINE", async () => { _verdict = null; await Attack(GuillotineDamage, 1); }, new SingleAttackIntent(() => GuillotineDamage));
        Add("STAGGER", async () => {
            _verdict = null;
            int converted = Math.Max(0, Creature.CurrentHp - 1);
            await CreatureCmd.SetCurrentHp(Creature, 1);
            await CreatureCmd.SetMaxHp(Creature, 1);
            await ChangeMana(converted);
        }, new StunIntent());
        Add("TAUNT", () => {
            _tauntPending = false;
            TalkCmd.Play(new LocString("monsters", "FRIERENMOD-AURA.taunt"), Creature, VfxColor.White, VfxDuration.Long);
            return Task.CompletedTask;
        }, new UnknownIntent());
        Add("SUMMON", async () => {
            if (CombatState.Enemies.Any(c => c.IsAlive && c.Monster is AuraSoldier)) return;
            _summonedCycle = _actions / 3;
            await CreatureCmd.Add<AuraSoldier>(CombatState, "Soldier1");
            var guard = (AuraSoldier)ModelDb.Monster<AuraSoldier>().ToMutable();
            guard.GuardFirst = true;
            await CreatureCmd.Add(guard, CombatState, CombatSide.Enemy, "Soldier2");
        }, new SummonIntent());
        Add("BARRAGE", async () => { _meditateNext = true; await Attack(BarrageDamage, 3); }, new MultiAttackIntent(BarrageDamage, 3));
        Add("MEDITATE", async () => {
            _meditateNext = false;
            if (UpcomingTurn % 3 == 0 && _lastWeighedAction != UpcomingTurn) {
                _lastWeighedAction = UpcomingTurn;
                await Weigh();
            }
            await ChangeMana(20 * PlayerCount);
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
        }, new BuffIntent());
        return new MonsterMoveStateMachine(_moves.Values.Cast<MonsterState>().Append(next), next);
    }

    private Task Attack(int damage, int count) => DamageCmd.Attack(damage).WithHitCount(count).FromMonster(this)
        .WithHitFx("vfx/vfx_attack_slash").OnlyPlayAnimOnce().Execute(null);

    private async Task GiveDispel()
    {
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            await CardPileCmd.AddGeneratedCardToCombat(CombatState.CreateCard<AuraDispel>(player), PileType.Hand, player, CardPilePosition.Top);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || !Creature.IsAlive || _actions == 0 || _actions % 3 != 0 || _lastWeighedAction == _actions) return;
        _lastWeighedAction = _actions;
        await Weigh();
        RefreshOrdinaryIntent();
    }

    private async Task Weigh()
    {
        ScaleCount++;
        if (!MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn)
        {
            SfxCmd.Play("event:/sfx/enemy/enemy_attacks/terror_eel/terror_eel_debuff");
            VfxCmd.PlayOnCreatureCenter(Creature, VfxCmd.gazePath);
            FrierenMod.Scripts.UI.AuraScalesHud.Judgment();
            await Cmd.CustomScaledWait(0.6f, 0.6f);
        }
        // 天平直接比较双方总魔力；阿乌拉的基础魔力与固定增减量按开场人数缩放。
        var players = CombatState.Players.Where(p => p.Creature.IsAlive).ToArray();
        int playerMana = players.Sum(ManaCmd.Total);
        if (Mana > playerMana)
        {
            _verdict = "GUILLOTINE";
            foreach (var player in players)
                await PowerCmd.Apply<AuraSubmissionPower>(new ThrowingPlayerChoiceContext(), player.Creature, SubmissionEnergy, Creature, null);
        }
        else if (Mana < playerMana)
        {
            _verdict = "STAGGER";
        }
    }

    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, MegaCrit.Sts2.Core.Models.CardModel? source, bool prevented, float animLength)
    {
        if (creature != Creature || prevented) return;
        foreach (var soldier in CombatState.Enemies.Where(c => c.IsAlive && c.Monster is AuraSoldier).ToArray())
            await CreatureCmd.Escape(soldier);
    }
}


// 意图生成顺序中阿乌拉和士兵都可能先被处理，分别在原生掷行动后同步。
[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.RollMove))]
internal static class AuraSoldierCoordination
{
    private static void Postfix(MonsterModel __instance)
    {
        if (__instance is Aura aura) aura.CoordinateSoldiers();
        else if (__instance is AuraSoldier soldier &&
            soldier.CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura)?.Monster is Aura leader)
            leader.CoordinateSoldiers();
    }
}

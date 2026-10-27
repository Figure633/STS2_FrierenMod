using BaseLib.Abstracts;
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
    public int GuillotineDamage => ScaleCount >= 3 ? 40 : 25;
    public override int MinInitialHp => 200;
    public override int MaxInitialHp => 200;
    public override string CustomVisualPath => "res://FrierenMod/scenes/aura_combat.tscn";

    public override async Task AfterAddedToRoom()
    {
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<AuraElegancePower>(context, Creature, 201, Creature, null);
        await PowerCmd.Apply<AuraScalesPower>(context, Creature, 1, Creature, null);
        await PowerCmd.Apply<AuraArrogancePower>(context, Creature, 1, Creature, null);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Player || _openingGranted) return;
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
        _tauntPending = true;
        RefreshOrdinaryIntent();
    }

    // 只有首次解除魔法与清空士兵会即时改意图；固定天平及已结算的裁决具有更高优先级。
    public void RefreshOrdinaryIntent()
    {
        if (!Creature.IsAlive || IsPerformingMove || _moves.Count == 0) return;
        SetMoveImmediate(_moves[ChooseMove()], forceTransition: true);
    }

    private string ChooseMove()
    {
        if (_verdict != null) return _verdict;
        if (_tauntPending) return "TAUNT";
        if (Mana >= 40 && _summonedCycle != _actions / 3 && !CombatState.Enemies.Any(c => c.IsAlive && c.Monster is AuraSoldier)) return "SUMMON";
        return _meditateNext ? "MEDITATE" : "BARRAGE";
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
        Add("STAGGER", () => { _verdict = null; return Task.CompletedTask; }, new StunIntent());
        Add("TAUNT", () => {
            _tauntPending = false;
            TalkCmd.Play(new LocString("monsters", "FRIERENMOD-AURA.taunt"), Creature, VfxColor.White, VfxDuration.Long);
            return Task.CompletedTask;
        }, new UnknownIntent());
        Add("SUMMON", async () => {
            // 意图公布后魔力可能已被打空；资源不足则召唤失败，不产生负魔力或免费士兵。
            if (Mana < 40 || CombatState.Enemies.Any(c => c.IsAlive && c.Monster is AuraSoldier)) return;
            _summonedCycle = _actions / 3;
            await ChangeMana(-40);
            await CreatureCmd.Add<AuraSoldier>(CombatState, "Soldier1");
            var guard = (AuraSoldier)ModelDb.Monster<AuraSoldier>().ToMutable();
            guard.GuardFirst = true;
            await CreatureCmd.Add(guard, CombatState, CombatSide.Enemy, "Soldier2");
        }, new SummonIntent());
        Add("BARRAGE", async () => { _meditateNext = true; await Attack(6, 3); }, new MultiAttackIntent(6, 3));
        Add("MEDITATE", async () => { _meditateNext = false; await ChangeMana(20); await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null); }, new BuffIntent());
        return new MonsterMoveStateMachine(_moves.Values.Cast<MonsterState>().Append(next), next);
    }

    private Task Attack(int damage, int count) => DamageCmd.Attack(damage).WithHitCount(count).FromMonster(this)
        .WithHitFx("vfx/vfx_attack_slash").OnlyPlayAnimOnce().Execute(null);

    private async Task GiveDispel()
    {
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            await CardFlowCmd.Generate<AuraDispel>(player, PileType.Hand);
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
        // 单人取本人；全芙莉莲联机队伍按平均魔力比较，避免人数直接决定天平胜负。
        var players = CombatState.Players.Where(p => p.Creature.IsAlive).ToArray();
        decimal playerMana = players.Length == 0 ? 0 : players.Average(p => (decimal)ManaCmd.Total(p));
        if (Mana > playerMana)
        {
            _verdict = "GUILLOTINE";
            foreach (var player in players)
                await PowerCmd.Apply<AuraSubmissionPower>(new ThrowingPlayerChoiceContext(), player.Creature, 2, Creature, null);
        }
        else if (Mana < playerMana)
        {
            _verdict = "STAGGER";
            await ChangeMana(-30);
        }
        await GiveDispel();
    }

    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool prevented, float animLength)
    {
        if (creature != Creature || prevented) return;
        foreach (var soldier in CombatState.Enemies.Where(c => c.IsAlive && c.Monster is AuraSoldier).ToArray())
            await CreatureCmd.Escape(soldier);
    }
}

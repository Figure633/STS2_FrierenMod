using BaseLib.Abstracts;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace FrierenMod.Scripts.Monsters;

public sealed class Mimic : CustomMonsterModel
{
    private bool _hasRested;
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 170, 160);
    public override int MaxInitialHp => MinInitialHp + 4;
    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 25, 22);
    public override string CustomVisualPath => "res://FrierenMod/scenes/mimic_combat.tscn";
    public override Task AfterAddedToRoom() => PowerCmd.Apply<MimicPower>(new ThrowingPlayerChoiceContext(), Creature, 5, Creature, null);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var disguise = new MoveState("DISGUISE", _ => Task.CompletedTask, new UnknownIntent());
        var swallow = new MoveState("SWALLOW", Swallow, new DebuffIntent());
        var strike = new MoveState("STRIKE", Strike, new SingleAttackIntent(Damage));
        var rest = new MoveState("FULL", _ => { _hasRested = true; Burp(Creature); return Task.CompletedTask; }, new UnknownIntent());
        var wake = new ConditionalBranchState("WAKE");
        wake.AddState(disguise, () => Creature.HasPower<MimicPower>());
        wake.AddState(swallow, () => true);
        var roll = new RandomBranchState("ROLL");
        roll.AddBranch(strike, 2, () => 70f);
        roll.AddBranch(swallow, MoveRepeatType.CannotRepeat, 30f);
        var next = new ConditionalBranchState("NEXT");
        next.AddState(rest, () => !_hasRested && Creature.CombatState!.Players.Any(p => p.Creature.GetPowerAmount<SwallowedPower>() >= 6));
        next.AddState(roll, () => true);
        disguise.FollowUpState = wake; swallow.FollowUpState = next; strike.FollowUpState = next; rest.FollowUpState = roll;
        return new MonsterMoveStateMachine([disguise, swallow, strike, rest, wake, roll, next], disguise);
    }
    private Task Swallow(IReadOnlyList<Creature> targets) => PowerCmd.Apply<SwallowedPower>(new ThrowingPlayerChoiceContext(), targets, 2, Creature, null);
    private Task Strike(IReadOnlyList<Creature> targets) => DamageCmd.Attack(Damage).FromMonster(this).WithHitFx("vfx/vfx_attack_slash").Execute(null);
    internal static void Burp(Creature speaker) =>
        TalkCmd.Play(new LocString("monsters", "FRIERENMOD-MIMIC.burp"), speaker, VfxColor.White, VfxDuration.Long);
}

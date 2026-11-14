using BaseLib.Abstracts;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;

namespace FrierenMod.Scripts.Monsters;

public sealed class AuraSoldier : CustomMonsterModel
{
    private bool _released;
    private MoveState? _strike;
    public bool GuardFirst { get; set; }
    public bool Enhanced => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 0) == 1;
    public int AttackDamage => Enhanced ? 10 : 8;
    public int GuardAmount => Enhanced ? 12 : 10;
    public override int MinInitialHp => 40;
    public override int MaxInitialHp => 40;
    public override string CustomVisualPath => "res://FrierenMod/scenes/aura_soldier_combat.tscn";
    public override Task AfterAddedToRoom() => PowerCmd.Apply<AuraSoulPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var strike = new MoveState("STRIKE", _ => DamageCmd.Attack(AttackDamage).FromMonster(this).WithHitFx("vfx/vfx_attack_slash").Execute(null), new SingleAttackIntent(() => AttackDamage));
        _strike = strike;
        var guard = new MoveState("GUARD", async _ => {
            if (CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura) is { } aura)
                await CreatureCmd.GainBlock(aura, GuardAmount, ValueProp.Unpowered, null);
        }, new DefendIntent());
        strike.FollowUpState = guard; guard.FollowUpState = strike;
        return new MonsterMoveStateMachine([strike, guard], GuardFirst ? guard : strike);
    }
    internal void ForceAttack()
    {
        if (_strike != null && NextMove.Id != "STRIKE")
            SetMoveImmediate(_strike, forceTransition: true);
    }

    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, MegaCrit.Sts2.Core.Models.CardModel? source, bool prevented, float animLength)
    {
        if (creature != Creature || prevented || _released) return;
        _released = true;
        if (CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura)?.Monster is not Aura aura) return;
        await aura.ChangeMana(-15 * aura.PlayerCount);
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            await ManaCmd.Generate(player, PileType.Discard, 10, previewTime: 0.15f, fastAnimation: true);
        // 死亡只结算资源；召唤资格留给下一次正常行动选择，不改写已公布的意图。
    }
}

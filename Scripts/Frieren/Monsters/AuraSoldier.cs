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

namespace FrierenMod.Scripts.Monsters;

public sealed class AuraSoldier : CustomMonsterModel
{
    private bool _released;
    public bool GuardFirst { get; set; }
    public override int MinInitialHp => 40;
    public override int MaxInitialHp => 40;
    public override string CustomVisualPath => "res://FrierenMod/scenes/aura_soldier_combat.tscn";
    public override Task AfterAddedToRoom() => PowerCmd.Apply<AuraSoulPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var strike = new MoveState("STRIKE", _ => DamageCmd.Attack(8).FromMonster(this).WithHitFx("vfx/vfx_attack_slash").Execute(null), new SingleAttackIntent(8));
        var guard = new MoveState("GUARD", async _ => {
            if (CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura) is { } aura)
                await CreatureCmd.GainBlock(aura, 10, ValueProp.Unpowered, null);
        }, new DefendIntent());
        strike.FollowUpState = guard; guard.FollowUpState = strike;
        return new MonsterMoveStateMachine([strike, guard], GuardFirst ? guard : strike);
    }
    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, bool prevented, float animLength)
    {
        if (creature != Creature || prevented || _released) return;
        _released = true;
        if (CombatState.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura)?.Monster is not Aura aura) return;
        await aura.ChangeMana(-15);
        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
            await ManaCmd.Generate(player, PileType.Discard, 10, previewTime: 0.15f, fastAnimation: true);
        aura.RefreshOrdinaryIntent();
    }
}

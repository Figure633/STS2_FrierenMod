using FrierenMod.Scripts.Shared.Compatibility;
using BaseLib.Abstracts;
using HarmonyLib;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace FrierenMod.Scripts.Monsters;

public sealed class SpiegelFrieren : CustomMonsterModel
{
    private Queue<int>? _debuffs;
    private int _multiAttackUses;
    private int MultiAttackHits => 2 + _multiAttackUses / 2;
    private bool _apexTriggered;
    private MoveState? _apex;
    private AttackIntent? _apexAttackIntent;
    public bool Enhanced => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 1, 0) == 1;
    public override int MinInitialHp => 500;
    public override int MaxInitialHp => 500;
    // 最后的输出窗口按仍能行动的玩家计，不叠加多人 Boss 的额外生命系数。
    public int ApexHp => 100 * Math.Max(1, CombatState.Players.Count(p => p.Creature.IsAlive));
    private int Damage => Enhanced ? 25 : 18;
    private int MultiAttackDamage => Enhanced ? 12 : 10;
    public override string CustomVisualPath => "res://FrierenMod/scenes/spiegel_combat.tscn";
    public override async Task AfterAddedToRoom()
    {
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<CopyPower>(context, Creature, 1, Creature, null);
        await PowerCmd.Apply<SpellCasterPower>(context, Creature, 1, Creature, null);
        await PowerCmd.Apply<EnemyManaPower>(context, Creature, 1, Creature, null);
    }
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var suppress = new MoveState("MANA_SUPPRESSION", Suppress, new DebuffIntent());
        var strike = new MoveState("OFFENSIVE_MAGIC", targets => Attack(1), new SingleAttackIntent(Damage));
        var pollute = new MoveState("MANA_POLLUTION", Pollute, new DebuffIntent());
        var invert = new MoveState("UPSIDE_DOWN", Invert, new StatusIntent(1));
        var weaken = new MoveState("CHANT_SUPPRESSION", Weaken, new DebuffIntent());
        var multi = new MoveState("MULTIPLE_OFFENSIVE_MAGIC", MultiAttack, new MultiAttackIntent(MultiAttackDamage, () => MultiAttackHits));
        _apex = new MoveState("APEX_MAGIC", PrepareApex, new UnknownIntent());
        _apexAttackIntent = new MultiAttackIntent(999, 999);
        var apexAttack = new MoveState("APEX_ATTACK", _ => Attack(999, 999, fixedDamage: true), _apexAttackIntent);
        var next = new ConditionalBranchState("NEXT_DEBUFF");
        next.AddState(pollute, () => NextDebuff() == 0);
        next.AddState(invert, () => NextDebuff() == 1);
        next.AddState(weaken, () => NextDebuff() == 2);
        next.AddState(multi, () => true);
        suppress.FollowUpState = strike; strike.FollowUpState = next;
        pollute.FollowUpState = strike; invert.FollowUpState = strike; weaken.FollowUpState = strike;
        multi.FollowUpState = multi; _apex.FollowUpState = apexAttack; apexAttack.FollowUpState = multi;
        return new MonsterMoveStateMachine([suppress, strike, pollute, invert, weaken, multi, _apex, apexAttack, next], suppress);
    }
    private int NextDebuff()
    {
        // 只在战斗开始后消耗怪物AI随机数；预加载与百科读取意图不消耗跑局随机数。
        if (_debuffs == null)
        {
            var values = new List<int> { 0, 1, 2 };
            for (int i = values.Count - 1; i > 0; i--) { int j = RunRng.MonsterAi.NextInt(i + 1); (values[i], values[j]) = (values[j], values[i]); }
            _debuffs = new Queue<int>(values);
        }
        return _debuffs.TryPeek(out var value) ? value : -1;
    }
    private void ConsumeDebuff() => _debuffs?.Dequeue();
    private async Task Attack(int hits, int? damage = null, bool fixedDamage = false)
    {
        if (CombatState.PlayerCreatures.FirstOrDefault(c => c.IsAlive) is { } target && NHyperbeamVfx.Create(Creature, target) is { } beam)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChild(beam);
            await Cmd.Wait(NHyperbeamVfx.hyperbeamAnticipationDuration + NHyperbeamVfx.hyperbeamLaserDuration);
        }
        var attack = DamageCmd.Attack(damage ?? Damage).WithHitCount(hits).FromMonster(this)
            .WithHitFx("vfx/vfx_attack_blunt").OnlyPlayAnimOnce();
        if (fixedDamage) attack.Unpowered();
        await attack.Execute(null);
    }
    // 原生攻击意图固定用 Move 预览；仅此招改用与结算相同的 Unpowered，保留无实体等受击防护。
    [HarmonyPatch(typeof(AttackIntent), nameof(AttackIntent.GetSingleDamage))]
    private static class ApexIntentDamage
    {
        private static bool Prefix(AttackIntent __instance, Creature owner, ref int __result)
        {
            if (owner.Monster is not SpiegelFrieren monster || !ReferenceEquals(__instance, monster._apexAttackIntent)) return true;
            decimal damage = 999;
            var player = LocalContext.GetMe(owner.CombatState);
            if (player != null) damage = Hook.ModifyDamage(player.RunState, owner.CombatState, player.Creature, owner, damage,
                ValueProp.Move | ValueProp.Unpowered, null, null, ModifyDamageHookType.All, CardPreviewMode.None, out _);
            __result = Math.Max(0, (int)damage);
            return false;
        }
    }
    private async Task MultiAttack(IReadOnlyList<Creature> targets) { await Attack(MultiAttackHits, MultiAttackDamage); _multiAttackUses++; }
    private Task Suppress(IReadOnlyList<Creature> targets) => PowerCmd.Apply<BanManaGainPower>(new ThrowingPlayerChoiceContext(), targets, 1, Creature, null);
    private async Task Weaken(IReadOnlyList<Creature> targets)
    {
        ConsumeDebuff();
        await PowerCmd.Apply<WeakenedChantPower>(new ThrowingPlayerChoiceContext(), targets, Enhanced ? 3 : 1, Creature, null);
    }
    private async Task Invert(IReadOnlyList<Creature> targets)
    {
        ConsumeDebuff();
        foreach (var player in CombatState.Players) await CardFlowCmd.Generate<UpsideDown>(player, PileType.Draw);
    }
    private async Task Pollute(IReadOnlyList<Creature> targets)
    {
        ConsumeDebuff();
        foreach (var player in CombatState.Players)
        {
            var mana = ManaCmd.InPile(player, PileType.Draw).Concat(ManaCmd.InPile(player, PileType.Discard)).Take(Enhanced ? 5 : 3).ToArray();
            foreach (var card in mana)
            {
                var pile = card.Pile!.Type;
                await CardPileCmd.RemoveFromCombat(card);
                CardModel replacement = (RunRng.MonsterAi.NextInt(100) % 5) switch {
                    1 => CombatState.CreateCard<Slimed>(player), 2 => CombatState.CreateCard<Wound>(player),
                    3 => CombatState.CreateCard<Burn>(player), 4 => CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Void>(player), _ => CombatState.CreateCard<Dazed>(player)
                };
                await CardFlowCmd.AddGenerated(replacement, pile, player);
            }
        }
    }
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Creature && delta < 0 && Creature.CurrentHp > 0 && Creature.CurrentHp <= ApexHp && !_apexTriggered && _apex != null)
        {
            _apexTriggered = true; SetMoveImmediate(_apex, forceTransition: true);
        }
        return Task.CompletedTask;
    }
    private async Task PrepareApex(IReadOnlyList<Creature> targets)
    {
        foreach (var power in Creature.Powers.ToArray()) await PowerCmd.Remove(power);
        await GameApi.LoseBlock(new ThrowingPlayerChoiceContext(), Creature, Creature.Block, Creature);
    }
    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature, MegaCrit.Sts2.Core.Models.CardModel? source, bool prevented, float animLength)
    {
        if (creature != Creature || prevented) return;
        foreach (var summoned in CombatState.Enemies.Where(c => c.Monster is DampCultist && c.IsAlive).ToArray()) await CreatureCmd.Escape(summoned);
    }
}

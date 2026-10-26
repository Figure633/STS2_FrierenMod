using BaseLib.Patches.Features;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using FrierenMod.Scripts.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace FrierenMod.Scripts.Cards;

public sealed class AuraDispel() : FrierenCard(0, CardType.Skill, CardRarity.Token, SoldierTarget)
{
    [CustomEnum] public static TargetType SoldierTarget;
    public static void RegisterTarget() => CustomTargetType.RegisterSingleTargetType(SoldierTarget,
        (creature, player) => creature.IsAlive && creature.Side != player.Creature.Side && creature.Monster is AuraSoldier);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];
    public override int MaxUpgradeLevel => 0;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target is not { IsAlive: true, Monster: AuraSoldier } soldier) return;
        foreach (var aura in CombatState!.Enemies.Where(c => c.IsAlive).Select(c => c.Monster).OfType<Aura>()) aura.NoticeDispel();
        await CreatureCmd.Kill(soldier);
    }
}

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class AuraDispelTargetRegistration
{
    private static void Postfix() => AuraDispel.RegisterTarget();
}

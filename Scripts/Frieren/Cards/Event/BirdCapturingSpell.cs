using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Entities.Creatures;
using HarmonyLib;

namespace FrierenMod.Scripts.Cards;

public sealed class BirdCapturingSpell() : FrierenCard(0, CardType.Skill, CardRarity.Event, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount",2)];
    // 以实际有翼外形为准；漂浮、旋翼及戴鸟面具的邪教徒不等于有翅膀。
    public static bool IsBird(MonsterModel? monster) => monster is Byrdpip or Byrdonis or OwlMagistrate or HunterKiller;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target != null && IsBird(play.Target.Monster)) {
            await PowerCmd.Apply<BirdCapturedPower>(context, play.Target, (int)DynamicVars["Amount"].BaseValue, Owner.Creature, this);
            if (play.Target.HasPower<BirdCapturedPower>()) Presentation.FrierenAchievements.Unlock(Owner, "4004");
        }
    }
    protected override void OnUpgrade() => DynamicVars["Amount"].UpgradeValueBy(1);

    [HarmonyPatch(typeof(CardModel),nameof(CardModel.IsValidTarget))]
    private static class WingedTarget
    {
        private static void Postfix(CardModel __instance,Creature? target,ref bool __result)
        {
            if(__instance is BirdCapturingSpell && __result)__result=IsBird(target?.Monster);
        }
    }
}

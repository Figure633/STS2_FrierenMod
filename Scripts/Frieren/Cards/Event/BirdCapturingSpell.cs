using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace FrierenMod.Scripts.Cards;

public sealed class BirdCapturingSpell() : FrierenCard(0, CardType.Skill, CardRarity.Event, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    // 对应一代鸟、邪教徒及鸟形敌人；二代使用具体模型，避免按名称字符串误伤其他怪物。
    public static bool IsBird(MonsterModel? monster) => monster is Byrdpip or Byrdonis or OwlMagistrate or CalcifiedCultist or DampCultist;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Target != null && IsBird(play.Target.Monster)) {
            await PowerCmd.Apply<BirdCapturedPower>(context, play.Target, 3, Owner.Creature, this);
            if (play.Target.HasPower<BirdCapturedPower>()) Presentation.FrierenAchievements.Unlock(Owner, "4004");
        }
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}

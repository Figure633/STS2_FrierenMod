using BaseLib.Utils;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace FrierenMod.Scripts.Cards;

public sealed class HonestLiving() : FrierenCard(1, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DynamicVar("MinDamage", 4), new DynamicVar("MaxDamage", 7), new DynamicVar("Hits", 3),
         new DynamicVar("Gold", 777), new DynamicVar("Debt", 7)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("CurrentDebt", IsMutable && Owner != null ? DebtStore.Read(Owner) : 0);
    }

    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int maximum = DynamicVars["MaxDamage"].IntValue;
        // 三次投点先独立生成，判奖依据投点，而非敌人格挡后的生命损失。
        var rolls = Enumerable.Range(0, DynamicVars["Hits"].IntValue).Select(_ => RollDamage()).ToArray();
        foreach (int damage in rolls)
            await DamageCmd.Attack(damage).FromCardCompatibility(this, play).Unpowered()
                .TargetingRandomOpponents(CombatState!).Execute(context);
        if (rolls.All(damage => damage == maximum)) {
            await PlayerCmd.GainGold(DynamicVars["Gold"].IntValue, Owner);
            Presentation.FrierenAchievements.Unlock(Owner,"8017");
        }
        else DebtStore.Add(Owner, DynamicVars["Debt"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["MinDamage"].UpgradeValueBy(1);

    internal int RollDamage()
    {
        int min = DynamicVars["MinDamage"].IntValue, max = DynamicVars["MaxDamage"].IntValue;
        var rng = Owner.RunState.Rng.Niche;
        if (Enchantment is not Enchantments.LoadedDice { Status: MegaCrit.Sts2.Core.Entities.Enchantments.EnchantmentStatus.Normal })
            return rng.NextInt(min, max + 1);
        // 原本最大值的概率加 15 个百分点，其余点数均分剩余概率。
        int count = max - min + 1;
        return count <= 1 || rng.NextInt(100 * count) < 100 + 15 * count ? max : rng.NextInt(min, max);
    }
}

using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public sealed class StrikeFrieren() : FrierenCard(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(context);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

public sealed class DefendFrieren() : FrierenCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Defend];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

public sealed class Flow() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target).Execute(context);
        foreach (var pile in new[] { PileType.Draw, PileType.Discard })
        {
            var mana = ManaCmd.InPile(Owner, pile).FirstOrDefault();
            if (mana != null) await CardPileCmd.Add(mana, PileType.Hand);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

public sealed class RapidChant() : FrierenCard(0, CardType.Skill, CardRarity.Common, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, 1).Count > 0;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, 1);
        await ManaCmd.Generate(Owner, PileType.Draw, 1);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class DefensiveMagic() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, 1).Count > 0;
    protected override void AddExtraArgsToDescription(MegaCrit.Sts2.Core.Localization.LocString description) => description.Add("Upgraded", IsUpgraded);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, 1);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        var card = Owner.Creature.CombatState!.CreateCard<SecondDefensiveMagic>(Owner);
        if (IsUpgraded) card.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

public sealed class SecondDefensiveMagic() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false), IChantCard
{
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, 1).Count > 0;
    protected override string ArtName => nameof(DefensiveMagic);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, 1);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(2);
}

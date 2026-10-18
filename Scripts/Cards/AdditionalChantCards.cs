using FrierenMod.Core;
using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Cards;

public abstract class NormalChantCard(int cost, CardRarity rarity, int chant)
    : FrierenCard(cost, CardType.Skill, rarity, TargetType.Self), IChantCard
{
    protected virtual int Intensity => chant;
    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("Intensity", Intensity);
        description.Add("Upgraded", IsUpgraded);
    }
    protected override bool IsPlayable => Owner != null && ChantCmd.Options(Owner, Intensity).Count > 0;
}

public sealed class ContinualChant() : NormalChantCard(0, CardRarity.Uncommon, 3), ILegendaryCard
{
    protected override int Intensity => IsUpgraded ? 4 : 3;
    protected override bool IsPlayable => Owner != null && LegendaryCards.ChantsThisTurn(Owner) > 0 &&
        (Owner.Creature.GetPower<BanManaGainPower>() == null || base.IsPlayable);
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ManaCmd.Generate(Owner, PileType.Draw, Intensity);
        await ChantCmd.Choose(context, play, Intensity);
    }
}

public sealed class FlowerFieldSpell() : NormalChantCard(1, CardRarity.Common, 2)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (await ChantCmd.Choose(context, play, Intensity) == null) return;
        var card = CombatState!.CreateCard<BlueMoonWeed>(Owner);
        if (IsUpgraded) card.UpgradeInternal();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}

public sealed class BlueMoonWeed() : FrierenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) => await CardPileCmd.Draw(context, DynamicVars.Cards.BaseValue, Owner);
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

public sealed class ManaBarricade() : NormalChantCard(1, CardRarity.Common, 2)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var option = await ChantCmd.Choose(context, play, Intensity);
        if (option?.Slot == ManaSlot.Draw) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

public sealed class LureTheEnemyInDeep() : NormalChantCard(2, CardRarity.Uncommon, 2)
{
    private bool _returnToHand;
    protected override int Intensity => IsUpgraded ? 1 : 2;
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        _returnToHand = (await ChantCmd.Choose(context, play, Intensity))?.Slot == ManaSlot.Hand;
        await PlayerCmd.GainEnergy(Intensity, Owner);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card == this && _returnToHand)
        {
            _returnToHand = false;
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class PerfectDefensiveMagic() : NormalChantCard(2, CardRarity.Rare, 3)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(10, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await ChantCmd.Choose(context, play, Intensity);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        await PowerCmd.Apply<BufferPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5);
}

public sealed class TrueColours() : NormalChantCard(1, CardRarity.Rare, 4)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        if (await ChantCmd.Choose(context, play, Intensity) != null)
        {
            await CardPileCmd.Draw(context, 3, Owner);
            await ManaCmd.Generate(Owner, PileType.Hand, 3);
        }
        await PlayerCmd.GainEnergy(3, Owner);
        await ManaCmd.Generate(Owner, PileType.Draw, 3);
        await ManaCmd.Generate(Owner, PileType.Discard, 3);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class FullAhead() : FrierenCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await PowerCmd.Apply<FullAheadPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class Simmering() : FrierenCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await PowerCmd.Apply<SimmeringPower>(context, Owner.Creature, 1, Owner.Creature, this);
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

public sealed class HalfTheSky() : FrierenCard(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy), IChantListener
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) =>
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (Pile?.Type == PileType.Discard) await CardPileCmd.Add(this, PileType.Hand);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

public sealed class Confrontation() : FrierenCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play) => await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    public override Task BeforeCardPlayed(CardPlay play)
    {
        if (play.Card.Owner == Owner && play.Card is IChantCard && Pile?.Type == PileType.Hand) DynamicVars.Block.BaseValue += 3;
        return Task.CompletedTask;
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

public sealed class Imagination() : FrierenCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await PowerCmd.Apply<ChantWithoutManaPower>(context, Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<BanManaGainPower>(context, Owner.Creature, 1, Owner.Creature, this);
        await PowerCmd.Apply<ManaInsteadOfEnergyPower>(context, Owner.Creature, 1, Owner.Creature, this);
    }
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}

public sealed class PreparedPosture() : FrierenCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(context);
        var chant = Owner.PlayerCombatState!.DrawPile.Cards.FirstOrDefault(c => c is IChantCard);
        if (chant == null) return;
        await CardPileCmd.Add(chant, PileType.Draw, CardPilePosition.Top);
        await CardPileCmd.Draw(context, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

public sealed class FolkGrimoire() : FrierenCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override void AddExtraArgsToDescription(LocString description) => description.Add("Discount", IsUpgraded ? 2 : 1);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        var chants = ModelDb.CardPool<FrierenMod.Scripts.Characters.FrierenCardPool>().AllCards
            .Where(c => c is IChantCard && c.Rarity != CardRarity.Token).OrderBy(c => c.Id.Entry).ToArray();
        var chant = CombatState!.CreateCard(chants[Owner.RunState.Rng.CombatCardGeneration.NextInt(chants.Length)], Owner);
        foreach (var card in new[] { chant, LegendaryCards.CreateRandom(Owner) })
        {
            card.EnergyCost.AddThisTurn(IsUpgraded ? -2 : -1);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }
}

public sealed class HellFireSummoning() : FrierenCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];
    public override async Task BeforeCombatStart()
    {
        if (Pile?.Type != PileType.Draw) return;
        var power = Owner.Creature.GetPower<SealPower>();
        if (power == null)
            power = await PowerCmd.Apply<SealPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, this);
        power!.Cards.Add(this);
        Pile.RemoveInternal(this);
    }
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        int hits = LegendaryCards.ChantsThisTurn(Owner) * 2;
        if (hits > 0) await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).WithHitCount(hits).Execute(context);
        EnergyCost.AddThisTurn(1);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

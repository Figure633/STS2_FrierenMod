using BaseLib.Abstracts;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Powers;

public abstract class FrierenPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => $"res://FrierenMod/images/powers/{GetType().Name}.png";
    public override string CustomBigIconPath => CustomPackedIconPath;
}

public abstract class TurnOnlyPower : FrierenPower
{
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}

public sealed class ConcentrationPower : FrierenPower
{
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player && play.Card is not Mana && play.Card is not ISpeedCard)
            await PowerCmd.Decrement(this);
    }
}

public sealed class WeakenedChantPower : TurnOnlyPower
{
    public override PowerType Type => PowerType.Debuff;
}
public sealed class ChantWithoutManaPower : TurnOnlyPower;
public sealed class ChantWithoutManaTimesPower : TurnOnlyPower;
public sealed class BanManaGainPower : TurnOnlyPower;

public sealed class ManaInsteadOfEnergyPower : TurnOnlyPower
{
    private bool _calculating;
    public int Cost(CardModel card)
    {
        if (card.EnergyCost.CostsX) return 0;
        _calculating = true;
        try { return card.EnergyCost.GetAmountToSpend(); }
        finally { _calculating = false; }
    }
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        bool replace = !_calculating && card.Owner == Owner.Player && !card.EnergyCost.CostsX;
        modifiedCost = replace ? 0 : originalCost;
        return replace;
    }
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
        card.Owner != Owner.Player || autoPlayType != AutoPlayType.None || ManaCmd.Total(card.Owner) >= Cost(card);
}

public sealed class SealPower : FrierenPower, IChantListener
{
    private List<CardModel>? _cards;
    public List<CardModel> Cards => _cards ??= [];
    public override int DisplayAmount => Cards.Count;
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (LegendaryCards.ChantsThisTurn(Owner.Player!) < 5) return;
        foreach (var card in Cards.ToArray()) await CardPileCmd.Add(card, PileType.Hand);
        Cards.Clear();
        await PowerCmd.Remove(this);
    }
}
public sealed class SynchroWithoutManaPower : FrierenPower
{
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position) =>
        card.Owner == Owner.Player && card is Mana ? (PileType.Discard, position) : (pileType, position);
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner.Player && play.Card is Mana) await PowerCmd.Decrement(this);
    }
}

public sealed class StrikeUpPower : FrierenPower
{
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) && cardSource?.Tags.Contains(CardTag.Strike) == true ? Amount : 0;
}

public sealed class MetallicizePower : FrierenPower
{
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }
}

public sealed class PhantasmalPower : FrierenPower
{
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player != Owner.Player || AmountOnTurnStart == 0) return;
        await PowerCmd.Apply<DoubleDamagePower>(context, Owner, 1, Owner, null);
        await PowerCmd.Decrement(this);
    }
}

public sealed class RestoreStrengthPower : TurnOnlyPower
{
    public override async Task AfterSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;
        await PowerCmd.Apply<StrengthPower>(context, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }
}

public sealed class MantraPower : FrierenPower
{
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount, Creature? applier, CardModel? source)
    {
        if (power != this || Amount < 10) return;
        await PowerCmd.ModifyAmount(context, this, -10, Owner, source);
        if (Owner.GetPower<DivinityPower>() == null)
        {
            await PlayerCmd.GainEnergy(3, Owner.Player!);
            await PowerCmd.Apply<DivinityPower>(context, Owner, 1, Owner, source);
        }
    }
}

public sealed class DivinityPower : FrierenPower
{
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? source) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) ? 3 : 1;
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player && AmountOnTurnStart > 0) await PowerCmd.Remove(this);
    }
}

public sealed class PuzzlePower : TurnOnlyPower
{
    // 复用一代各阶段的名称和碎片提示，实际累积及结算仍由 Amount 驱动。
    public override LocString Title => Amount is >= 1 and <= 3 ? new("powers", $"{Id.Entry}.{Amount}.title") : base.Title;
    public override LocString Description => Amount is >= 1 and <= 3 ? new("powers", $"{Id.Entry}.{Amount}.description") : base.Description;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (Amount == 3 && participants.Contains(Owner))
            await CreatureCmd.Kill(CombatState.Enemies.Where(x => x.IsAlive).ToArray());
    }
}

public interface IChantListener
{
    Task AfterChant(PlayerChoiceContext context, CardPlay play);
}

public sealed class FullAheadPower : FrierenPower, IChantListener
{
    private bool _triggered;
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        if (_triggered) return;
        _triggered = true;
        await PowerCmd.Apply<StrengthPower>(context, Owner, Amount, Owner, play.Card);
        await PowerCmd.Apply<DexterityPower>(context, Owner, Amount, Owner, play.Card);
    }
    public override Task AfterPlayerTurnStart(PlayerChoiceContext context, Player player)
    {
        if (player == Owner.Player) _triggered = false;
        return Task.CompletedTask;
    }
}

public interface ISpeedCard;

public sealed class ThunderPower : FrierenPower;

public sealed class SimmeringPower : FrierenPower, IChantListener
{
    public async Task AfterChant(PlayerChoiceContext context, CardPlay play)
    {
        var player = Owner.Player!;
        var cards = player.PlayerCombatState!.DrawPile.Cards.Where(x => x is ILegendaryCard).Take(Amount).ToArray();
        foreach (var card in cards)
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
            await CardPileCmd.Draw(context, player);
        }
    }
}

public sealed class PaperPlanePower : FrierenPower
{
    private int _consecutive;
    private int _attacks;
    public override int DisplayAmount => _attacks;
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        dealer == Owner && props.HasFlag(ValueProp.Move) && _attacks > 0 ? 2 : 1;
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner != Owner.Player) return Task.CompletedTask;
        if (play.Card.Type == CardType.Attack) _attacks = Math.Max(0, _attacks - 1);
        if (play.Card is Mana)
        {
            if (++_consecutive == 4) { _attacks += Amount; _consecutive = 0; }
        }
        else _consecutive = 0;
        return Task.CompletedTask;
    }
}

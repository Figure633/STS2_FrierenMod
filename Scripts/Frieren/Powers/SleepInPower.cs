using FrierenMod.Scripts.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace FrierenMod.Scripts.Powers;

public sealed class SleepInPower : FrierenPower
{
    public override PowerType Type => PowerType.Debuff;
    public int StoredEnergy { get; private set; }
    public override PowerStackType StackType => PowerStackType.Single;
    public override LocString Description { get { var text = base.Description; text.Add("StoredEnergy", StoredEnergy); return text; } }
    public override int DisplayAmount => StoredEnergy;
    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) => card.Owner != Owner.Player || card is WakeUp;
    public override Task BeforeSideTurnEndVeryEarly(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner)) {
            StoredEnergy += Owner.Player!.PlayerCombatState!.Energy;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }
    public int TakeEnergy() { int energy = StoredEnergy; StoredEnergy = 0; InvokeDisplayAmountChanged(); return energy; }
}

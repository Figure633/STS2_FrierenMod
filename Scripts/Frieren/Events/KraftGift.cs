using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using FrierenMod.Scripts.Shared.Compatibility;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

public sealed class KraftGift : FrierenStoryEvent
{
    protected override string[] Portraits => ["Kraft1", "Kraft2"];
    public override bool IsAllowed(IRunState runState) => base.IsAllowed(runState) &&
        runState.Players.All(p => p.Creature.MaxHp >= 80 || p.Gold >= 300 || p.Deck.Cards.Count >= 30);
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("MEAT", Meat, Owner!.Creature.MaxHp >= 80).ThatDoesDamage(10), Choice("MEMBERSHIP", Membership, Owner!.Gold >= 300),
        CardChoice<Writhe>("RING", Ring, Owner!.Deck.Cards.Count >= 30), Choice("LEAVE", Leave)
    ];
    private async Task Meat() {
        await GameApi.Damage(new ThrowingPlayerChoiceContext(),Owner!.Creature,10,ValueProp.Unblockable | ValueProp.Unpowered,null,null);
        if (Owner.Creature.IsDead) return;
        await RelicCmd.Obtain<MeatOnTheBone>(Owner); Finish("MEAT", 1);
    }
    private async Task Membership() { await PlayerCmd.LoseGold(100,Owner!); await RelicCmd.Obtain<MembershipCard>(Owner!); Finish("MEMBERSHIP", 1); }
    private async Task Ring() { await CardPileCmd.AddCurseToDeck<Writhe>(Owner!); await RelicCmd.Obtain<KraftSerpentRing>(Owner!); Finish("RING", 1); }
    private Task Leave() { Finish("LEAVE", 0); return Task.CompletedTask; }
}

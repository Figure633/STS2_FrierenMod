using FrierenMod.Scripts.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

public sealed class KraftGift : FrierenStoryEvent
{
    protected override string[] Portraits => ["Kraft1", "Kraft2"];
    public override bool IsAllowed(IRunState runState) => base.IsAllowed(runState) && runState.CurrentActIndex is 1 or 2;
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        Choice("MEAT", Meat, Owner!.Creature.MaxHp >= 80), Choice("MEMBERSHIP", Membership, Owner!.Gold >= 300),
        Choice("RING", Ring, Owner!.Deck.Cards.Count >= 30), Choice("LEAVE", Leave)
    ];
    private async Task Meat() { await RelicCmd.Obtain<MeatOnTheBone>(Owner!); Finish("MEAT", 1); }
    // 一代这里只检查金币，并不扣钱。
    private async Task Membership() { await RelicCmd.Obtain<MembershipCard>(Owner!); Finish("MEMBERSHIP", 1); }
    private async Task Ring() { await RelicCmd.Obtain<KraftSerpentRing>(Owner!); Finish("RING", 1); }
    private Task Leave() { Finish("LEAVE", 0); return Task.CompletedTask; }
}

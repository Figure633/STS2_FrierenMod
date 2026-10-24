using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Encounters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FrierenMod.Scripts.Events;

public sealed class MimicFight : FrierenStoryEvent
{
    protected override string[] Portraits => ["MimicEvent1", "MimicEvent2", "MimicEvent3", "MimicEvent4"];
    public override bool IsShared => true;
    public override bool IsAllowed(IRunState runState) => base.IsAllowed(runState) && runState.CurrentActIndex == 1;
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [Choice("IMPULSE", Open), Choice("REASON", Open)];
    private Task Open()
    {
        SetEventState(PageDescription("CAUGHT"), [Choice("FIGHT", Fight), Choice("EXPLODE", Explode).ThatDoesDamage(15), Choice("RESCUE", Rescue)]);
        ShowPortrait(1); return Task.CompletedTask;
    }
    private Task Fight()
    {
        EnterCombatWithoutExitingEvent<MimicEncounter>([], shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
    private async Task Explode()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner!.Creature, 15, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        if (Owner.Creature.IsDead) return;
        await AddCard<RingletForm>(); Finish("EXPLODE", 2);
    }
    private Task Rescue() { Finish("RESCUE", 3); return Task.CompletedTask; }
}

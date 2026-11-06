using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Events;

public sealed class DemonArmyAttack : FrierenStoryEvent
{
    private EncounterModel? _boss;
    public static IReadOnlyList<EncounterModel> BossPool => ModelDb.Act<Hive>().BossDiscoveryOrder
        .Where(e => e.GetType().Assembly == typeof(Hive).Assembly).OrderBy(e => e.Id.Entry, StringComparer.Ordinal).ToArray();
    // 原生共享事件为所有玩家提供相同种子；只抽一次，显示的Boss与实际交战保持一致。
    public EncounterModel SelectedBoss => _boss ??= BossPool[Rng.NextInt(BossPool.Count)];
    public override bool IsShared => true;
    public override bool IsAllowed(IRunState runState) => base.IsAllowed(runState) && runState.CurrentActIndex == 2;
    protected override string[] Portraits => ["DemonArmyAttack"];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var triumph = Choice("TRIUMPH", () => Fight(SelectedBoss));
        triumph.Description.Add("Boss", SelectedBoss.Title.GetFormattedText());
        triumph.HoverTips = [HoverTipFactory.FromPower<BarricadePower>(), HoverTipFactory.FromPower<PlatingPower>()];
        var awaken = Choice("AWAKEN", () => Fight(ModelDb.Encounter<InfestedPrismsElite>()));
        awaken.HoverTips = [HoverTipFactory.FromPower<AfterimagePower>()];
        var unhurt = Choice("UNHURT", () => Fight(ModelDb.Encounter<HunterKillerNormal>()));
        unhurt.HoverTips = [HoverTipFactory.FromPower<ArtifactPower>()];
        return [triumph, awaken, unhurt];
    }

    private Task Fight(EncounterModel encounter)
    {
        EnterCombatWithoutExitingEvent(encounter.ToMutable(), [], shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}

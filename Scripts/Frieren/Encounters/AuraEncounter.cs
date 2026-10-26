using BaseLib.Abstracts;
using FrierenMod.Scripts.Monsters;
using FrierenMod.Scripts.Presentation;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Encounters;

public sealed class AuraEncounter() : CustomEncounterModel(RoomType.Boss)
{
    public override bool IsValidForAct(ActModel act) => act is Hive;
    public override string CustomScenePath => "res://FrierenMod/scenes/aura_encounter.tscn";
    public override IReadOnlyList<string> Slots => ["Boss", "Soldier1", "Soldier2"];
    public override string BossNodePath => "res://FrierenMod/images/monsters/Aura_node";
    public override string CustomRunHistoryIconPath => BossNodePath + ".png";
    public override string CustomRunHistoryIconOutlinePath => BossNodePath + "_outline.png";
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Aura>(), ModelDb.Monster<AuraSoldier>()];
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var guard = (AuraSoldier)ModelDb.Monster<AuraSoldier>().ToMutable(); guard.GuardFirst = true;
        return [(ModelDb.Monster<Aura>().ToMutable(), "Boss"), (ModelDb.Monster<AuraSoldier>().ToMutable(), "Soldier1"), (guard, "Soldier2")];
    }
}

// 首次遭遇顺序会覆盖 GenerateRooms 随机池，必须在原生发现顺序处理之后再次保证固定Boss。
[HarmonyPatch(typeof(ActModel), nameof(ActModel.ApplyDiscoveryOrderModifications))]
public static class AuraFixedBoss
{
    private static void Postfix(ActModel __instance)
    {
        if (__instance is Hive && !PresentationSettings.AllowOtherActTwoBosses &&
            FrierenEncounterEligibility.IsFrierenRun(RunManager.Instance.DebugOnlyGetState()))
            __instance.SetBossEncounter(ModelDb.Encounter<AuraEncounter>());
    }
}

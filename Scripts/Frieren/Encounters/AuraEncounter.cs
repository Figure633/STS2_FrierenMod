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

// 在原生跑局生成（含发现顺序和双Boss抽取）完成时只设置一次；此后地图、存档和战斗共用原生 RoomSet。
[HarmonyPatch(typeof(RunManager), nameof(RunManager.GenerateRooms))]
public static class AuraFixedBoss
{
    private static void Postfix(RunManager __instance)
    {
        var run = __instance.DebugOnlyGetState();
        if (PresentationSettings.AllowOtherActTwoBosses || !FrierenEncounterEligibility.IsFrierenRun(run)) return;
        foreach (var act in run!.Acts)
        {
            if (act is Hive) act.SetBossEncounter(ModelDb.Encounter<AuraEncounter>());
            if (act is not Glory) continue;
            // 原生已经抽到复制体时保留它的原位置；不再事后随机交换前后Boss。
            if (act.BossEncounter is SpiegelFrierenEncounter || act.SecondBossEncounter is SpiegelFrierenEncounter) continue;
            act.SetBossEncounter(ModelDb.Encounter<SpiegelFrierenEncounter>());
        }
    }
}

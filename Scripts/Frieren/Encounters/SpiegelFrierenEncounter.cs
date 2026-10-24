using BaseLib.Abstracts;
using FrierenMod.Scripts.Monsters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace FrierenMod.Scripts.Encounters;

public sealed class SpiegelFrierenEncounter() : CustomEncounterModel(RoomType.Boss)
{
    public override bool IsValidForAct(ActModel act) => act is Glory;
    public override string CustomScenePath => "res://FrierenMod/scenes/spiegel_encounter.tscn";
    public override IReadOnlyList<string> Slots => ["Boss", "Bird1", "Bird2"];
    public override string BossNodePath => "res://FrierenMod/images/monsters/Spiegel_node";
    public override string CustomRunHistoryIconPath => BossNodePath + ".png";
    public override string CustomRunHistoryIconOutlinePath => BossNodePath + "_outline.png";
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<SpiegelFrieren>(), ModelDb.Monster<DampCultist>()];
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() => [(ModelDb.Monster<SpiegelFrieren>().ToMutable(), "Boss")];
}

using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using FrierenMod.Scripts.Encounters;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Relics;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.Ancients;

public sealed class Serie : CustomAncientModel
{
    public static IReadOnlyList<RelicModel> Gifts => [ModelDb.Relic<IcicleCherryBlossom>(),
        ModelDb.Relic<ScaleOfObedience>(), ModelDb.Relic<ComfortableBed>(), ModelDb.Relic<HimmelGravestone>()];
    protected override OptionPools MakeOptionPools => new(MakePool(Gifts.ToArray()));
    // 百科每件只登记一次，不能使用重复三次的选项池枚举。
    public override IEnumerable<EventOption> AllPossibleOptions => Gifts.Select(r => RelicOption(r.ToMutable()));
    public override bool IsValidForAct(ActModel act)
    {
        var run = CurrentGeneratingRunState.State ?? RunManager.Instance.DebugOnlyGetState();
        return act.Index == 2 && (run == null || FrierenEncounterEligibility.IsFrierenRun(run));
    }
    public override string CustomScenePath => "res://FrierenMod/scenes/ancients/serie.tscn";
    public override string CustomMapIconPath => "res://FrierenMod/images/ancients/serie_node.png";
    public override string CustomMapIconOutlinePath => "res://FrierenMod/images/ancients/serie_node_outline.png";
    public override string CustomRunHistoryIconPath => "res://FrierenMod/images/ancients/serie_history.png";
    public override string CustomRunHistoryIconOutlinePath => "res://FrierenMod/images/ancients/serie_history_outline.png";
    public override Color ButtonColor => new("242417D9");
    public override Color DialogueColor => new("50543A");
    public override IEnumerable<CharacterModel> AnyCharacterDialogueBlacklist => [ModelDb.Character<Frieren>()];
    protected override AncientDialogueSet DefineDialogues() => new()
    {
        FirstVisitEverDialogue = null,
        CharacterDialogues = new() { [CharKey<Frieren>()] = AncientDialogueUtil.GetDialoguesForKey("ancients", $"{Id.Entry}.talk.{CharKey<Frieren>()}.") },
        AgnosticDialogues = AncientDialogueUtil.GetDialoguesForKey("ancients", $"{Id.Entry}.talk.ANY.")
    };

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 委托可能已给出其中的遗物；只抽未持有的，少于三件时展示剩余件数。
        var remaining = Gifts.Where(r => !Owner!.Relics.Any(owned => owned.Id == r.Id)).ToList();
        var options = new List<EventOption>();
        while (remaining.Count > 0 && options.Count < 3)
        {
            int index = Rng.NextInt(remaining.Count);
            options.Add(RelicOption(remaining[index].ToMutable()));
            remaining.RemoveAt(index);
        }
        return options; // 全部持有时由原生先古流程进入结束页。
    }
}

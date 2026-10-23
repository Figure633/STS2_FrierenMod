using BaseLib.Abstracts;
using FrierenMod.Scripts.Mechanics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace FrierenMod.Scripts.Events;

// 一代“新的开始”前置事件；不加入随机事件池，页面进度直接来自玩家魔法书存档。
public sealed class ReadingGrimoireEvent() : CustomEventModel(false)
{
    private const string Key = "FRIERENMOD-READING_GRIMOIRE_EVENT";
    public override string LocTable => "frieren";
    public override string CustomInitialPortraitPath => "res://FrierenMod/images/events/ReadingGrimoire.png";
    public override bool IsShared => true;
    public override bool IsAllowed(IRunState runState) => false;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [CurrentOption()];

    protected override void SetInitialEventState(bool isPreFinished) => RefreshPage();

    private string Page {
        get {
            var deck = MagicDeckStore.Read(Owner!);
            return !deck.SetupCompleted ? "BOOK_" + deck.DraftSlot : !deck.StartingPropsGranted ? "TOOLS" : "DONE";
        }
    }

    private EventOption CurrentOption() => Page switch {
        "TOOLS" => new(this, ObtainTools, Key + ".pages.TOOLS.options.OBTAIN"),
        "DONE" => new(this, Depart, Key + ".pages.DONE.options.DEPART", isProceed: true),
        _ => new(this, InspectBook, Key + ".pages." + Page + ".options.INSPECT")
    };

    private void RefreshPage() => SetEventState(L10NLookup(Key + ".pages." + Page + ".description"), [CurrentOption()]);

    private async Task InspectBook()
    {
        await MagicDeckCmd.ChooseStartingEventFactor(new BlockingPlayerChoiceContext(), Owner!);
        if (ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), Owner!.RunState) && !RunManager.Instance.IsCleaningUp)
            RefreshPage();
    }

    private async Task ObtainTools()
    {
        await MagicDeckCmd.GrantStartingProps(Owner!);
        if (ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), Owner!.RunState) && !RunManager.Instance.IsCleaningUp)
            RefreshPage();
    }

    private async Task Depart()
    {
        var run = Owner!.RunState;
        if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run) || !MagicDeckStore.Read(Owner).StartingPropsGranted) return;
        var ancient = run.Act.PullAncient();
        ReadingGrimoireEntrance.Stage.Set(Owner, 2);
        SetEventFinished(L10NLookup(Key + ".pages.DONE.description"));
        // 使用原生 Proceed 路径，在事件同步任务之外切房，避免退出房间等待自身。
        await RunManager.Instance.EnterRoom(new EventRoom(ancient));
        run.CurrentMapPointHistoryEntry?.Rooms.Add(new MapPointRoomHistoryEntry { RoomType = RoomType.Event, ModelId = ancient.Id });
        if (RunManager.Instance.ShouldSave) await SaveManager.Instance.SaveRun(null, saveProgress: false);
    }
}

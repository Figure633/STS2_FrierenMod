using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Runs;
using NativeWisp = MegaCrit.Sts2.Core.Models.Relics.LostWisp;

namespace FrierenMod.Scripts.Events;

// 临时插画与文字特效验收：原生事件模型/按钮，奖励选项与结算复用迷失鬼火。
// 只通过控制台打开；不让占位叙事进入正常跑局的随机事件池。
public abstract class EventArtPreview : CustomEventModel
{
    public static readonly string[] Images = ["BugKnight", "DragonBall", "elderRing", "jojo", "nier", "onepiece",
        "p5r", "sekiro", "starve", "terraria", "trail", "Zelda"];
    protected abstract int StartingIndex { get; }
    private int _pageOffset;
    public int ArtIndex => (StartingIndex + _pageOffset + Images.Length) % Images.Length;
    public string ArtLabel => $"{ArtIndex + 1}/12 · {Images[ArtIndex]}";
    public override string CustomInitialPortraitPath => ArtPath(ArtIndex);
    public static string ArtPath(int index) => Images[index] == "Zelda"
        ? "res://FrierenMod/images/events/Zelda.png" : $"res://FrierenMod/images/events/Preview{Images[index]}.png";
    public override bool IsAllowed(IRunState runState) => false;
    public override LocString InitialDescription => new("events", "FRIERENMOD-EVENT_ART_PREVIEW.pages.INITIAL.description");
    public override IEnumerable<LocString> GameInfoOptions {
        get {
            foreach (var option in new[] { "CLAIM", "SEARCH" }) {
                var text = new LocString("events", $"LOST_WISP.pages.INITIAL.options.{option}.description");
                DynamicVars.AddTo(text);
                yield return text;
            }
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(60),
        new StringVar("Relic", ModelDb.Relic<NativeWisp>().Title.GetFormattedText()),
        new StringVar("Curse", ModelDb.Card<Decay>().Title)];
    public override void CalculateVars() => DynamicVars.Gold.BaseValue = 60 + Rng.NextInt(-15, 16);
    public override IEnumerable<string> GetAssetPaths(IRunState runState) =>
        base.GetAssetPaths(runState).Concat(Enumerable.Range(0, Images.Length).Select(ArtPath));

    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [
        new(this, Previous, "FRIERENMOD-EVENT_ART_PREVIEW.pages.INITIAL.options.PREVIOUS"),
        new(this, Next, "FRIERENMOD-EVENT_ART_PREVIEW.pages.INITIAL.options.NEXT"),
        new(this, Claim, "LOST_WISP.pages.INITIAL.options.CLAIM",
            HoverTipFactory.FromRelic<NativeWisp>().Concat(HoverTipFactory.FromCardWithCardHoverTips<Decay>())),
        new(this, Search, "LOST_WISP.pages.INITIAL.options.SEARCH"),
        new(this, Leave, "FRIERENMOD-EVENT_ART_PREVIEW.pages.INITIAL.options.LEAVE")];

    private Task Previous() => Turn(-1);
    private Task Next() => Turn(1);
    private Task Turn(int direction)
    {
        AssertMutable();
        _pageOffset = (_pageOffset + direction + Images.Length) % Images.Length;
        // 刷新原生选项以解除点击后的禁用状态；不改金币随机数或生成奖励。
        SetEventState(InitialDescription, GenerateInitialOptions());
        if (Node is NEventLayout layout) {
            layout.SetPortrait(GD.Load<Texture2D>(CustomInitialPortraitPath));
            layout.SetTitle(ArtLabel);
        }
        return Task.CompletedTask;
    }
    private async Task Claim()
    {
        await CardPileCmd.AddCurseToDeck<Decay>(Owner!);
        await RelicCmd.Obtain<NativeWisp>(Owner!);
        SetEventFinished(new LocString("events", "LOST_WISP.pages.CLAIM.description"));
    }
    private async Task Search()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner!);
        SetEventFinished(new LocString("events", "LOST_WISP.pages.SEARCH.description"));
    }
    private Task Leave()
    {
        SetEventFinished(new LocString("events", "FRIERENMOD-EVENT_ART_PREVIEW.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}

using BaseLib.Config.UI;
using FrierenMod.Core;
using FrierenMod.Scripts.Presentation;
using FrierenMod.Scripts.Cards;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Ftue;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using System.Threading;

namespace FrierenMod.Scripts.UI;

// 继承原版三页教程场景，保留遮罩、翻页按钮和 NFtue 输入管理。
public partial class FrierenTutorial : NCombatRulesFtue
{
    public const string TutorialId = "frieren_magic_deck_ftue";
    private readonly TaskCompletionSource<bool> _completion = new();
    private readonly List<NCard> _illustrations = [];
    private readonly List<Control> _diagrams = [];
    private Player _player = null!;
    private NButton _previous = null!, _next = null!;
    private NConfigButton _finish = null!;
    private Control _image = null!;
    private int _page;
    private bool _closed, _accepted;

    private static string Text(string key) => new LocString("frieren", key).GetFormattedText();

    // 原版教程开关不能表示玩家已学过本模组；模组仅使用自己的已读标记。
    public static bool ShouldAutoShow(Player player) => LocalContext.IsMe(player) &&
        !SaveManager.Instance.Progress.FtueCompleted.Contains(TutorialId);

    public static async Task<bool> Show(Player player, bool replay = false, CancellationToken cancellation = default)
    {
        if (!LocalContext.IsMe(player)) return true;
        if (!replay && !ShouldAutoShow(player)) return true;
        var modal = NModalContainer.Instance;
        // 无界面的测试不弹窗；实机缺少容器时留给界面兜底重试。
        if (modal == null) return TestMode.IsOn;
        if (modal.OpenModal != null || cancellation.IsCancellationRequested) return false;
        var tutorial = GD.Load<PackedScene>("res://FrierenMod/scenes/frieren_tutorial.tscn").Instantiate<FrierenTutorial>();
        tutorial._player = player;
        modal.Add(tutorial);
        try { return await tutorial._completion.Task.WaitAsync(cancellation); }
        finally {
            if (GodotObject.IsInstanceValid(tutorial) && !tutorial._closed) tutorial.Finish(false);
        }
    }

    public override void _Ready()
    {
        // 不调用固定为原版战斗说明的 NCombatRulesFtue._Ready/Start。
        _previous = GetNode<NButton>("LeftArrow");
        _next = GetNode<NButton>("RightArrow");
        _previous.Released += _ => Turn(-1);
        _next.Released += _ => Turn(1);
        var image = GetNode<TextureRect>("Image");
        image.Texture = null;
        _image = image;
        _finish = new NConfigButton { Name = "FinishTutorial" };
        _finish.Initialize(Text("TUTORIAL_SKIP"), () => Finish(true));
        AddChild(_finish);
        _finish.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _finish.OffsetLeft = -364; _finish.OffsetRight = -40;
        _finish.OffsetTop = -104; _finish.OffsetBottom = -40;
        RefreshPage();
    }

    public override void _Input(InputEvent input)
    {
        if (!IsVisibleInTree()) return;
        if (input.IsActionPressed(MegaInput.cancel)) Finish(true);
        else if (input.IsActionPressed(MegaInput.left)) Turn(-1);
        else if (input.IsActionPressed(MegaInput.right)) Turn(1);
        else return;
        GetViewport().SetInputAsHandled();
    }

    private void Turn(int direction)
    {
        if (_page == 2 && direction > 0) { Finish(true); return; }
        _page = Math.Clamp(_page + direction, 0, 2);
        RefreshPage();
    }

    private void RefreshPage()
    {
        GetNode<MegaLabel>("Header").SetTextAutoSize(Text("TUTORIAL_TITLE"));
        var page = new LocString("frieren", "TUTORIAL_PAGE");
        page.Add("Current", _page + 1); page.Add("Total", 3);
        GetNode<MegaLabel>("PageCount").SetTextAutoSize(page.GetFormattedText());
        GetNode<MegaRichTextLabel>("%Description").SetTextAutoSize(Text("TUTORIAL_BODY_" + (_page + 1)));
        _previous.Visible = _page > 0;
        if (_page > 0) _previous.Enable(); else _previous.Disable();
        _next.Show(); _next.Enable();
        _finish.GetNode<MegaLabel>("Label").SetTextAutoSize(Text(_page == 2 ? "TUTORIAL_DONE" : "TUTORIAL_SKIP"));
        ReleaseIllustrations();
        CardModel Card<T>() where T : CardModel {
            var card = ModelDb.Card<T>().ToMutable(); card.Owner = _player; return card;
        }
        CardModel[] models = _page switch {
            0 => [Card<Mana>()],
            1 => [FactorCard.Create(_player, "BuildUpBody", ManaSlot.Draw), FactorCard.Create(_player, "BigFire", ManaSlot.Hand),
                FactorCard.Create(_player, "ImpeccableIntangible", ManaSlot.Discard)],
            _ => [MagicPropCard.Create(_player, "UnbelievableTool"), MagicPropCard.Create(_player, "ShadowDragonHorn")]
        };
        for (int i = 0; i < models.Length; i++) {
            var card = MagicBookScreen.CreateCard(models[i]);
            card.MouseBehaviorRecursive = MouseBehaviorRecursiveEnum.Disabled;
            card.FocusBehaviorRecursive = FocusBehaviorRecursiveEnum.Disabled;
            _image.AddChild(card); _illustrations.Add(card);
            card.Position = new Vector2(_image.Size.X * (i + 0.5f) / models.Length, _image.Size.Y / 2);
            card.Scale = Vector2.One * (_page == 0 ? .85f : models.Length == 2 ? .8f : .55f);
            if (_page == 0) card.Position -= new Vector2(_image.Size.X * .17f, 0);
            if (_page == 1) {
                var slot = ChantSelectionPresentation.CreateSlotVisual((ManaSlot)i);
                _image.AddChild(slot); _image.MoveChild(slot, 0); _diagrams.Add(slot);
                slot.Position = card.Position; slot.Scale = card.Scale;
            }
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            card.CardHighlight.AnimHideInstantly();
        }
        if (_page == 0) AddManaCounter();
        if (_page == 2) {
            var icon = new TextureRect { Name = "TutorialMagicDeckIcon", Texture = GD.Load<Texture2D>("res://FrierenMod/images/ui/magic_deck.png"),
                Position = new(_image.Size.X / 2 - 40, 4), Size = new(80, 80),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore };
            _image.AddChild(icon); _diagrams.Add(icon);
        }
    }

    private void AddManaCounter()
    {
        var counter = new Control { Name = "TutorialManaCounter", Position = new(_image.Size.X * .72f, _image.Size.Y / 2 - 132),
            Size = new(108, 264), MouseFilter = MouseFilterEnum.Ignore };
        _image.AddChild(counter); _diagrams.Add(counter);
        foreach (string layer in new[] { "MPLayer0", "MPWrap" }) counter.AddChild(new TextureRect {
            Texture = new AtlasTexture { Atlas = GD.Load<Texture2D>($"res://FrierenMod/images/ui/{layer}.png"), Region = new(88, 0, 80, 256) },
            Size = new(68, 264), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = MouseFilterEnum.Ignore });
        for (int i = 0; i < 3; i++) {
            var number = new Label { Text = "3", Position = new(0, 20 + 88 * i), Size = new(68, 36), HorizontalAlignment = HorizontalAlignment.Center };
            number.AddThemeFontSizeOverride("font_size", 27); number.AddThemeConstantOverride("outline_size", 7);
            number.AddThemeColorOverride("font_outline_color", Colors.Black); counter.AddChild(number);
            var label = new Label { Text = Text("SLOT_SHORT." + (ManaSlot)i), Position = new(70, 28 + 88 * i) };
            label.AddThemeFontSizeOverride("font_size", 16); counter.AddChild(label);
        }
    }

    public void Finish(bool accepted)
    {
        if (_closed) return;
        _closed = true; _accepted = accepted;
        if (accepted) SaveManager.Instance.MarkFtueAsComplete(TutorialId);
        if (ReferenceEquals(NModalContainer.Instance?.OpenModal, this)) CloseFtue();
        else this.QueueFreeSafely();
    }

    private void ReleaseIllustrations()
    {
        foreach (var card in _illustrations) card.QueueFreeSafely();
        _illustrations.Clear();
        foreach (var diagram in _diagrams) diagram.QueueFreeSafely();
        _diagrams.Clear();
    }

    public override void _ExitTree()
    {
        _closed = true;
        ReleaseIllustrations();
        base._ExitTree();
        _completion.TrySetResult(_accepted);
    }
}

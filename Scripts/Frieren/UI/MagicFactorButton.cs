using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.addons.mega_text;

namespace FrierenMod.Scripts.UI;

public partial class MagicFactorButton : NTopBarButton
{
    private bool _bookOpen;
    private int _itemCount = -1;

    public override void _Ready() => InitTopBarButton();
    protected override bool IsOpen() => _bookOpen;
    public void SetBookOpen(bool open) { _bookOpen = open; UpdateScreenOpen(); NHoverTipSet.Remove(this); }
    public void SetItemCount(int count) {
        if (_itemCount == count) return;
        _itemCount = count;
        GetNode<MegaLabel>("DeckCardCount").SetTextAutoSize(count.ToString());
    }

    private static HoverTip CreateHoverTip() => new(new LocString("frieren", "TIP.MAGIC_DECK.title"),
        new LocString("frieren", "TIP.MAGIC_DECK.description"));

    protected override void OnFocus()
    {
        base.OnFocus();
        if (NGame.Instance == null) return;
        NHoverTipSet.Remove(this);
        var tip = NHoverTipSet.CreateAndShow(this, CreateHoverTip());
        tip?.SetGlobalPosition(GlobalPosition + new Vector2(Size.X - tip.Size.X, Size.Y + 20));
    }

    protected override void OnUnfocus() { base.OnUnfocus(); NHoverTipSet.Remove(this); }

    public static MagicFactorButton Create(Control root)
    {
        // 复用原生卡组按钮的图标布局、材质和 NTopBarButton 悬停/禁用行为。
        var template = GD.Load<PackedScene>("res://scenes/ui/top_bar/top_bar_deck_button.tscn").Instantiate<NTopBarDeckButton>();
        var visual = template.GetNode<Control>("Control");
        template.RemoveChild(visual);
        visual.Owner = null;
        foreach (var child in visual.FindChildren("*", "", true, false)) child.Owner = null;
        var button = new MagicFactorButton {
            Name = "FrierenMagicBookButton", CustomMinimumSize = new(80, 80),
            FocusMode = Control.FocusModeEnum.All
        };
        button.AddChild(visual);
        // 保留原版牌组按钮右下角的数量标签和排版，统计魔法书中的全部因子与魔具。
        var countLabel = template.GetNode<MegaLabel>("DeckCardCount");
        template.RemoveChild(countLabel); countLabel.Owner = null;
        button.AddChild(countLabel);
        var icon = visual.GetNode<TextureRect>("Icon");
        icon.Texture = GD.Load<Texture2D>("res://FrierenMod/images/ui/magic_deck.png");
        icon.Material = (Material)icon.Material.Duplicate();
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        template.Free();

        var row = root.FindChild("RightAlignedStuff", true, false) as BoxContainer;
        if (row == null) {
            // 独立界面测试没有整套 GlobalUi，仍使用相同的原生按钮和自动排版。
            row = new HBoxContainer { Name = "FrierenTopBarRow", Alignment = BoxContainer.AlignmentMode.End,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(row);
            row.SetAnchorsPreset(Control.LayoutPreset.TopRight);
            row.OffsetLeft = -96; row.OffsetRight = -16; row.OffsetBottom = 80;
        }
        row.GrowHorizontal = Control.GrowDirection.Begin;
        row.AddChild(button);
        var map = row.GetNodeOrNull<Control>("Map");
        if (map != null) row.MoveChild(button, map.GetIndex());
        return button;
    }
}

using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace FrierenMod.Scripts.UI;

// 跟随原生遗物区的实际占用高度，始终放在其下方；全控件不接收鼠标输入。
public partial class NowPlayingNotice : PanelContainer
{
    private readonly Label _title = new() { MouseFilter = MouseFilterEnum.Ignore };
    public NowPlayingNotice()
    {
        Name = "NowPlayingNotice"; MouseFilter = MouseFilterEnum.Ignore;
        var style = new StyleBoxFlat { BgColor = new Color(0.035f,0.045f,0.065f,0.78f),
            ContentMarginLeft = 8, ContentMarginRight = 12, ContentMarginTop = 5, ContentMarginBottom = 5,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 };
        AddThemeStyleboxOverride("panel",style);
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation",8); AddChild(row);
        row.AddChild(new RecordDisc { CustomMinimumSize = new Vector2(22,22), SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore });
        _title.AddThemeFontSizeOverride("font_size",20);
        _title.AddThemeColorOverride("font_color",new Color("e1d5bd")); row.AddChild(_title);
        Position = new Vector2(24,172);
    }
    public void SetTitle(string title) => _title.Text = title;
    public override void _Process(double delta)
    {
        float y=172;
        if (NRun.Instance?.GlobalUi?.RelicInventory is { } inventory)
            foreach (var holder in inventory.RelicNodes)
                if (holder.IsVisibleInTree() && holder.GetGlobalRect() is var rect && rect.Position.X < 24+Size.X)
                    y=Math.Max(y,rect.End.Y+12);
        Position = new Vector2(24,y);
    }
}

using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Mechanics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace FrierenMod.Scripts.UI;

// 鼠标进入计数器即显示三槽卡面；浮层不接收输入，避免它自己打断悬停。
public sealed class ManaFactorPreview
{
    private readonly Control _root;
    private readonly Control _hud;
    private readonly Player _player;
    private readonly Control _view;
    private readonly List<NCard> _cards = [];
    private string _displayed = "";

    public ManaFactorPreview(Control root, Control hud, Player player)
    {
        _root = root; _hud = hud; _player = player;
        var panel = new Panel { Name = "FrierenFactorPreview", Visible = false, Size = new(850, 470),
            ZIndex = 90, ZAsRelative = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        var style = new StyleBoxFlat { BgColor = new Color("12151cee"), BorderColor = new Color("9a8862"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12 };
        panel.AddThemeStyleboxOverride("panel", style);
        _view = panel;
        root.AddChild(_view);
        root.Resized += Layout;
        _view.TreeExiting += () => { root.Resized -= Layout; ReleaseCards(); };
        Layout();
    }

    public void Show()
    {
        if (!_hud.IsVisibleInTree()) return;
        Populate(); Layout(); _view.Show();
    }
    public void Hide() => _view.Hide();
    public void Refresh()
    {
        if (_view.Visible) { Populate(); Layout(); }
    }

    private void Layout()
    {
        float scale = Math.Min(_root.Size.X / 1920, _root.Size.Y / 1080);
        scale = Math.Min(scale, (_root.Size.X - 160) / _view.Size.X);
        _view.Scale = Vector2.One * Math.Max(0.1f, scale);
        var size = _view.Size * _view.Scale;
        _view.Position = new(Math.Clamp(_hud.Position.X + _hud.Size.X + 20, 12, Math.Max(12, _root.Size.X - size.X - 12)),
            Math.Clamp((_root.Size.Y - size.Y) / 2, 12, Math.Max(12, _root.Size.Y - size.Y - 12)));
    }

    private void ReleaseCards()
    {
        foreach (var card in _cards) {
            if (!GodotObject.IsInstanceValid(card)) continue;
            card.GetParent()?.RemoveChild(card); card.QueueFreeSafely();
        }
        _cards.Clear();
    }

    private void Populate()
    {
        var deck = MagicDeckStore.Read(_player);
        string state = deck.ToJson();
        if (_displayed == state) return;
        ReleaseCards();
        foreach (var child in _view.GetChildren()) { _view.RemoveChild(child); child.QueueFree(); }
        foreach (var slot in Enum.GetValues<ManaSlot>()) {
            float x = 150 + (int)slot * 275;
            var title = new Label { Text = new LocString("frieren", "SLOT." + slot).GetFormattedText(),
                Position = new(x - 125, 15), Size = new(250, 42), HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            title.AddThemeFontSizeOverride("font_size", 23);
            title.AddThemeColorOverride("font_color", new Color("fff1d5"));
            _view.AddChild(title);
            var item = deck.InSlot(slot);
            if (item == null) {
                var empty = new Label { Text = new LocString("frieren", "EMPTY").GetFormattedText(),
                    Position = new(x - 125, 200), Size = new(250, 80), HorizontalAlignment = HorizontalAlignment.Center,
                    MouseFilter = Control.MouseFilterEnum.Ignore };
                _view.AddChild(empty); continue;
            }
            var model = FactorCard.Create(_player, item.Id, slot);
            var card = MagicBookScreen.CreateCard(model);
            card.Name = "Preview" + slot;
            card.Position = new(x, 260); card.Scale = Vector2.One * 0.72f;
            _view.AddChild(card);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            foreach (var control in card.FindChildren("*", "Control", true, false).OfType<Control>())
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            card.MouseFilter = Control.MouseFilterEnum.Ignore;
            _cards.Add(card);
        }
        _displayed = state;
    }
}

using FrierenMod.Scripts.Mechanics;
using FrierenMod.Scripts.Monsters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace FrierenMod.Scripts.UI;

// 复用一代天平原图的秤盘和吊环；横梁、悬链采用独立节点几何，保证倾斜时秤盘保持竖直。
public partial class AuraScalesHud : Control
{
    private Texture2D _art = null!;
    private Label _left = null!, _right = null!, _caption = null!;
    private float _angle;
    private float _judgmentTime;
    private float _velocity;
    private Aura? _aura;
    public float CurrentAngle => _angle;
    public static float TargetAngle(decimal playerMana, int auraMana) =>
        Math.Clamp((float)(auraMana - playerMana) / 100f, -1, 1) * 0.25f;

    public static void Judgment()
    {
        if (NCombatRoom.Instance?.GetNodeOrNull<AuraScalesHud>("AuraScalesHud") is { } hud) hud._judgmentTime = 1.6f;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        MouseBehaviorRecursive = MouseBehaviorRecursiveEnum.Disabled;
        SetAnchorsPreset(LayoutPreset.CenterTop);
        OffsetLeft = -260; OffsetRight = 260; OffsetTop = 110; OffsetBottom = 340;
        _art = GD.Load<Texture2D>("res://FrierenMod/images/ui/AuraScales.png");
        _left = MakeLabel(new Vector2(0, 30), new Vector2(120, 52), 34, new Color("b5efff"));
        _right = MakeLabel(new Vector2(400, 30), new Vector2(120, 52), 34, new Color("d9b1eb"));
        _caption = MakeLabel(new Vector2(0, 198), new Vector2(520, 30), 18, new Color("ded1b7"));
        var player = MakeLabel(new Vector2(0, 12), new Vector2(120, 25), 17, Colors.White);
        player.Text = new LocString("frieren", "AURA.PLAYER").GetFormattedText();
        var aura = MakeLabel(new Vector2(400, 12), new Vector2(120, 25), 17, Colors.White);
        aura.Text = new LocString("monsters", "FRIERENMOD-AURA.name").GetFormattedText();
        Visible = false;
    }

    private Label MakeLabel(Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = new Label { Position = position, Size = size, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0.04f, 0.03f, 0.06f, 0.95f));
        label.AddThemeConstantOverride("outline_size", 6);
        AddChild(label);
        return label;
    }

    public override void _Process(double delta)
    {
        var combat = CombatManager.Instance.DebugOnlyGetState();
        _aura = combat?.Enemies.FirstOrDefault(c => c.IsAlive && c.Monster is Aura)?.Monster as Aura;
        Visible = _aura != null && CombatManager.Instance.IsInProgress;
        if (!Visible) return;
        var players = combat!.Players.Where(p => p.Creature.IsAlive).ToArray();
        decimal mana = players.Length == 0 ? 0 : players.Average(p => (decimal)ManaCmd.Total(p));
        UpdateDisplay(mana, _aura!.Mana, _aura.UpcomingTurn, delta);
    }

    public void UpdateDisplay(decimal playerMana, int auraMana, int turn, double delta)
    {
        _left.Text = playerMana.ToString("0.#");
        _right.Text = auraMana.ToString();
        var text = new LocString("frieren", "AURA.COUNTDOWN");
        text.Add("Turn", ((turn + 2) / 3) * 3);
        _judgmentTime = Math.Max(0, _judgmentTime - (float)delta);
        _caption.Text = _judgmentTime > 0 ? new LocString("frieren", "AURA.JUDGMENT").GetFormattedText() : text.GetFormattedText();
        Modulate = _judgmentTime > 0 ? new Color(1.5f, 1.25f, 1.6f) : Colors.White;
        // 阻尼弹簧：魔力变化时轻摆，平衡后自然停住；正角使右侧下降。
        float dt = Math.Min((float)delta, 1f / 30);
        _velocity += (TargetAngle(playerMana, auraMana) - _angle) * 75 * dt;
        _velocity *= MathF.Exp(-11 * dt);
        _angle += _velocity * dt;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_art == null) return;
        var center = new Vector2(260, 48);
        var arm = new Vector2(115, 0).Rotated(_angle);
        var left = center - arm;
        var right = center + arm;
        var bronze = new Color("a99a79");
        var dark = new Color("423d39");
        DrawLine(left, right, dark, 10, true);
        DrawLine(left, right, bronze, 5, true);
        DrawCircle(center, 10, dark, true);
        DrawCircle(center, 6, bronze, false, 2, true);
        DrawTextureRectRegion(_art, new Rect2(239, 8, 42, 30), new Rect2(50, 27, 28, 20));
        DrawLine(new Vector2(260, 30), center, bronze, 3, true);
        DrawPan(left, new Rect2(20, 64, 30, 40));
        DrawPan(right, new Rect2(83, 44, 26, 38));
    }

    private void DrawPan(Vector2 anchor, Rect2 source)
    {
        var chain = new Color("908570");
        DrawCircle(anchor, 4, chain, false, 2, true);
        DrawLine(anchor, anchor + new Vector2(-23, 62), chain, 1.5f, true);
        DrawLine(anchor, anchor + new Vector2(23, 62), chain, 1.5f, true);
        DrawTextureRectRegion(_art, new Rect2(anchor + new Vector2(-30, 28), new Vector2(60, 80)), source);
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class AuraScalesHudAttachment
{
    private static void Postfix(NCombatRoom __instance) => __instance.AddChild(new AuraScalesHud { Name = "AuraScalesHud" });
}

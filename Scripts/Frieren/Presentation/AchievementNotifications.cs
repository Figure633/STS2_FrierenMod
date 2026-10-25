using System.Runtime.CompilerServices;
using BaseLib.Audio;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class AchievementNotifications
{
    private static readonly ConditionalWeakTable<Control, Session> Sessions = new();
    private static void Postfix(NGame __instance) => Attach(__instance);
    // 游戏异步初始化模组时，NGame._Ready 可能已经执行；不能只等后来才安装的补丁。
    public static void Initialize() { if (NGame.Instance is { } game) Attach(game); }
    public static Session Attach(Control root) => Sessions.GetValue(root, r => new Session(r));

    public sealed class Session
    {
        private readonly Control _root;
        private readonly CanvasLayer _layer;
        private readonly Queue<string> _pending = new();
        private PanelContainer? _popup;
        private float _arrival;
        public int PendingCount => _pending.Count + (_popup == null ? 0 : 1);

        public Session(Control root)
        {
            _root = root;
            _layer = new CanvasLayer { Name = "FrierenAchievementLayer", Layer = 100, ProcessMode = Node.ProcessModeEnum.Always };
            root.AddChild(_layer);
            SlotBackStore.Unlocked += Enqueue; root.Resized += Layout;
            root.TreeExiting += () => { SlotBackStore.Unlocked -= Enqueue; root.Resized -= Layout; _pending.Clear(); };
        }

        private void Enqueue(string id)
        {
            _pending.Enqueue(id);
            if (_popup == null) Next();
        }

        private void Layout()
        {
            if (_popup == null) return;
            Vector2 available = _root.Size;
            if (available.X <= 0 || available.Y <= 0) available = _root.GetViewportRect().Size;
            float scale = Math.Max(.1f, Math.Min(available.X / 1920, available.Y / 1080));
            _popup.Scale = Vector2.One * scale;
            _popup.Position = new((available.X - _popup.Size.X * scale) / 2,
                Mathf.Lerp(-_popup.Size.Y - 20, 82, _arrival) * scale);
        }

        private void Next()
        {
            if (_pending.Count == 0 || !_root.IsInsideTree()) return;
            string id = _pending.Dequeue();
            string title = SlotBackLibrary.Text("BACK." + id + ".title");
            // 一代提示含卡背缩略图、金色标题和解锁条件；长文本由容器自动换行撑高。
            float width = Math.Clamp(344 + Math.Max(0, title.Length - 5) * 15, 390, 660);
            var popup = new PanelContainer { Name = "FrierenAchievementPopup", CustomMinimumSize = new(width, 190),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            popup.AddThemeStyleboxOverride("panel", SlotBackLibrary.PopupStyle());
            _popup = popup; _arrival = 0; _layer.AddChild(popup);
            var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            row.AddThemeConstantOverride("separation", 20); popup.AddChild(row);
            row.MinimumSizeChanged += popup.ResetSize;
            row.AddChild(new TextureRect { Name = "Thumbnail", Texture = SlotBackLibrary.Texture(id),
                CustomMinimumSize = new(60, 92), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore });
            var text = new VBoxContainer { Name = "Text", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
            text.AddThemeConstantOverride("separation", 12); row.AddChild(text);
            void AddLabel(string name, string value, int fontSize, Color color) {
                var label = new Label { Name = name, Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    MouseFilter = Control.MouseFilterEnum.Ignore };
                label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
                label.AddThemeFontSizeOverride("font_size", fontSize); label.AddThemeColorOverride("font_color", color);
                label.AddThemeConstantOverride("outline_size", 2); label.AddThemeColorOverride("font_outline_color", Colors.Black);
                text.AddChild(label);
            }
            AddLabel("Title", title, 25, new Color("ffcc4d"));
            AddLabel("Condition", SlotBackLibrary.Text("BACK." + id + ".description"), 22, new Color("fff1d4"));
            popup.Resized += Layout; Layout();
            // 成就既会在战斗中解锁，也会在战败/通关/主菜单解锁，绕过 SfxCmd 的战斗结束屏蔽。
            ModAudio.PlaySoundGlobal("res://FrierenMod/audio/achievement.mp3");
            var tween = popup.CreateTween();
            tween.TweenMethod(Callable.From<float>(value => { _arrival = value; Layout(); }), 0f, 1f, .7)
                .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
            tween.TweenInterval(2);
            tween.TweenMethod(Callable.From<float>(value => { _arrival = value; Layout(); }), 1f, 0f, .6)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
            tween.TweenCallback(Callable.From(() => { popup.QueueFree(); _popup = null; Next(); }));
        }
    }
}

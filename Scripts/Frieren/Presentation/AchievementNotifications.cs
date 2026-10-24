using System.Runtime.CompilerServices;
using FrierenMod.Scripts.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes;

namespace FrierenMod.Scripts.Presentation;

[HarmonyPatch(typeof(NGame), nameof(NGame._Ready))]
public static class AchievementNotifications
{
    private static readonly ConditionalWeakTable<Control, Session> Sessions = new();
    private static void Postfix(NGame __instance) => Attach(__instance);
    public static Session Attach(Control root) => Sessions.GetValue(root, r => new Session(r));

    public sealed class Session
    {
        private readonly Control _root;
        private readonly Queue<string> _pending = new();
        private Control? _popup;
        public int PendingCount => _pending.Count + (_popup == null ? 0 : 1);

        public Session(Control root)
        {
            _root = root; SlotBackStore.Unlocked += Enqueue;
            root.TreeExiting += () => { SlotBackStore.Unlocked -= Enqueue; _pending.Clear(); };
        }

        private void Enqueue(string id)
        {
            _pending.Enqueue(id);
            if (_popup == null) Next();
        }

        private void Next()
        {
            if (_pending.Count == 0 || !_root.IsInsideTree()) return;
            string id = _pending.Dequeue();
            float scale = Math.Max(0.1f, Math.Min(_root.Size.X / 1920, _root.Size.Y / 1080));
            var popup = new Control { Name = "FrierenAchievementPopup", Size = new(600, 348), Scale = Vector2.One * scale,
                Position = new((_root.Size.X - 600 * scale) / 2, -350 * scale),
                MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = 250, ZAsRelative = false };
            _popup = popup; _root.AddChild(popup);
            popup.AddChild(new TextureRect { Texture = GD.Load<Texture2D>("res://FrierenMod/images/achievement/background.png"),
                Size = popup.Size, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, MouseFilter = Control.MouseFilterEnum.Ignore });
            popup.AddChild(new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Texture = SlotBackLibrary.Texture(id), Position = new(45, 76), Size = new(110, 168),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore });
            void Label(string text, int size, Vector2 pos) {
                var label = new Godot.Label { Text = text, Position = pos, Size = new(380, 75),
                    VerticalAlignment = VerticalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    MouseFilter = Control.MouseFilterEnum.Ignore };
                label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", new Color("fff1d4"));
                label.AddThemeConstantOverride("outline_size", 4); label.AddThemeColorOverride("font_outline_color", Colors.Black);
                popup.AddChild(label);
            }
            Label(SlotBackLibrary.Text("BACK_NEW"), 20, new(175, 72));
            Label(SlotBackLibrary.Text("BACK." + id + ".title"), 22, new(175, 145));
            SfxCmd.Play("res://FrierenMod/audio/achievement.mp3");
            var tween = popup.CreateTween();
            tween.TweenProperty(popup, "position:y", 10f, 0.35).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenInterval(3);
            tween.TweenProperty(popup, "position:y", -350f * scale, 0.35);
            tween.TweenCallback(Callable.From(() => { popup.QueueFree(); _popup = null; Next(); }));
        }
    }
}

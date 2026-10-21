using System.Runtime.CompilerServices;
using FrierenMod.Core;
using FrierenMod.Scripts.Cards;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace FrierenMod.Scripts.UI;

// 表现层只读取已经提交的库存差量，不支付物品、不抽取随机数，也不写入存档。
public static class MagicDeckAnimations
{
    private static readonly ConditionalWeakTable<Player, Presentation> Views = new();

    public static void Bind(Player player, Control layer, Control destination, MagicBookScreen screen)
    {
        var view = new Presentation(player, layer, destination, screen);
        Views.Remove(player);
        Views.Add(player, view);
        layer.TreeExiting += () => {
            view.Closed = true;
            if (Views.TryGetValue(player, out var current) && current == view) Views.Remove(player);
        };
    }

    public static bool IsAttached(Player player) => Views.TryGetValue(player, out _);
    public static Task WhenIdle(Player player) => Views.TryGetValue(player, out var view) ? view.Pending : Task.CompletedTask;
    public static void Changed(Player player, MagicDeckState before, MagicDeckState after, int? usedProp)
    {
        try {
            if (Views.TryGetValue(player, out var view)) view.Enqueue(before, after, usedProp);
        }
        catch (Exception error) { GD.PushWarning("魔法牌组动画未能启动：" + error); }
    }

    private sealed record Item(int Id, CardModel Model, Vector2? Position, Vector2? Scale);

    private sealed class Presentation(Player player, Control layer, Control destination, MagicBookScreen screen)
    {
        public bool Closed;
        public Task Pending { get; private set; } = Task.CompletedTask;

        private Item[] Items(MagicDeckState deck) => deck.Items.Select(i =>
                Snapshot(i.InstanceId, FactorCard.Create(player, i.SpellId)))
            .Concat(deck.Props.Select(p => Snapshot(p.InstanceId, MagicPropCard.Create(player, p.PropId)))).ToArray();

        private Item Snapshot(int id, CardModel model)
        {
            var pose = screen.AnimationPose(id);
            return new Item(id, model, pose?.Position, pose?.Scale);
        }

        public void Enqueue(MagicDeckState before, MagicDeckState after, int? usedProp)
        {
            var oldIds = before.Items.Select(i => i.InstanceId).Concat(before.Props.Select(p => p.InstanceId)).ToHashSet();
            var newIds = after.Items.Select(i => i.InstanceId).Concat(after.Props.Select(p => p.InstanceId)).ToHashSet();
            if (oldIds.SetEquals(newIds)) return; // 槽位移动与读档刷新不重复播放入库。
            var removed = Items(before).Where(i => !newIds.Contains(i.Id)).OrderBy(i => i.Id == usedProp ? 0 : 1).ToArray();
            var added = Items(after).Where(i => !oldIds.Contains(i.Id)).ToArray();
            Pending = PlayAfter(Pending, removed, added, usedProp);
        }

        private async Task PlayAfter(Task previous, Item[] removed, Item[] added, int? usedProp)
        {
            await previous;
            if (Closed) return;
            var cards = new List<NCard>();
            try {
                foreach (var item in removed) {
                    if (Closed) return;
                    var card = Show(item, 0, 1, true); cards.Add(card);
                    if (item.Id == usedProp) {
                        card.Name = "UsedMagicProp";
                        card.CardHighlight.Modulate = new Color("65ef85");
                        card.CardHighlight.AnimFlash();
                        // 单次绿光完成后，物品才淡出，接着展示新因子。
                        var flash = layer.CreateTween(); flash.TweenInterval(0.48);
                        if (!await flash.AwaitFinished(layer)) return;
                    }
                    var fade = layer.CreateTween().SetParallel();
                    fade.TweenProperty(card, "modulate:a", 0f, 0.22);
                    fade.TweenProperty(card, "scale", card.Scale * 0.65f, 0.22);
                    if (!await fade.AwaitFinished(layer)) return;
                    Release(card); cards.Remove(card);
                }
                // 多件奖励并排展示、依次飞入，避免后生成的物品遮住先生成的物品。
                for (int start = 0; start < added.Length; start += 3) {
                    int count = Math.Min(3, added.Length - start);
                    for (int i = 0; i < count; i++) cards.Add(Show(added[start + i], i, count, false));
                    var flight = layer.CreateTween().SetParallel();
                    for (int i = 0; i < cards.Count; i++) {
                        var card = cards[i]; double delay = 0.28 + i * 0.12;
                        flight.TweenProperty(card, "global_position", destination.GetGlobalRect().GetCenter(), 0.55)
                            .SetDelay(delay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                        flight.TweenProperty(card, "scale", Vector2.One * 0.07f, 0.55).SetDelay(delay);
                        flight.TweenProperty(card, "modulate:a", 0f, 0.16).SetDelay(delay + 0.39);
                    }
                    if (!await flight.AwaitFinished(layer)) return;
                    foreach (var card in cards) Release(card);
                    cards.Clear();
                }
            }
            catch (Exception error) { GD.PushWarning("魔法牌组动画未完成：" + error); }
            finally { foreach (var card in cards) Release(card); }
        }

        private NCard Show(Item item, int index, int count, bool source)
        {
            var card = MagicBookScreen.CreateCard(item.Model);
            layer.AddChild(card);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            card.Name = (source ? "ConsumedMagicItem_" : "AcquiredMagicItem_") + item.Id;
            card.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Disabled;
            card.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Disabled;
            card.Modulate = Colors.White;
            float scale = Math.Min(layer.Size.X / 1920, layer.Size.Y / 1080);
            card.GlobalPosition = source && item.Position.HasValue ? item.Position.Value :
                layer.GetGlobalRect().GetCenter() + new Vector2((index - (count - 1) / 2f) * 290 * scale, 0);
            card.Scale = (source && item.Scale.HasValue ? item.Scale.Value : Vector2.One * (0.75f * scale)) /
                layer.GetGlobalTransform().Scale;
            card.CardHighlight.AnimHideInstantly();
            card.CardHighlight.Modulate = NCardHighlight.playableColor;
            return card;
        }

        private static void Release(NCard card)
        {
            if (!GodotObject.IsInstanceValid(card)) return;
            card.CardHighlight.AnimHideInstantly();
            card.CardHighlight.Modulate = NCardHighlight.playableColor;
            card.Modulate = Colors.White;
            card.GetParent()?.RemoveChild(card);
            card.QueueFreeSafely();
        }
    }
}

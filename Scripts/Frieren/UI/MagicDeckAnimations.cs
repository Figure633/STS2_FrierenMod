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
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Commands;

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
            var exhausts = new List<Task>();
            screen.BeginInventoryAnimation();
            try {
                foreach (var item in removed) {
                    if (Closed) return;
                    screen.HideAnimatedItem(item.Id);
                    var card = Show(item, 0, 1, true); cards.Add(card);
                    if (item.Id == usedProp) {
                        card.Name = "UsedMagicProp";
                        card.CardHighlight.Modulate = new Color("65ef85");
                        card.CardHighlight.AnimFlash();
                        // 单次绿光完成后，物品才淡出，接着展示新因子。
                        var flash = layer.CreateTween(); flash.TweenInterval(0.48);
                        if (!await flash.AwaitFinished(layer)) return;
                    }
                    // 与原生 CardPileCmd 消耗牌相同：粒子与音效由原版提供，卡面变灰后释放。
                    var exhaust = NExhaustVfx.Create(card);
                    if (exhaust != null) {
                        exhaust.Scale = card.GetGlobalTransform().Scale / layer.GetGlobalTransform().Scale;
                        layer.AddChild(exhaust);
                        exhausts.Add(WaitForExit(exhaust));
                    }
                    var fade = layer.CreateTween();
                    fade.TweenProperty(card, "modulate", StsColors.exhaustGray, 0.3);
                    if (!await fade.AwaitFinished(layer)) return;
                    Release(card); cards.Remove(card);
                }
                // 魔法书已经打开时，直接展示库存中的新条目，不再从书内飞回右上角图标。
                if (screen.View.IsVisibleInTree()) {
                    screen.RevealInventoryChanges();
                    // GridContainer 在下一帧完成排版后，才有正确的新条目滚动位置。
                    await layer.ToSignal(layer.GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                if (Closed) return;
                var external = new List<Item>();
                foreach (var item in added) {
                    if (Closed) return;
                    if (!screen.FocusAcquiredItem(item.Id)) { external.Add(item); continue; }
                    await layer.ToSignal(layer.GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (Closed) return;
                    screen.FlashAcquiredItem(item.Id);
                    var reveal = layer.CreateTween(); reveal.TweenInterval(1.2);
                    if (!await reveal.AwaitFinished(layer)) return;
                }
                // 外部奖励仍使用原版飞入；多件并排展示，依次起飞。
                for (int start = 0; start < external.Count; start += 3) {
                    int count = Math.Min(3, external.Count - start);
                    for (int i = 0; i < count; i++) {
                        var card = Show(external[start + i], i, count, false);
                        cards.Add(card);
                        // 一代盲盒会令新因子 superFlash；复用原版卡牌闪光，留出完整辨认时间。
                        if (usedProp.HasValue) card.CardHighlight.AnimFlash();
                    }
                    var flights = new List<Task>();
                    for (int i = 0; i < cards.Count; i++) {
                        flights.Add(Fly(cards[i], (usedProp.HasValue ? 1.2 : 0.28) + i * 0.12));
                    }
                    await Task.WhenAll(flights);
                    // 原生飞入特效自行释放卡牌；不能再次归还对象池。
                    cards.Clear();
                }
                await Task.WhenAll(exhausts);
            }
            catch (Exception error) { GD.PushWarning("魔法牌组动画未完成：" + error); }
            finally {
                foreach (var card in cards) Release(card);
                screen.EndInventoryAnimation();
            }
        }

        private static async Task WaitForExit(Node effect) => await effect.ToSignal(effect, Node.SignalName.TreeExiting);

        private async Task Fly(NCard card, double delay)
        {
            var pause = layer.CreateTween(); pause.TweenInterval(delay);
            if (!await pause.AwaitFinished(layer)) return;
            // 原生工厂只接受普通牌堆；同一原生场景改填魔法书终点，其余动画、拖尾与回收完全由原版执行。
            var effect = GD.Load<PackedScene>(NCardFlyVfx.AssetPaths.Single()).Instantiate<NCardFlyVfx>();
            effect.Set(NCardFlyVfx.PropertyName._card, card);
            effect.Set(NCardFlyVfx.PropertyName._startPos, card.GlobalPosition);
            effect.Set(NCardFlyVfx.PropertyName._endPos, destination.GetGlobalRect().GetCenter());
            effect.Set(NCardFlyVfx.PropertyName._isAddingToPile, false);
            effect.Set(NCardFlyVfx.PropertyName._trailPath, player.Character.TrailPath);
            layer.AddChild(effect);
            SfxCmd.Play("event:/sfx/ui/cards/card_movement_B_into_deck");
            var exited = new TaskCompletionSource();
            void OnExit() => exited.TrySetResult();
            layer.TreeExiting += OnExit;
            try { await Task.WhenAny(effect.SwooshAwayCompletion!.Task, exited.Task); }
            finally { if (GodotObject.IsInstanceValid(layer)) layer.TreeExiting -= OnExit; }
        }

        private NCard Show(Item item, int index, int count, bool source)
        {
            var card = MagicBookScreen.CreateCard(item.Model);
            layer.AddChild(card);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            card.Name = (source ? "ConsumedMagicItem_" : "AcquiredMagicItem_") + item.Id;
            // 输入屏蔽由专用父层承担，不能污染会被手牌和魔法书重复使用的 NCard 对象池。
            card.MouseBehaviorRecursive = Control.MouseBehaviorRecursiveEnum.Inherited;
            card.FocusBehaviorRecursive = Control.FocusBehaviorRecursiveEnum.Inherited;
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

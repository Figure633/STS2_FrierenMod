using FrierenMod.Scripts.Cards;
using FrierenMod.Scripts.Characters;
using FrierenMod.Scripts.Presentation;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Unlocks;

public static class PreviewAndFlowTests
{
    public static async Task<int> Run(Node root)
    {
        int count = 0;
        void Check(bool value, string name) {
            if (!value) throw new Exception(name);
            count++; GD.Print("PASS 轮播与流转：" + name);
        }
        var pool = ModelDb.CardPool<FrierenCardPool>();
        Check(pool.AllCards.OfType<Flow>().Count() == 1, "流转保留注册和百科模型");
        Check(ModelDb.Character<Frieren>().StartingDeck.OfType<Flow>().Count() == 1, "初始牌组保留流转");
        foreach (var constraint in new[] { CardMultiplayerConstraint.None, CardMultiplayerConstraint.SingleplayerOnly, CardMultiplayerConstraint.MultiplayerOnly })
            Check(!pool.GetUnlockedCards(UnlockState.all, constraint).OfType<Flow>().Any(), "普通卡池排除流转：" + constraint);
        Check(pool.GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).OfType<ElementsBombing>().Any(), "其他普通卡仍可获取");

        foreach (bool upgraded in new[] { false, true }) {
            var source = ModelDb.Card<ElementsBombing>().ToMutable();
            if (upgraded) { source.UpgradeInternal(); source.FinalizeUpgradeInternal(); }
            var previews = DerivedCardPreviews.For(source).OfType<CardHoverTip>().ToArray();
            Check(previews.Length == 1 && previews[0] is CyclingCardHoverTip, "只占一个原生预览位置");
            var tip = (CyclingCardHoverTip)previews[0];
            Type[] expected = [typeof(Ice), typeof(Fire), typeof(Thunder), typeof(Dark), typeof(Light)];
            Check(tip.Cards.Select(c => c.GetType()).SequenceEqual(expected), "轮播顺序与一代一致");
            Check(tip.Cards.All(c => c.IsUpgraded == upgraded), "预览继承升级状态");
            var container = new NHoverTipCardContainer(); root.AddChild(container);
            container.Add(tip);
            var holder = container.GetChildren().OfType<Control>().Single();
            var node = holder.FindChildren("*", recursive: true, owned: false).OfType<NCard>().Single();
            var timer = holder.GetChildren().OfType<Godot.Timer>().Single();
            Check(node.Model is Ice && !timer.IsStopped(), "原生卡面首张为冰且轮播计时器已启动");
            if (!upgraded) {
                await root.ToSignal(root.GetTree().CreateTimer(2.1), SceneTreeTimer.SignalName.Timeout);
                Check(node.Model is Fire, "悬停两秒后自动切换下一张");
            } else timer.EmitSignal(Godot.Timer.SignalName.Timeout);
            for (int i = 2; i <= expected.Length; i++) {
                timer.EmitSignal(Godot.Timer.SignalName.Timeout);
                Check(node.Model!.GetType() == expected[i % expected.Length] && node.Model.IsUpgraded == upgraded, "依次切换并循环回首张：" + i);
            }
            var timerRef = GodotObject.WeakRef(timer);
            root.RemoveChild(container); container.QueueFree();
            await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            await root.ToSignal(root.GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(timerRef!.GetRef().AsGodotObject() == null, "关闭浮窗后计时器随节点释放");
            timerRef.Dispose();
        }
        var single = DerivedCardPreviews.For(ModelDb.Card<FireSpell>()).OfType<CardHoverTip>().Single();
        Check(single is not CyclingCardHoverTip && single.Card is LittleFire, "单张衍生牌沿用静态预览");
        return count;
    }
}

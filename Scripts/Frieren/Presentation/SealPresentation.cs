using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace FrierenMod.Scripts.Presentation;

public static class SealPresentation
{
    // 一代SealEffect：卡面旋正、向人物收拢、缩至0.001；仅表现，不参与牌堆或同步结算。
    public static void Play(CardModel card, Player player)
    {
        if (MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn || NCombatRoom.Instance is not {} room) return;
        var visual = NCard.Create(card);
        if (visual == null) return;
        room.Ui.AddChild(visual);
        visual.UpdateVisuals(PileType.Draw, CardPreviewMode.Normal);
        visual.Position = PileType.Draw.GetTargetPosition(visual);
        var creature = player.Creature.GetCreatureNode();
        var destination = creature == null ? new Vector2(300, 650)
            : room.Ui.GetGlobalTransform().AffineInverse() * creature.GlobalPosition;
        var tween = visual.CreateTween().SetParallel();
        tween.TweenProperty(visual, "position", destination, .3).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(visual, "scale", Vector2.One * .001f, .3);
        tween.TweenProperty(visual, "rotation", 0f, .3);
        tween.Finished += visual.QueueFree;
    }
}

using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace FrierenMod.Scripts.UI;

// 使用原生排版和屏幕边界修正，仅把定位锚点放到鼠标旁。
internal static class CommissionHoverTips
{
    private sealed class Presentation(Control host)
    {
        public readonly Control Anchor = new() { Name="CommissionTipAnchor",Size=Vector2.One,MouseFilter=Control.MouseFilterEnum.Ignore };
        public NHoverTipSet? Tips;
        public void Position() {
            if (!GodotObject.IsInstanceValid(Tips)) return;
            Anchor.GlobalPosition=host.GetGlobalMousePosition()+new Vector2(18,18);
            Tips!.SetAlignment(Anchor,Anchor.GlobalPosition.X<host.GetViewportRect().Size.X/2?HoverTipAlignment.Right:HoverTipAlignment.Left);
        }
    }
    private static readonly ConditionalWeakTable<Control,Presentation> Presentations=new();
    private static Control? _active;
    public static void Show(Control host,IEnumerable<IHoverTip> tips) {
        if(NGame.Instance==null)return;
        if(_active!=null)Remove(_active);
        var presentation=Presentations.GetValue(host,h=>{
            var p=new Presentation(h);h.AddChild(p.Anchor);
            var timer=new Godot.Timer{WaitTime=.016,Autostart=true};timer.Timeout+=p.Position;h.AddChild(timer);
            h.TreeExiting+=()=>Remove(h);
            return p;
        });
        presentation.Anchor.GlobalPosition=host.GetGlobalMousePosition()+new Vector2(18,18);
        presentation.Tips=NHoverTipSet.CreateAndShow(presentation.Anchor,tips,HoverTipAlignment.Right);
        presentation.Position();_active=host;
    }
    public static void Remove(Control host) {
        if(Presentations.TryGetValue(host,out var p)){NHoverTipSet.Remove(p.Anchor);p.Tips=null;}
        if(_active==host)_active=null;
    }
}

using Godot;

namespace FrierenMod.Scripts.UI;

public partial class RecordDisc : Control
{
    private float _angle;
    public override void _Process(double delta) { _angle += (float)delta*1.8f; QueueRedraw(); }
    public override void _Draw()
    {
        var center=Size/2; float r=Math.Min(Size.X,Size.Y)/2;
        DrawCircle(center,r,new Color("171820"));
        DrawArc(center,r*.8f,0,Mathf.Tau,48,new Color("515158"),.7f,true);
        DrawArc(center,r*.62f,0,Mathf.Tau,48,new Color("3b3b43"),.7f,true);
        DrawArc(center,r*.76f,_angle,_angle+.6f,12,new Color("b0a6a0"),1.5f,true);
        DrawArc(center,r*.76f,_angle+Mathf.Pi,_angle+Mathf.Pi+.6f,12,new Color("756f73"),1.5f,true);
        DrawCircle(center,r*.33f,new Color("a73d3c"));
        DrawCircle(center,1,new Color("dbc8a3"));
    }
}

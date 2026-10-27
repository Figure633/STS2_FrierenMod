using System.Runtime.CompilerServices;
using FrierenMod.Scripts.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.UI;

// 沿原稿轮廓填补封闭区域，遮住路线；保留角与头发之间的空隙，不生成凸包外框。
[HarmonyPatch(typeof(NBossMapPoint),nameof(NBossMapPoint._Ready))]
public static class CustomBossMapMask
{
    private static readonly ConditionalWeakTable<Texture2D,ImageTexture> Masks = new();
    public static ImageTexture For(Texture2D texture) => Masks.GetValue(texture,Create);
    private static ImageTexture Create(Texture2D texture)
    {
        using var image=texture.GetImage();if(image.IsCompressed())image.Decompress();
        int width=image.GetWidth(),height=image.GetHeight();
        var outside=new bool[width*height];var queue=new Queue<Vector2I>();
        void Visit(int x,int y) {
            if(x<0 || y<0 || x>=width || y>=height || outside[y*width+x] || image.GetPixel(x,y).A>.1f)return;
            outside[y*width+x]=true;queue.Enqueue(new Vector2I(x,y));
        }
        for(int x=0;x<width;x++){Visit(x,0);Visit(x,height-1);}
        for(int y=0;y<height;y++){Visit(0,y);Visit(width-1,y);}
        while(queue.TryDequeue(out var p)){Visit(p.X-1,p.Y);Visit(p.X+1,p.Y);Visit(p.X,p.Y-1);Visit(p.X,p.Y+1);}
        using var mask=Image.CreateEmpty(width,height,false,Image.Format.Rgba8);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            mask.SetPixel(x,y,new Color(1,1,1,outside[y*width+x]?image.GetPixel(x,y).A:1));
        return ImageTexture.CreateFromImage(mask);
    }
    private static void Postfix(NBossMapPoint __instance)
    {
        var outline=__instance.GetNodeOrNull<TextureRect>("%PlaceholderOutline");
        if(outline?.Texture is not {} texture || !texture.ResourcePath.Contains("res://FrierenMod/images/monsters/"))return;
        outline.Texture=For(texture);
        foreach(var view in new[]{outline,__instance.GetNode<TextureRect>("%PlaceholderImage")}) {
            view.ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize;
            view.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
            view.TextureFilter=CanvasItem.TextureFilterEnum.Linear;
        }
    }
}

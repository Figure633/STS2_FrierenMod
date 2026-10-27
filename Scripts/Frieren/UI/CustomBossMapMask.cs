using System.Runtime.CompilerServices;
using FrierenMod.Scripts.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace FrierenMod.Scripts.UI;

// 原生地图使用背景色的白色轮廓遮住路线；稀疏线稿需要实心轮廓，不能仍用透明线条。
[HarmonyPatch(typeof(NBossMapPoint),nameof(NBossMapPoint._Ready))]
public static class CustomBossMapMask
{
    private static readonly ConditionalWeakTable<Texture2D,ImageTexture> Masks = new();
    public static ImageTexture For(Texture2D texture) => Masks.GetValue(texture,Create);
    private static ImageTexture Create(Texture2D texture)
    {
        using var image=texture.GetImage();if(image.IsCompressed())image.Decompress();
        image.Resize(128,128,Image.Interpolation.Lanczos);
        var points=new List<Vector2>();
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
            if(image.GetPixel(x,y).A>.1f)points.Add(new Vector2(x,y));
        var hull=Geometry2D.ConvexHull(points.ToArray());
        using var mask=Image.CreateEmpty(128,128,false,Image.Format.Rgba8);
        if(hull.Length>=3)
            for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                if(Geometry2D.IsPointInPolygon(new Vector2(x,y),hull))mask.SetPixel(x,y,Colors.White);
        return ImageTexture.CreateFromImage(mask);
    }
    private static void Postfix(NBossMapPoint __instance)
    {
        var outline=__instance.GetNodeOrNull<TextureRect>("%PlaceholderOutline");
        if(outline?.Texture is not {} texture || !texture.ResourcePath.Contains("res://FrierenMod/images/monsters/"))return;
        outline.Texture=For(texture);
    }
}

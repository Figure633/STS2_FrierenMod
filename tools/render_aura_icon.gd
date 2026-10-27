extends SceneTree
# 基于已验看的完整头部原稿，输出单色512像素地图图标与原生描边。
func _initialize():
    var source = Image.load_from_file("res://FrierenMod/images/monsters/Aura_node_source.png")
    var bounds = source.get_used_rect()
    var head = source.get_region(bounds)
    var fit = 464.0 / max(head.get_width(), head.get_height())
    head.resize(roundi(head.get_width()*fit), roundi(head.get_height()*fit), Image.INTERPOLATE_LANCZOS)
    var result = Image.create(512,512,false,Image.FORMAT_RGBA8)
    result.blit_rect(head, Rect2i(Vector2i.ZERO,head.get_size()),(Vector2i(512,512)-head.get_size())/2)
    var outline = Image.create(512,512,false,Image.FORMAT_RGBA8)
    var expanded = BitMap.new()
    expanded.create_from_image_alpha(result, 0.01)
    expanded.grow_mask(8, Rect2i(0,0,512,512))
    for y in range(512):
        for x in range(512):
            var alpha = result.get_pixel(x,y).a
            result.set_pixel(x,y,Color(0.306,0.314,0.322,alpha))
            var edge = 1.0 if expanded.get_bit(x,y) else 0.0
            outline.set_pixel(x,y,Color(1,1,1,edge))
    result.save_png("res://FrierenMod/images/monsters/Aura_node.png")
    outline.save_png("res://FrierenMod/images/monsters/Aura_node_outline.png")
    quit()

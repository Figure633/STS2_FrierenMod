extends SceneTree
# 基于已验看的完整头部原稿，输出单色128像素地图图标与原生描边。
func _initialize():
    var source = Image.load_from_file("res://FrierenMod/images/monsters/Aura_node_source.png")
    var bounds = source.get_used_rect()
    var head = source.get_region(bounds)
    var fit = 116.0 / max(head.get_width(), head.get_height())
    head.resize(roundi(head.get_width()*fit), roundi(head.get_height()*fit), Image.INTERPOLATE_LANCZOS)
    var result = Image.create(128,128,false,Image.FORMAT_RGBA8)
    result.blit_rect(head, Rect2i(Vector2i.ZERO,head.get_size()),(Vector2i(128,128)-head.get_size())/2)
    var outline = Image.create(128,128,false,Image.FORMAT_RGBA8)
    for y in range(128):
        for x in range(128):
            var alpha = result.get_pixel(x,y).a
            result.set_pixel(x,y,Color(0.306,0.314,0.322,alpha))
            var edge = alpha
            for dy in range(-2,3):
                for dx in range(-2,3):
                    if x+dx>=0 and x+dx<128 and y+dy>=0 and y+dy<128:
                        edge=max(edge,result.get_pixel(x+dx,y+dy).a)
            outline.set_pixel(x,y,Color(1,1,1,edge))
    result.save_png("res://FrierenMod/images/monsters/Aura_node.png")
    outline.save_png("res://FrierenMod/images/monsters/Aura_node_outline.png")
    var mirror = Image.load_from_file("res://FrierenMod/images/monsters/Spiegel_node.png").get_region(Rect2i(490,430,490,650))
    mirror.resize(84,112,Image.INTERPOLATE_LANCZOS)
    var small = Image.create(128,128,false,Image.FORMAT_RGBA8)
    small.blit_rect(mirror,Rect2i(Vector2i.ZERO,mirror.get_size()),Vector2i(22,8))
    small.save_png("res://FrierenMod/images/monsters/Spiegel_topbar.png")
    for y in range(128):
        for x in range(128):
            var c=small.get_pixel(x,y)
            small.set_pixel(x,y,Color(1,1,1,c.a))
    small.save_png("res://FrierenMod/images/monsters/Spiegel_topbar_outline.png")
    quit()

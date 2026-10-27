extends SceneTree

# 将作者认可的线稿接入地图、顶栏和历史图标；不改变原稿。
func _initialize():
    var source = Image.load_from_file("res://FrierenMod/images/monsters/Spiegel_node_candidate.png")
    var head = source.get_region(source.get_used_rect())
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
    for prefix in ["Spiegel_node", "Spiegel_topbar"]:
        result.save_png("res://FrierenMod/images/monsters/"+prefix+".png")
        outline.save_png("res://FrierenMod/images/monsters/"+prefix+"_outline.png")
    quit()

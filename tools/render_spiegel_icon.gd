extends SceneTree

# 将作者认可的线稿接入地图、顶栏和历史图标；不改变原稿。
func _initialize():
    var source = Image.load_from_file("res://FrierenMod/images/monsters/Spiegel_node_candidate.png")
    var head = source.get_region(source.get_used_rect())
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
    for prefix in ["Spiegel_node", "Spiegel_topbar"]:
        result.save_png("res://FrierenMod/images/monsters/"+prefix+".png")
        outline.save_png("res://FrierenMod/images/monsters/"+prefix+"_outline.png")
    quit()

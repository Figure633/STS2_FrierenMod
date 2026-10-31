extends SceneTree

# 原稿由image_gen生成；这里只做游戏资源所需的等比缩放、居中和透明留白。
func _initialize():
    for name in ["PaperPlanePower", "RecyclePower", "SwampManaNextTurnPower"]:
        var source = Image.load_from_file("res://assets/power_sources/"+name+".png")
        var bounds = source.get_used_rect()
        if bounds.size.x == 0 or bounds.size.y == 0:
            push_error("能力图标原稿为空："+name)
            quit(1)
            return
        var icon = source.get_region(bounds)
        var factor = 76.0 / max(icon.get_width(), icon.get_height())
        icon.resize(roundi(icon.get_width()*factor), roundi(icon.get_height()*factor), Image.INTERPOLATE_LANCZOS)
        var result = Image.create(84,84,false,Image.FORMAT_RGBA8)
        result.blit_rect(icon,Rect2i(Vector2i.ZERO,icon.get_size()),(Vector2i(84,84)-icon.get_size())/2)
        var error = result.save_png("res://FrierenMod/images/powers/"+name+".png")
        if error != OK:
            push_error("无法保存能力图标："+name)
            quit(1)
            return
    print("POWER_ICON_IMPORT_OK: 3 transparent 84x84 icons")
    quit()

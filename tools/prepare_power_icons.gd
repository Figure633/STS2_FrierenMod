extends SceneTree

# 二代 BaseLib：战斗/悬停小图64×64，爆闪大图256×256。
# 原稿置于运行资源之外；不拉伸、不重绘，只统一透明留白和占比。
func _initialize():
    var manifest = JSON.parse_string(FileAccess.get_file_as_string("res://tools/power_icon_sources.json"))
    DirAccess.make_dir_recursive_absolute("res://FrierenMod/images/powers/big")
    for entry in manifest.icons:
        var source = Image.load_from_file("res://" + entry.source)
        if source == null or source.is_empty():
            push_error("无法读取图标原稿：" + entry.source)
            quit(1)
            return
        source.convert(Image.FORMAT_RGBA8)
        var bounds = source.get_used_rect()
        if bounds.size.x == 0 or bounds.size.y == 0:
            push_error("图标原稿为空：" + entry.source)
            quit(1)
            return
        for size in [64, 256]:
            var icon = source.get_region(bounds)
            var factor = float(size) * 0.9375 / max(icon.get_width(), icon.get_height())
            icon.resize(maxi(1, roundi(icon.get_width()*factor)), maxi(1, roundi(icon.get_height()*factor)), Image.INTERPOLATE_LANCZOS)
            var canvas = Image.create(size,size,false,Image.FORMAT_RGBA8)
            canvas.blit_rect(icon,Rect2i(Vector2i.ZERO,icon.get_size()),(Vector2i(size,size)-icon.get_size())/2)
            var folder = "big/" if size == 256 else ""
            if canvas.save_png("res://FrierenMod/images/powers/"+folder+entry.name+".png") != OK:
                push_error("无法保存图标："+entry.name)
                quit(1)
                return
    print("POWER_ICON_PREPARE_OK: ",manifest.icons.size()," icons, small64 / big256, preserved aspect and alpha")
    quit()

extends SceneTree

# 复用 StunIntent 的原版图集像素，只还原图集裁边；不重绘击晕图案。
func _initialize():
    var args = OS.get_cmdline_user_args()
    if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], false):
        push_error("请在--后传入游戏PCK路径")
        quit(1)
        return
    var sheet = JSON.parse_string(FileAccess.get_file_as_string("res://images/atlases/intent_atlas.tpsheet"))
    for page in sheet.textures:
        for sprite in page.sprites:
            if sprite.filename != "intent_stun.png": continue
            var atlas = load("res://images/atlases/"+page.image).get_image()
            if atlas.is_compressed(): atlas.decompress()
            var r = sprite.region
            var m = sprite.margin
            var result = Image.create(int(r.w+m.w),int(r.h+m.h),false,Image.FORMAT_RGBA8)
            result.blit_rect(atlas,Rect2i(int(r.x),int(r.y),int(r.w),int(r.h)),Vector2i(int(m.x),int(m.y)))
            if result.save_png("res://assets/power_sources/native_stun.png") == OK:
                print("STUN_EXTRACT_OK: ",result.get_size())
                quit()
                return
    push_error("未找到原版击晕图标")
    quit(1)

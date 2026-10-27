extends SceneTree

# 单色矢量源保留可编辑；游戏原生地图接口使用同名 PNG。
func _initialize():
	var svg = FileAccess.get_file_as_string("res://FrierenMod/images/monsters/Aura_node.svg")
	for outline in [false, true]:
		var image = Image.new()
		var source = svg.replace("#4e5052", "#ffffff").replace('stroke-width="4.2"', 'stroke-width="8"') if outline else svg
		if image.load_svg_from_string(source, 2.0) != OK:
			quit(1)
			return
		image.resize(128, 128, Image.INTERPOLATE_LANCZOS)
		image.save_png("res://FrierenMod/images/monsters/Aura_node%s.png" % ("_outline" if outline else ""))
	quit(0)

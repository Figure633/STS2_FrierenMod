extends SceneTree

# 只打包模组资源和 Godot 导入产物，排除游戏 DLL、源码、测试和本机配置。
func _initialize():
	var packer = PCKPacker.new()
	var error = packer.pck_start("res://artifacts/FrierenMod/FrierenMod.pck", 32)
	if error != OK:
		push_error("Cannot create PCK: " + str(error))
		quit(1)
		return
	var files: Array[String] = []
	collect("res://FrierenMod", files)
	var imported: Dictionary = {}
	for path in files:
		if path.ends_with(".import"):
			var config = ConfigFile.new()
			if config.load(path) != OK:
				quit(1)
				return
			for dest in config.get_value("deps", "dest_files", []):
				imported[dest] = true
	for path in imported.keys():
		files.append(path)
	files.sort()
	for path in files:
		if packer.add_file(path, path) != OK:
			push_error("Cannot pack: " + path)
			quit(1)
			return
	if packer.flush() != OK:
		quit(1)
		return
	print("PACK_OK files=", files.size())
	quit(0)

func collect(path: String, files: Array[String]):
	var dir = DirAccess.open(path)
	for file in dir.get_files():
		files.append(path.path_join(file))
	for folder in dir.get_directories():
		collect(path.path_join(folder), files)

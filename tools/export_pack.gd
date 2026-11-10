extends SceneTree

const IMPORTED_MEDIA_TYPES = ["CompressedTexture2D", "AudioStreamOggVorbis", "AudioStreamMP3", "AudioStreamWAV"]

# 只打包模组资源、Godot 节点脚本和导入产物，排除游戏 DLL、其他源码、测试和本机配置。
func _initialize():
	var packer = PCKPacker.new()
	var error = packer.pck_start("res://artifacts/FrierenMod/FrierenMod.pck", 32)
	if error != OK:
		push_error("Cannot create PCK: " + str(error))
		quit(1)
		return
	var files: Array[String] = []
	collect("res://FrierenMod", files)
	# C# 节点由 DLL 实现；注册后的脚本路径仍需存在于 PCK 中。
	files.append("res://Scripts/Frieren/UI/MagicFactorButton.cs")
	files.append("res://Scripts/Frieren/UI/MagicBookPointerInput.cs")
	files.append("res://Scripts/Frieren/UI/MagicBookView.cs")
	files.append("res://Scripts/Frieren/UI/LegacyAchievementTip.cs")
	files.append("res://Scripts/Frieren/UI/FrierenTutorial.cs")
	files.append("res://Scripts/Frieren/UI/AuraScalesHud.cs")
	files.append("res://Scripts/Frieren/UI/NowPlayingNotice.cs")
	files.append("res://Scripts/Frieren/UI/RecordDisc.cs")
	for script in ["FrierenDivinityEyeSpawner", "FrierenDivinityActivationBurst", "FrierenAuraBlobEmitter", "DivinityPresentation", "FrierenScreenFlashEffect"]:
		files.append("res://Scripts/Frieren/Presentation/Divinity/" + script + ".cs")
	var imported: Dictionary = {}
	var omitted_sources: Dictionary = {}
	for path in files:
		if path.ends_with(".import"):
			var config = ConfigFile.new()
			if config.load(path) != OK:
				quit(1)
				return
			var destinations = config.get_value("deps", "dest_files", [])
			for dest in destinations:
				if not FileAccess.file_exists(dest):
					push_error("Missing imported resource: " + dest)
					quit(1)
					return
				imported[dest] = true
			# 沿用 Godot 的 .import 路径重映射；运行包只保留原有导入产物，不重新压缩。
			# 原文件留在工程。只排除确认有完整导入结果的纹理和音频，JSON 等直接读取文件仍保留。
			var source = config.get_value("deps", "source_file", "")
			if destinations.size() > 0 and source == path.trim_suffix(".import") \
					and config.get_value("remap", "type", "") in IMPORTED_MEDIA_TYPES:
				omitted_sources[source] = true
	var omitted_bytes: int = 0
	for source in omitted_sources:
		var original = FileAccess.open(source, FileAccess.READ)
		omitted_bytes += original.get_length()
	files = files.filter(func(path: String): return not omitted_sources.has(path))
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
	print("PACK_OK files=", files.size(), " omitted_media=", omitted_sources.size(), " saved_source_bytes=", omitted_bytes)
	quit(0)

func collect(path: String, files: Array[String]):
	# 成就悬停复用二代自带字体；一代原字体保留为资料，不重复分发。
	if path == "res://FrierenMod/fonts":
		return
	var dir = DirAccess.open(path)
	for file in dir.get_files():
		files.append(path.path_join(file))
	for folder in dir.get_directories():
		collect(path.path_join(folder), files)

extends SceneTree

func _initialize():
	var args = OS.get_cmdline_user_args()
	if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0], true):
		push_error("Cannot mount PCK")
		quit(1)
		return
	if not FileAccess.file_exists("res://Scripts/Frieren/UI/MagicFactorButton.cs"):
		push_error("Missing top bar button script")
		quit(1)
		return
	var textures = 0
	for folder in ["cards", "powers", "relics", "character", "energy", "ui"]:
		var path = "res://FrierenMod/images/" + folder
		for file in DirAccess.get_files_at(path):
			if not file.ends_with(".png"):
				continue
			var texture = load(path + "/" + file)
			if not texture is Texture2D or texture.get_width() <= 0:
				push_error("Invalid texture: " + file)
				quit(1)
				return
			textures += 1
	for lang in ["eng", "zhs"]:
		for table in ["cards", "characters", "relics", "frieren", "powers", "settings_ui", "rest_site_ui", "ancients"]:
			var text = FileAccess.get_file_as_string("res://FrierenMod/localization/" + lang + "/" + table + ".json")
			if not JSON.parse_string(text) is Dictionary:
				push_error("Invalid localization: " + lang + "/" + table)
				quit(1)
				return
	var audio_count = 0
	for file in DirAccess.get_files_at("res://FrierenMod/audio"):
		if not file.ends_with(".ogg") and not file.ends_with(".mp3"):
			continue
		var stream = load("res://FrierenMod/audio/" + file)
		if not stream is AudioStream or stream.get_length() <= 0:
			push_error("Invalid audio: " + file)
			quit(1)
			return
		audio_count += 1
	var scenes = 0
	for file in DirAccess.get_files_at("res://FrierenMod/scenes"):
		if not file.ends_with(".tscn"):
			continue
		var packed = load("res://FrierenMod/scenes/" + file)
		if not packed is PackedScene or not packed.can_instantiate():
			push_error("Invalid scene: " + file)
			quit(1)
			return
		var scene = packed.instantiate()
		scene.free()
		scenes += 1
	if textures < 119 or audio_count != 16 or scenes != 6:
		push_error("Missing texture files")
		quit(1)
		return
	print("PACK_CHECK_OK: ", textures, " textures + ", audio_count, " audio + ", scenes, " scenes + 16 localization tables")
	quit(0)

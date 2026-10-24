extends SceneTree

func _initialize():
	var args = OS.get_cmdline_user_args()
	# 场景直接引用原版能量纹理，检查时先挂载游戏资源，再挂载模组。
	if args.size() == 2 and not ProjectSettings.load_resource_pack(args[1], true):
		push_error("Cannot mount game PCK")
		quit(1)
		return
	if args.size() < 1 or args.size() > 2 or not ProjectSettings.load_resource_pack(args[0], true):
		push_error("Cannot mount PCK")
		quit(1)
		return
	for script in ["MagicFactorButton", "MagicBookPointerInput", "MagicBookView"]:
		if not FileAccess.file_exists("res://Scripts/Frieren/UI/" + script + ".cs"):
			push_error("Missing UI script: " + script)
			quit(1)
			return
	var textures = 0
	var slot_backs = 0
	for folder in ["cards", "powers", "relics", "potions", "character", "energy", "ui", "card_frames", "events", "monsters", "vfx", "slot_backs", "achievement"]:
		var path = "res://FrierenMod/images/" + folder
		for file in DirAccess.get_files_at(path):
			if not file.ends_with(".png") and not file.ends_with(".tres"):
				continue
			var texture = load(path + "/" + file)
			if not texture is Texture2D or texture.get_width() <= 0:
				push_error("Invalid texture: " + file)
				quit(1)
				return
			textures += 1
			if folder == "slot_backs":
				slot_backs += 1
	for lang in ["eng", "zhs"]:
		for table in ["cards", "characters", "relics", "potions", "frieren", "powers", "settings_ui", "rest_site_ui", "ancients", "events", "monsters", "encounters"]:
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
		# 非 Mono 打包器无法创建原版 C# 教程节点；真实继承场景由引擎专项验证。
		if file == "divinity_aura.tscn":
			for script in ["FrierenDivinityEyeSpawner", "FrierenDivinityActivationBurst", "FrierenAuraBlobEmitter", "DivinityPresentation", "FrierenScreenFlashEffect"]:
				if not FileAccess.file_exists("res://Scripts/Frieren/Presentation/Divinity/" + script + ".cs"):
					quit(1)
					return
			scenes += 1
			continue
		if file == "frieren_tutorial.tscn":
			if not FileAccess.file_exists("res://Scripts/Frieren/UI/FrierenTutorial.cs"):
				quit(1)
				return
			scenes += 1
			continue
		var packed = load("res://FrierenMod/scenes/" + file)
		if not packed is PackedScene or not packed.can_instantiate():
			push_error("Invalid scene: " + file)
			quit(1)
			return
		var scene = packed.instantiate()
		scene.free()
		scenes += 1
	if textures < 119 or slot_backs != 104 or audio_count != 20 or scenes != 11:
		push_error("Resource count mismatch: textures=%d slot_backs=%d audio=%d scenes=%d" % [textures, slot_backs, audio_count, scenes])
		quit(1)
		return
	print("PACK_CHECK_OK: ", textures, " textures + ", audio_count, " audio + ", scenes, " scenes + 24 localization tables")
	quit(0)

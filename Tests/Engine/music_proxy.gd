extends Node

var stopped = 0
var started = 0

func stop_music():
	stopped += 1

func update_music(_track):
	started += 1

func update_global_parameter(_key, _value):
	pass

func update_ambience(_track):
	pass

func stop_ambience():
	pass

func load_act_banks(_paths):
	pass

func unload_act_banks():
	pass

func update_campfire_ambience(_value):
	pass

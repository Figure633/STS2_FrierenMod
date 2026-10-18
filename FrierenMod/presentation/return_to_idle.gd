extends AnimationPlayer

func _ready():
	animation_finished.connect(_on_animation_finished)

func _on_animation_finished(animation: StringName):
	if animation != &"Dead":
		play(&"Idle")

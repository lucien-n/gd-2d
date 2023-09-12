extends Node2D

var dev: bool = true

func _ready():
	if dev:
		get_window().content_scale_factor = .5
		$interface.scale = Vector2(2, 2)

extends CanvasLayer

var player: CharacterBody2D

@onready var label_fps: Label = $Control/fps

func _ready():
	player = get_parent().get_child(0)

func _process(delta):
	label_fps.text = "FPS: " + str(Engine.get_frames_per_second())

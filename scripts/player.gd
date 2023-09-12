extends CharacterBody2D

var speed: int = 5_000
var friction: float = 0.0
var acceleration: int = 800

var current_chunk: Vector2 = Vector2(0, 0)

func _physics_process(delta):
	velocity = Vector2.ZERO
	
	if Input.is_action_pressed("up"):
		velocity.y -= 1
	if Input.is_action_pressed("down"):
		velocity.y += 1
	if Input.is_action_pressed("left"):
		velocity.x -= 1
	if Input.is_action_pressed("right"):
		velocity.x += 1
	
	velocity = velocity.normalized() * round(speed * delta)
	
	velocity = velocity.floor()

	move_and_slide()

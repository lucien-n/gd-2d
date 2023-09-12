extends Node2D

@export var tile_size: int = 16
@export var chunk_size: int = 16
@export var world_size: int = 4_196

var player: CharacterBody2D
var player_current_chunk: Vector2 = Vector2.ZERO
var player_previous_chunk: Vector2 = Vector2.ZERO

@onready var tilemap = $TileMap
var noise: FastNoiseLite = FastNoiseLite.new()
var moisture_noise: FastNoiseLite = FastNoiseLite.new()
var altitude_noise: FastNoiseLite = FastNoiseLite.new()
var temperature_noise: FastNoiseLite = FastNoiseLite.new()

var generated_chunks: Dictionary = {}

var render_distance: int = 3;
var render_distance_offsets: Array = []

var interface: CanvasLayer

var tiles: Dictionary = {
	"deep_water": 0,
	"shallow_water": 1,
	"sand": 2,
	"lush_grass": 3,
	"grass": 4,
	"cold_grass": 5,
	"stone": 6,
	"snow": 7,
}

var unready_chunks: Array[Vector2] = []
var rerender_chunks: Array[Vector2] = []

var generation_thread: Thread = Thread.new()
var rendering_thread: Thread = Thread.new()

func _ready():
	noise.seed = randi()
	moisture_noise.seed = randi()
	altitude_noise.seed = randi()
	temperature_noise.seed = randi()
	player = get_parent().get_child(0)
	
	render_distance_offsets = generate_render_distance_matrices(render_distance)
	
	interface = get_parent().get_child(2)
	
func _process(_delta):
	player_current_chunk = Vector2(floor(player.global_position.x / tile_size / chunk_size), floor(player.global_position.y / tile_size / chunk_size))
	
	var t1 = Time.get_ticks_usec()
	for offset in render_distance_offsets:
		var chunk_pos: Vector2 = player_current_chunk + offset
		if chunk_pos not in generated_chunks.keys() and not unready_chunks.has(chunk_pos) and not generation_thread.is_started():
			unready_chunks.append(chunk_pos)
			var callable := Callable(self, "generate_chunk")
			callable = callable.bind(chunk_pos)
			generation_thread.start(callable)

	interface.get_child(0).get_child(2).text = "Generating: " + str((Time.get_ticks_usec() - t1) / 1000.) + "ms"
	
	var t2 = Time.get_ticks_usec()
	# DRAWING CHUNKS
	for offset in render_distance_offsets:
			var chunk_pos: Vector2 = player_current_chunk + offset
			if chunk_pos not in generated_chunks.keys() or chunk_pos not in rerender_chunks: continue
			render_chunk(chunk_pos)
				
	interface.get_child(0).get_child(1).text = "Drawing: " + str((Time.get_ticks_usec() - t2) / 1000.) + "ms"
	
func generate_render_distance_matrices(distance):
	var offsets: Array = []
	for x in range((distance * 2) + distance % 2):
		for y in range((distance * 2) + distance % 2):
			offsets.append(Vector2(x - distance, y - distance))
	return offsets

func generate_chunk(chunk_pos: Vector2):
	var chunk: Dictionary = {}
	var offset: Vector2 = chunk_pos * chunk_size
	for x in chunk_size:
		for y in chunk_size:
			var moisture: float = 2 * abs(moisture_noise.get_noise_2dv(Vector2(x, y) + offset))
			var altitude: float = 2 * abs(altitude_noise.get_noise_2dv(Vector2(x, y).floor() + offset))
			var temperature: float = 2 * abs(temperature_noise.get_noise_2dv(Vector2(x, y) + offset))
			
			var t: String = "grass"
			# ocean
			if altitude < 0.2:
				t = "shallow_water"
			# beach
			elif between(altitude, 0.2, 0.3):
				# stone beach
				if temperature < 0.3:
					t = "stone"
				# sand beach
				else:
					t = "sand"
			elif between(altitude, 0.3, 0.8):
				# jungle
				if between(moisture, 0.4, 0.9) and temperature > 0.6:
					t = "lush_grass"
				# desert
				elif temperature > 0.7 and moisture < 0.4:
					t = "sand"
				# snow
				elif temperature < 0.1 and moisture < 0.9:
					t = "snow"
			else:
				# taiga
				if altitude < 1:
					t = "cold_grass"
				# mountain & peak
				elif altitude > 1.2:
					if temperature < 0.1:
						t = "snow"
					else:
						t = "stone"
			
			chunk[Vector2(x, y)] = t
	
	call_deferred("finish_chunk", chunk, chunk_pos)
	
func finish_chunk(chunk: Dictionary, chunk_pos: Vector2):
	generated_chunks[chunk_pos] = chunk
	rerender_chunks.append(chunk_pos)
	if generation_thread.is_started():
		generation_thread.wait_to_finish()

func render_chunk(chunk_pos: Vector2):
	rerender_chunks.erase(chunk_pos)
	for x in range(chunk_size):
		for y in range(chunk_size):
			set_tile(Vector2(
				chunk_pos.x * chunk_size + x,
				chunk_pos.y * chunk_size + y),
				generated_chunks[chunk_pos][Vector2(x, y)])
	
func rerender_chunk(chunk_pos: Vector2):
	if generated_chunks.keys().has(chunk_pos) and chunk_pos not in rerender_chunks:
		rerender_chunks.append(chunk_pos)

func set_tile(pos: Vector2, type: String):
	tilemap.set_cell(0, pos, tiles[type], Vector2.ZERO)

func between(value, min_value, max_value) -> bool:
	return min_value < value and value < max_value

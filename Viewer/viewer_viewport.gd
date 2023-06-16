extends SubViewport

@onready var camera:Camera2D = $displays/camera

func _ready() -> void:
	Signals.rotation_changed.connect(rotate)

func rotate(rotation:int) -> void:
	camera.rotation_degrees = rotation

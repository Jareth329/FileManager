extends SubViewport

@onready var camera:Camera2D = $displays/camera

func _ready() -> void:
	Signals.rotation_changed.connect(update_rotation)

func update_rotation(value:int) -> void:
	camera.rotation_degrees = value

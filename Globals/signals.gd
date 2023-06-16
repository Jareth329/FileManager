extends Node

# initial setup
signal settings_loaded

# viewer
signal rotation_changed
signal fullscreen_viewer(display_node, original_parent_node)

func _ready() -> void:
	rotation_changed.connect(test)

func test() -> void: print("test")

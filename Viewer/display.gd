extends TextureRect

var image_single:TextureRect
var camera:Camera2D
var default_camera_zoom:Vector2
var default_camera_offset:Vector2

var zoom_to_point:bool = true
var zoom_step:float = 0.05
var zoom_max:float = 16
var zoom_min:float = 0.05

func initialize(cam:Camera2D, viewport_display:TextureRect) -> void:
	image_single = viewport_display
	camera = cam
	default_camera_zoom = camera.zoom
	default_camera_offset = camera.offset

func _on_gui_input(event:InputEvent) -> void:
	# note: again, should probably use InputMap here to allow for remapping
	if event is InputEventMouseButton:
		if !event.pressed: return # prevent events from firing twice
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			if zoom_to_point: zoom_point(zoom_step, event.position)
			else: zoom_center(zoom_step)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			if zoom_to_point: zoom_point(-zoom_step, event.position)
			else: zoom_center(-zoom_step)

func zoom_center(step:float) -> void:
	var new_step:float = step * camera.zoom.x
	var new_zoom:float = camera.zoom.x + new_step
	new_zoom = min(zoom_max, new_zoom)
	new_zoom = max(zoom_min, new_zoom)
	camera.zoom = Vector2(new_zoom, new_zoom)

func zoom_point(step:float, position:Vector2) -> void:
	var new_step:float = step * camera.zoom.x
	var new_zoom:float = camera.zoom.x + new_step
	new_zoom = min(zoom_max, new_zoom)
	new_zoom = max(zoom_min, new_zoom)
	var ratio:Vector2 = self.size / image_single.size
	var new_offset:Vector2 = (position - ((image_single.size / 2) * ratio)) * (new_zoom - camera.zoom.x)
	camera.offset += new_offset
	camera.zoom = Vector2(new_zoom, new_zoom)

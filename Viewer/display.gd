extends TextureRect

var image_single:TextureRect
var camera:Camera2D
var default_camera_zoom:Vector2
var default_camera_offset:Vector2

# all of these should be in Settings/SettingsAccess (including the drag variables)
var zoom_to_point:bool = false
var zoom_step:float = 0.05
var zoom_max:float = 16
var zoom_min:float = 0.05

var dragging:bool = false
var drag_speed:float = 1.5
var drag_step:float = 0.4

func initialize(cam:Camera2D, viewport_display:TextureRect) -> void:
	image_single = viewport_display
	camera = cam
	default_camera_zoom = camera.zoom
	default_camera_offset = camera.offset

func _on_gui_input(event:InputEvent) -> void:
	# note: again, should probably use InputMap here to allow for remapping
	if event is InputEventMouseButton:
		if !event.pressed: 
			dragging = false
			return # prevent events from firing twice
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			if zoom_to_point: zoom_point(zoom_step, event.position)
			else: zoom_center(zoom_step)
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			if zoom_to_point: zoom_point(-zoom_step, event.position)
			else: zoom_center(-zoom_step)
		# previously I have used an else condition for this which also allows extra mouse buttons and middle mouse to be used
		elif event.button_index == MOUSE_BUTTON_LEFT:
			dragging = true
		elif event.button_index == MOUSE_BUTTON_RIGHT:
			camera.zoom = default_camera_zoom
			camera.offset = default_camera_offset
			Signals.rotation_changed.emit(0)
	elif event is InputEventMouseMotion and dragging:
		click_drag(event.relative)

func zoom_center(step:float) -> void:
	var new_step:float = step * camera.zoom.x
	var new_zoom:float = camera.zoom.x + new_step
	new_zoom = min(zoom_max, new_zoom)
	new_zoom = max(zoom_min, new_zoom)
	camera.zoom = Vector2(new_zoom, new_zoom)

func zoom_point(step:float, event_position:Vector2) -> void:
	var new_step:float = step * camera.zoom.x
	var new_zoom:float = camera.zoom.x + new_step
	new_zoom = min(zoom_max, new_zoom)
	new_zoom = max(zoom_min, new_zoom)
	var direction:int = -1 if new_step < 0 else 1
	var ratio:Vector2 = self.size / image_single.size
	# need to also add a multiplier based on distance from center (ie offset should be larger further from the center)
	# will need to calculate this with some combination of event_position, self.position and single_iamge.position
	# honestly does not work that well in general right now; also debatable if it is even necessary since
	# click-drag + zoom-center will effectively replicate the ideal results
	var new_offset:Vector2 = (event_position - ((image_single.size / 2) * ratio)) * (new_zoom - camera.zoom.x)
	camera.offset += new_offset
	camera.zoom = Vector2(new_zoom, new_zoom)

func click_drag(relative_position:Vector2) -> void:
	var rot:float = camera.rotation
	var rot_sin:float = sin(rot)
	var rot_cos:float = cos(rot)
	var rot_mult_x:float = (rot_cos * relative_position.x) - (rot_sin * relative_position.y)
	var rot_mult_y:float = (rot_sin * relative_position.x) + (rot_cos * relative_position.y)
	var rot_mult:Vector2 = Vector2(rot_mult_x, rot_mult_y)
	var zoom_mult:float = (zoom_max / camera.zoom.x) * zoom_step
	var rot_offset:Vector2 = rot_mult * zoom_mult * drag_speed 
	camera.offset -= rot_offset
	camera.offset = lerp(camera.offset, camera.offset - rot_offset, drag_step)

extends Control

@onready var margin:MarginContainer = $margin
@onready var vbox:VBoxContainer = $margin/vbox
@onready var ui_ribbon:PanelContainer = $margin/vbox/panel
@onready var display:TextureRect = $margin/vbox/display
@onready var rotation_slider:HSlider = $margin/vbox/panel/margin/hflow/vbox/rotation
@onready var rotation_spinbox:SpinBox = $margin/vbox/panel/margin/hflow/rotation_display
@onready var flip_horizontal:Button = $margin/vbox/panel/margin/hflow/flip_horizontal
@onready var flip_vertical:Button = $margin/vbox/panel/margin/hflow/flip_vertical

# note: currently flip v/h flip display, not displays/image_single
#	the result of this is that it will always flip horizontally/vertically
#	relative to the monitor, not relative to the image itself; this might be useful,
#	but I should probably allow both functionalities, and allow user to toggle which with CTRL or ALT

# changed to InputMap to prepare for allowing remapping buttons
func _unhandled_input(_event:InputEvent) -> void:
	if Input.is_action_just_pressed("viewer_fullscreen"):
		_on_fullscreen_pressed()
	if Input.is_action_just_pressed("viewer_hide_ui"):
		_on_hide_ui_pressed()

func _on_hide_ui_pressed() -> void:
	ui_ribbon.visible = !ui_ribbon.visible

func _on_rotation_display_value_changed(value:int) -> void:
	rotation_slider.value = value

func _on_rotation_value_changed(value:int) -> void: 
	rotation_spinbox.value = value
	Signals.rotation_changed.emit(value)

# these will eventually be textureButtons with left/right and up/down arrows
#	when off the icons will be outlined, when on they will be filled in
func _on_flip_horizontal_pressed() -> void:
	display.flip_h = !display.flip_h
	if display.flip_h: flip_horizontal.text = "  Flip H: ON  "
	else: flip_horizontal.text = "  Flip H: OFF  "

func _on_flip_vertical_pressed() -> void:
	display.flip_v = !display.flip_v
	if display.flip_v: flip_vertical.text = "  Flip V: ON  "
	else: flip_vertical.text = "  Flip V: OFF  "

func _on_fullscreen_pressed() -> void:
	Signals.fullscreen_viewer.emit(vbox, margin)

extends Control

@onready var margin:MarginContainer = $margin
@onready var vbox:VBoxContainer = $margin/vbox
@onready var ui_ribbon:PanelContainer = $margin/vbox/panel
@onready var display:TextureRect = $margin/vbox/display
@onready var rotation_slider:HSlider = $margin/vbox/panel/margin/hflow/vbox/rotation_slider
@onready var rotation_spinbox:SpinBox = $margin/vbox/panel/margin/hflow/rotation_spinbox
@onready var flip_horizontal:Button = $margin/vbox/panel/margin/hflow/flip_horizontal
@onready var flip_vertical:Button = $margin/vbox/panel/margin/hflow/flip_vertical

var viewport_display:TextureRect
var allow_rotation_signal:bool = true

func _ready() -> void:
	Signals.rotation_changed.connect(update_rotation)

# changed to InputMap to prepare for allowing remapping buttons
func _unhandled_input(_event:InputEvent) -> void:
	if Input.is_action_just_pressed("viewer_fullscreen"):
		_on_fullscreen_pressed()
	if Input.is_action_just_pressed("viewer_hide_ui"):
		_on_hide_ui_pressed()

func _on_hide_ui_pressed() -> void:
	ui_ribbon.visible = !ui_ribbon.visible

func _on_rotation_spinbox_value_changed(value:int) -> void:
	if allow_rotation_signal:
		Signals.rotation_changed.emit(value)

func _on_rotation_slider_value_changed(value:int) -> void: 
	if allow_rotation_signal: 
		Signals.rotation_changed.emit(value)

func update_rotation(value:int) -> void:
	allow_rotation_signal = false
	rotation_slider.value = value
	rotation_spinbox.value = value
	allow_rotation_signal = true

# these will eventually be textureButtons with left/right and up/down arrows
#	when off the icons will be outlined, when on they will be filled in
func _on_flip_horizontal_pressed() -> void:
	viewport_display.flip_h = !viewport_display.flip_h
	if viewport_display.flip_h: flip_horizontal.text = "Flip H: ON"
	else: flip_horizontal.text = "Flip H: OFF"

func _on_flip_vertical_pressed() -> void:
	viewport_display.flip_v = !viewport_display.flip_v
	if viewport_display.flip_v: flip_vertical.text = "Flip V: ON"
	else: flip_vertical.text = "Flip V: OFF"

func _on_fullscreen_pressed() -> void:
	Signals.fullscreen_viewer.emit(vbox, margin)

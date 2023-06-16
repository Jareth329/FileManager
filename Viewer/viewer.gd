extends Control

@onready var margin:MarginContainer = $margin
@onready var vbox:VBoxContainer = $margin/vbox
@onready var ui_ribbon:PanelContainer = $margin/vbox/panel
@onready var display:TextureRect = $margin/vbox/display
@onready var rotation_slider:HSlider = $margin/vbox/panel/hflow/vbox/rotation
@onready var rotation_spinbox:SpinBox = $margin/vbox/panel/hflow/rotation_display

# note: I can change the h/v flip buttons to normal buttons and store boolean variables to keep track of state to make them look cleaner
# 	BUT I will need to also change the button color or something when it is active so user knows if image is flipped or not

# changed to InputMap to prepare for allowing remapping buttons
func _unhandled_input(event:InputEvent) -> void:
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
	Signals.rotation_changed.emit()

func _on_flip_horizontal_toggled(button_pressed:bool) -> void:
	display.flip_h = button_pressed

func _on_flip_vertical_toggled(button_pressed:bool) -> void:
	display.flip_v = button_pressed

func _on_fullscreen_pressed() -> void:
	Signals.fullscreen_viewer.emit(vbox, margin)

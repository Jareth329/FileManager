extends VBoxContainer

@onready var simple_section:MarginContainer = $simple
@onready var advanced_section:MarginContainer = $advanced

func _on_advanced_toggled(button_pressed:bool) -> void: 
	simple_section.visible = !button_pressed
	advanced_section.visible = button_pressed

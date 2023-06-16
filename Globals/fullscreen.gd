extends Control

@onready var bg_color:ColorRect = $bg_color

var active:bool = false
var parent:Control

func _ready() -> void:
	Signals.fullscreen_viewer.connect(fullscreen_enable)

func fullscreen_enable(display_node:Control, original_parent:Control) -> void:
	if active:
		active = false
		remove_child(display_node)
		original_parent.add_child(display_node)
		bg_color.hide() # should be hidden regardless of setting
	else:
		active = true
		original_parent.remove_child(display_node)
		add_child(display_node)
		if SettingsAccess.GetEnableFullscreenBackground(): bg_color.show()

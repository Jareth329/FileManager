extends Control

@onready var hsplit:HSplitContainer = $margin/vbox/hbox/hsplit
@onready var vsplit_left:VSplitContainer = $margin/vbox/hbox/hsplit/left
@onready var vsplit_right:VSplitContainer = $margin/vbox/hbox/hsplit/right

# note: will need to replace the rotated text on buttons with images on TextureButtons
#	labels really do not like to be rotated 

func _ready() -> void:
	apply_settings()
	$notes.hide()

# note: once I actually load settings from disk I will need to emit a signal once said loading has finished
func apply_settings() -> void:
	hsplit.split_offset = SettingsAccess.GetHsplitOffset()
	vsplit_left.split_offset = SettingsAccess.GetVsplitOffsetLeft()
	vsplit_right.split_offset = SettingsAccess.GetVsplitOffsetRight()

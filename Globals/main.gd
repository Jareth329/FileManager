extends Control

@export var lister:Control
@export var viewer:Control

@onready var viewport:SubViewport = $viewport
@onready var camera:Camera2D = $viewport/displays/camera
@onready var shaders:Control = $viewport/shaders
@onready var display:TextureRect = $margin/vbox/hbox/hsplit/right/viewer/margin/vbox/display
@onready var image_single:TextureRect = $viewport/displays/image_single

@onready var hsplit:HSplitContainer = $margin/vbox/hbox/hsplit
@onready var vsplit_left:VSplitContainer = $margin/vbox/hbox/hsplit/left
@onready var vsplit_right:VSplitContainer = $margin/vbox/hbox/hsplit/right

# note: will need to replace the rotated text on buttons with images on TextureButtons
#	labels really do not like to be rotated 

func _ready() -> void:
	apply_settings()
	$notes.hide()
	display.texture = viewport.get_texture()
	display.initialize(camera, image_single)
	viewer.viewport_display = image_single

# note: once I actually load settings from disk I will need to emit a signal once said loading has finished
func apply_settings() -> void:
	hsplit.split_offset = SettingsAccess.GetHsplitOffset()
	vsplit_left.split_offset = SettingsAccess.GetVsplitOffsetLeft()
	vsplit_right.split_offset = SettingsAccess.GetVsplitOffsetRight()

# note: I should allow user to toggle between 'Keep Aspect Centered' and 'Keep Aspect Covered'
#	for 'display' as both have their own benefits

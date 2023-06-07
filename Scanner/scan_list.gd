extends VBoxContainer

const row:PackedScene = preload("res://Scanner/scan_item.tscn")

func _ready() -> void: get_tree().root.files_dropped.connect(add_rows)

# also need to send paths to c#, but that will probably be done elsewhere with an additional signal connection
# might even do it on the c# side if I can

# user drops or selects paths to scan
func add_rows(paths:Array) -> void:
	var default_depth:int = SettingsAccess.GetDefaultScanDepth()
	for path in paths:
		var r = row.instantiate()
		var label:Label = r.get_node("hbox/label")
		label.text = "  " + path
		var spinbox:SpinBox = r.get_node("hbox/recursion_depth")
		spinbox.value = default_depth
		self.add_child(r)

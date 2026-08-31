extends VBoxContainer
class_name SettingsMenu

var ResOptions: OptionButton
var WinOptions: OptionButton
var ResArray: Array
var WinModeArray: Array
var WindowLabel: Label
var ResolutionLabel: Label
var WindowOption: OptionButton
var ResolutionOption: OptionButton

enum CurrentOS {
	ANDROID,
	PC,
}

var currentOS: CurrentOS = CurrentOS.ANDROID

var Resolutions: Dictionary = {
	"1280×720": Vector2i(1280, 720),
	"1920x1080": Vector2i(1920, 1080),
	"2560x1440": Vector2i(2560, 1440),
	"3840x2160": Vector2i(3840, 2160),
}

var WindowMode: Dictionary = {
	"Fullscreen": Window.MODE_FULLSCREEN,
	"Windowed": Window.MODE_WINDOWED,
}


func _ready() -> void:
	WindowLabel = get_node("WindowModeLabel")
	ResolutionLabel = get_node("ResolutionLabel")
	WindowOption = get_node("WindowMode")
	ResolutionOption = get_node("Resolution")

	if OS.has_feature("mobile"):
		currentOS = CurrentOS.ANDROID
	elif OS.has_feature("pc"):
		currentOS = CurrentOS.PC

	ResArray = []
	ResArray.resize(4)
	ResOptions = get_node("Resolution")
	var optionIndex := 0
	ResOptions.selected = 1
	for key in Resolutions:
		ResOptions.add_item(key)
		if Resolutions[key] == get_window().size:
			ResOptions.selected = optionIndex
		optionIndex += 1
	ResArray = Resolutions.values()
	get_tree().root.content_scale_size = ResArray[1]

	WinModeArray = []
	WinModeArray.resize(2)
	WinOptions = get_node("WindowMode")
	var winOptionIndex := 0

	for key in WindowMode:
		WinOptions.add_item(key)
		winOptionIndex += 1
	WinModeArray = WindowMode.values()

	match currentOS:
		CurrentOS.ANDROID:
			# Hide settings only pc should see.
			WindowLabel.hide()
			WindowOption.hide()
		CurrentOS.PC:
			# show settings for pc build
			WindowLabel.show()
			WindowOption.show()
		_:
			print("Invalid operating system")


func _on_resolution_item_selected(index: int) -> void:
	get_tree().root.content_scale_size = ResArray[index]


func _on_window_mode_item_selected(index: int) -> void:
	get_window().mode = WinModeArray[index]


func _on_h_slider_value_changed(value: float) -> void:
	var busIndex := AudioServer.get_bus_index("Master")
	AudioServer.set_bus_volume_db(busIndex, value)


func _on_music_slider_value_changed(value: float) -> void:
	var busIndex := AudioServer.get_bus_index("Music")
	AudioServer.set_bus_volume_db(busIndex, value)


func _on_sfx_value_changed(value: float) -> void:
	var busIndex := AudioServer.get_bus_index("SFX")
	AudioServer.set_bus_volume_db(busIndex, value)

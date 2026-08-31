extends Node
## Autoload singleton (was res://scripts/SaveManager.cs). Reachable globally as
## `SaveManager` and at `/root/SaveManager`.

const CONFIG_SAVE_PATH := "user://settings.cfg"

var PostTitleScreen: bool = false

var Resolutions: Array[Vector2i] = [
	Vector2i(1280, 720),
	Vector2i(1920, 1080),
	Vector2i(2560, 1440),
	Vector2i(3840, 2160),
]

var ResOptions: OptionButton
var MVSlider: Slider
var MusicSlider: Slider
var SFXSlider: Slider


func _window_mode_to_string(m: int) -> String:
	match m:
		Window.MODE_WINDOWED:
			return "Windowed"
		Window.MODE_MINIMIZED:
			return "Minimized"
		Window.MODE_MAXIMIZED:
			return "Maximized"
		Window.MODE_FULLSCREEN:
			return "Fullscreen"
		Window.MODE_EXCLUSIVE_FULLSCREEN:
			return "ExclusiveFullscreen"
	return "Windowed"


func LoadConfig() -> void:
	var config := ConfigFile.new()

	var err: int
	if OS.has_feature("mobile"):
		err = config.load(OS.get_user_data_dir() + "settings.cfg")
	else:
		err = config.load(CONFIG_SAVE_PATH)

	# If the file didn't load, ignore it.
	if err != OK:
		return

	var Resolution: Vector2i = config.get_value("Settings", "Resolution", Vector2i(1280, 720))
	var WindowMode: String = config.get_value("Settings", "WindowMode", "Windowed")
	var MasterVolume: float = config.get_value("Volume", "Master", 0.0)
	var MusicVolume: float = config.get_value("Volume", "Music", 0.0)
	var SFXVolume: float = config.get_value("Volume", "SFX", 0.0)

	get_tree().root.content_scale_size = Resolution

	if PostTitleScreen:
		ResOptions = get_node("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/Resolution")
		ResOptions.selected = GetResolutionIndex(Resolutions, Resolution)

		MVSlider = get_node("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/MVSlider")
		MVSlider.value = MasterVolume

		SFXSlider = get_node("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/MusicSlider")
		SFXSlider.value = SFXVolume

		MusicSlider = get_node("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/sfx")
		MusicSlider.value = MusicVolume

		# NOTE: pre-existing logic from the C# version - the window mode and the
		# audio bus volumes are only applied when the saved WindowMode is "Fullscreen".
		if WindowMode == "Fullscreen":
			ResOptions = get_node("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/Resolution")
			ResOptions.selected = GetResolutionIndex(Resolutions, Resolution)

			if WindowMode == "Fullscreen":
				get_window().mode = Window.MODE_FULLSCREEN
			if WindowMode == "Windowed":
				get_window().mode = Window.MODE_WINDOWED

			var busIndex := AudioServer.get_bus_index("Master")
			AudioServer.set_bus_volume_db(busIndex, MasterVolume)

			busIndex = AudioServer.get_bus_index("Music")
			AudioServer.set_bus_volume_db(busIndex, MusicVolume)

			busIndex = AudioServer.get_bus_index("SFX")
			AudioServer.set_bus_volume_db(busIndex, SFXVolume)


func SaveConfig() -> void:
	var config := ConfigFile.new()

	config.set_value("Settings", "Resolution", get_tree().root.content_scale_size)
	config.set_value("Settings", "WindowMode", _window_mode_to_string(get_window().mode))

	var busIndex := AudioServer.get_bus_index("Master")
	config.set_value("Volume", "Master", AudioServer.get_bus_volume_db(busIndex))

	busIndex = AudioServer.get_bus_index("Music")
	config.set_value("Volume", "Music", AudioServer.get_bus_volume_db(busIndex))

	busIndex = AudioServer.get_bus_index("SFX")
	config.set_value("Volume", "SFX", AudioServer.get_bus_volume_db(busIndex))

	if OS.has_feature("mobile"):
		config.save(OS.get_user_data_dir() + "settings.cfg")
	else:
		config.save(CONFIG_SAVE_PATH)


func SaveScore(PlayerName: String, highScore: int, highestCombo: int) -> int:
	var PLAYER_SCORE_SAVE_PATH := "user://" + PlayerName + "playerscore.cfg"
	var score := ConfigFile.new()
	score.set_value(PlayerName, "Score", highScore)
	score.set_value(PlayerName, "HighestCombo", highestCombo)
	return score.save(PLAYER_SCORE_SAVE_PATH)


func LoadScore(PlayerName: String) -> Vector2:
	var PLAYER_SCORE_SAVE_PATH := "user://" + PlayerName + "playerscore.cfg"
	var score := ConfigFile.new()
	var _err := score.load(PLAYER_SCORE_SAVE_PATH)
	var highScore: int = score.get_value(PlayerName, "Score", 0)
	var highestCombo: int = score.get_value(PlayerName, "HighestCombo", 0)
	return Vector2(highScore, highestCombo)


func GetResolutionIndex(resolutions: Array[Vector2i], targetRes: Vector2i) -> int:
	for i in resolutions.size():
		if resolutions[i] == targetRes:
			return i
	return -1

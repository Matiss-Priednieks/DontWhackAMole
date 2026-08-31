extends Node3D
class_name Game

enum GameState {
	MENU,
	PLAYING,
	PAUSED,
	SHOP,
}

enum GameMode {
	STORY,
	ARCADE,
}


## Was a C# `struct UIVisibilityState`. GDScript has no structs, so each state is
## a Dictionary with these nine bool keys.
static func _ui_state(menu_buttons: bool, settings_menu: bool, help_menu: bool, account_menu: bool, login_screen: bool, registration_screen: bool, power_up_ui: bool, shop: bool, leaderboard: bool) -> Dictionary:
	return {
		"MenuButtonsVisible": menu_buttons,
		"SettingsMenuVisible": settings_menu,
		"HelpMenuVisible": help_menu,
		"AccountMenuVisible": account_menu,
		"LoginScreenVisible": login_screen,
		"RegistrationScreenVisible": registration_screen,
		"PowerUpUIVisible": power_up_ui,
		"ShopVisible": shop,
		"LeaderboardVisible": leaderboard,
	}

var CurrentGameMode: GameMode = GameMode.ARCADE
var currentState: GameState = GameState.MENU
var MenuDelay: float = 0.5
var CamPlayFOV: float
var CamMenuFOV: float
var MainCam: Camera3D
var CamPlayPos: Vector3
var CamPlayRot: Vector3
var CamMenuPos: Vector3
var CamMenuRot: Vector3
var CamShopPos: Vector3
var CamShopRot: Vector3
var RNG: RandomNumberGenerator
var SaveManagerRef: Node
var User: Node
var MalletNode: Mallet
var MoleNode: Mole
var MainMenu: Control
var GameOverMenu: Control
var PlayResume: Button
var MenuButtons: PanelContainer
var HelpMenu: PanelContainer
var SettingsMenu: PanelContainer
var AccountMenu: PanelContainer
var ShopVisible: PanelContainer
var LoginScreen: Panel
var RegistrationScreen: Panel
var ComboCointTimer: Timer

var worldEnvironment: WorldEnvironment

var PopOutTimer: Timer
var PowerUpUI: MarginContainer
var LeaderboardUI: MarginContainer

var CoinScene: PackedScene
var HeartScene: PackedScene
var EarlyPopScene: PackedScene
var Login: bool
var Register: bool
var CameraMoving: bool = false

var MenuJustPressed: bool

## Game-over is edge-triggered now - the desaturate/resaturate happens once on
## the transition instead of spawning a fresh Tween every frame.
var _wasGameOver: bool = false
var _satTween: Tween

var _music: AudioStreamPlayer

## Camera lean + fake motion blur. Both are single floats summed once per frame in
## _updateDynamicCamera - no competing tweens - so the clutch kick and the release
## are the same motion and can't fight each other.
var _leanFov: float = 0.0
var _blurAmt: float = 0.0
var _blurLayer: CanvasLayer
var _blurRect: ColorRect
var _blurMat: ShaderMaterial

const _FAKE_BLUR_SHADER := "
shader_type canvas_item;
uniform float amount : hint_range(0.0, 1.0) = 0.0;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear;
void fragment() {
	vec2 dir = vec2(0.5) - SCREEN_UV;
	vec3 col = vec3(0.0);
	const int N = 6;
	for (int i = 0; i < N; i++) {
		float t = float(i) / float(N - 1);
		col += texture(screen_tex, SCREEN_UV + dir * t * amount * 0.10).rgb;
	}
	COLOR = vec4(col / float(N), 1.0);
}
"

var CollectableSpawnTimes: Array[int] = [4, 4, 6, 8, 8, 8, 16]

var Collectables: Array[PackedScene]

var MenuUIState: Dictionary
var PlayingUIState: Dictionary
var ShopUIState: Dictionary
var SettingsUIState: Dictionary
var LoginUIState: Dictionary
var RegistrationUIState: Dictionary
var HelpUIState: Dictionary
var AccountUIState: Dictionary


func _ready() -> void:
	PowerUpUI = get_node("%Powerup")
	Collectables = [null, null, null]
	worldEnvironment = get_node("%WorldEnvironment")
	User = get_node("/root/LoggedInUser")
	User.LoginInitialisation()
	SaveManagerRef = get_tree().root.get_node("SaveManager")

	LeaderboardUI = get_node("UI/Menu/%Leaderboard")

	LoginScreen = get_node("%LoginScreen")
	ShopVisible = get_node("%ShopMenu")
	RegistrationScreen = get_node("%RegistrationScreen")

	RNG = RandomNumberGenerator.new()
	MenuButtons = get_node("%MenuButtons")
	AccountMenu = get_node("%AccountMenu")
	HelpMenu = get_node("%HelpScreen")
	SettingsMenu = get_node("%SettingsMenu")
	MainMenu = get_node("%Menu")
	MainCam = get_node("Camera3D")
	MalletNode = get_node("%Mallet")
	MoleNode = get_node("%Mole")
	GameOverMenu = get_node("%GameOver")
	PlayResume = get_node("%PlayResume")
	ComboCointTimer = get_node("%ComboCoinTimer")
	PopOutTimer = get_node("%PopOutTimer")
	_music = get_node("AudioStreamPlayer")
	MoleNode.ClutchDodge.connect(_on_clutch_dodge)
	_setupFakeBlur()

	CoinScene = ResourceLoader.load("res://scenes/Coin.tscn")
	HeartScene = ResourceLoader.load("res://scenes/HeartContainer.tscn")
	EarlyPopScene = ResourceLoader.load("res://scenes/EarlyPop.tscn")

	CamPlayPos = Vector3(0, 2.403, -9.956)
	CamPlayRot = Vector3(-28.7, 0, 0)
	CamPlayFOV = 55.0

	CamMenuPos = Vector3(-3.25, 1.63, -9.841)  # old menu
	CamMenuRot = Vector3(0, 90, 0)             # old menu
	CamMenuFOV = 80.0

	CamShopPos = Vector3(3.5, 1.5, -7.1)
	CamShopRot = Vector3(0, -91.1, 0)

	PlayResume.text = "Play"
	MainCam.position = CamMenuPos

	MoleNode.CurrentGameState = Mole.GameState.Paused

	Collectables[0] = CoinScene
	Collectables[1] = HeartScene
	Collectables[2] = EarlyPopScene
	SaveManagerRef.PostTitleScreen = true
	SaveManagerRef.LoadConfig()

	MenuUIState = _ui_state(true, false, false, false, false, false, false, false, true)
	PlayingUIState = _ui_state(false, false, false, false, false, false, true, false, false)
	ShopUIState = _ui_state(false, false, false, false, false, false, false, true, false)
	SettingsUIState = _ui_state(false, true, false, false, false, false, false, false, false)
	LoginUIState = _ui_state(false, false, false, true, true, false, false, false, false)
	RegistrationUIState = _ui_state(false, false, false, true, false, true, false, false, false)
	HelpUIState = _ui_state(false, false, true, false, false, false, false, false, true)
	AccountUIState = _ui_state(false, false, false, true, false, false, false, false, false)
	ToggleMenuVisibility(MenuUIState)


func _process(delta: float) -> void:
	if Input.is_action_just_released("menu"):
		if not CameraMoving:
			ToggleMenu()

	var isGameOver := MoleNode.CurrentGameState == Mole.GameState.GameOver
	if isGameOver != _wasGameOver:
		_wasGameOver = isGameOver
		HandleGameOver(isGameOver)

	_updateDynamicCamera(delta)
	_updateMusicIntensity(delta)


## Full-screen radial-blur quad, built in code so there's no scene/asset to manage.
func _setupFakeBlur() -> void:
	_blurLayer = CanvasLayer.new()
	_blurLayer.layer = 3
	add_child(_blurLayer)
	_blurRect = ColorRect.new()
	_blurRect.set_anchors_preset(Control.PRESET_FULL_RECT)
	_blurRect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_blurRect.visible = false
	var sh := Shader.new()
	sh.code = _FAKE_BLUR_SHADER
	_blurMat = ShaderMaterial.new()
	_blurMat.shader = sh
	_blurRect.material = _blurMat
	_blurLayer.add_child(_blurRect)


## Camera eases in as the mallet's danger meter climbs and releases when you dodge;
## a clutch dodge adds an extra kick to the same offset plus a short radial blur.
func _updateDynamicCamera(delta: float) -> void:
	_blurAmt = move_toward(_blurAmt, 0.0, delta * 6.0)
	if _blurAmt > 0.001:
		_blurRect.visible = true
		_blurMat.set_shader_parameter("amount", _blurAmt)
	elif _blurRect.visible:
		_blurRect.visible = false

	if CameraMoving or currentState != GameState.PLAYING:
		_leanFov = move_toward(_leanFov, 0.0, delta * 30.0)
		return

	var danger := clampf(MoleNode.GetDangerTimer() / maxf(MoleNode.OutTooLongTime, 0.01), 0.0, 1.0)
	var target := -3.5 * danger
	if MoleNode.GetLives() <= 1:
		target -= 2.0
	_leanFov = lerpf(_leanFov, target, clampf(delta * 4.0, 0.0, 1.0))
	MainCam.fov = CamPlayFOV + _leanFov


## Nudges the music pitch up as the game speeds toward its floor (PopSpeed 2.0 -> 0.75).
func _updateMusicIntensity(delta: float) -> void:
	var want := 1.0
	if currentState == GameState.PLAYING and MoleNode.CurrentGameState == Mole.GameState.Playing:
		var t := clampf(inverse_lerp(2.0, 0.75, MoleNode.PopSpeed), 0.0, 1.0)
		want = 1.0 + t * 0.12
	_music.pitch_scale = lerpf(_music.pitch_scale, want, clampf(delta * 2.0, 0.0, 1.0))


func _on_clutch_dodge() -> void:
	MalletNode.Hitstop(0.11)
	if currentState != GameState.PLAYING or CameraMoving:
		return
	_blurAmt = 1.0
	_leanFov -= 3.0  # extra kick on top of the danger lean; eases out with the rest


func MoveCamera(Pos: Vector3, Rot: Vector3, CamFOV: float) -> void:
	CameraMoving = true
	var MoveCam := get_tree().create_tween()
	MoveCam.tween_property(MainCam, "position", Pos, MenuDelay).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	var RotCam := get_tree().create_tween()
	RotCam.tween_property(MainCam, "rotation_degrees", Rot, MenuDelay).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	var FovCam := get_tree().create_tween()
	FovCam.tween_property(MainCam, "fov", CamFOV, MenuDelay).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN)
	await get_tree().create_timer(MenuDelay).timeout
	CameraMoving = false


func _on_combo_coin_timer_timeout() -> void:
	# Generates random number from 0 to 100, used to decide whether a regular coin
	# or an extra-life coin spawns. Extra-life coin is approx 1 in 10.
	var coinFavour := RNG.randi_range(0, 100)
	var sceneInstance: Node

	# Extra life only has a chance to spawn if your lives are below 3.
	if MoleNode.GetLives() < 3:
		if coinFavour > 10:
			sceneInstance = Collectables[PickRandomPickup()].instantiate()
		else:
			sceneInstance = Collectables[1].instantiate()
	else:
		sceneInstance = Collectables[PickRandomPickup()].instantiate()
	if MoleNode.CurrentGameState == Mole.GameState.Playing:
		add_child(sceneInstance)
		ComboCointTimer.start(CollectableSpawnTimes[RNG.randi_range(0, CollectableSpawnTimes.size() - 1)])


func PickRandomPickup() -> int:
	var secondCoinFavour := RNG.randi_range(0, 100)
	var randomIndex: int
	if secondCoinFavour <= 60:
		randomIndex = 0
	elif MoleNode.EarlyPops < 1:
		randomIndex = 2
	else:
		randomIndex = 0
	return randomIndex


func ToggleMenu() -> void:
	SaveManagerRef.SaveConfig()
	match currentState:
		GameState.MENU:
			ToggleMenuVisibility(MenuUIState)
		GameState.PLAYING:
			ToggleMenuVisibility(MenuUIState)

			if not MenuJustPressed:
				MenuJustPressed = true
				PopOutTimer.paused = false
				MoleNode.CurrentGameState = Mole.GameState.Paused
				MoveToMenuState(GameState.PAUSED)
				await get_tree().create_timer(MenuDelay).timeout
				MainMenu.show()
				MenuJustPressed = false
		GameState.PAUSED:
			ToggleMenuVisibility(PlayingUIState)
			if not MenuJustPressed:
				MenuJustPressed = true
				MoveToMenuState(GameState.PLAYING)
				MainMenu.hide()
				await get_tree().create_timer(MenuDelay).timeout
				PopOutTimer.paused = false
				MoleNode.CurrentGameState = Mole.GameState.Playing
				MenuJustPressed = false
		GameState.SHOP:
			ToggleMenuVisibility(MenuUIState)

			if not MenuJustPressed:
				MenuJustPressed = true
				PopOutTimer.paused = false
				MoleNode.CurrentGameState = Mole.GameState.Paused
				MoveToMenuState(GameState.MENU)
				await get_tree().create_timer(MenuDelay).timeout
				MainMenu.show()
				MenuJustPressed = false


func MoveToMenuState(newState: GameState) -> void:
	if currentState != newState:
		match newState:
			GameState.MENU:
				MoveCamera(CamMenuPos, CamMenuRot, CamMenuFOV)
			GameState.PLAYING:
				MoveCamera(CamPlayPos, CamPlayRot, CamPlayFOV)
			GameState.SHOP:
				MoveCamera(CamShopPos, CamShopRot, CamMenuFOV)
			GameState.PAUSED:
				MoveCamera(CamMenuPos, CamMenuRot, CamMenuFOV)
		currentState = newState


func HandleGameOver(isGameOver: bool) -> void:
	if _satTween and _satTween.is_valid():
		_satTween.kill()
	_satTween = create_tween()

	if isGameOver:
		GameOverMenu.show()
		_satTween.tween_property(worldEnvironment.environment, "adjustment_saturation", 0.0, 1.0) \
			.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		if currentState == GameState.PAUSED:
			PopOutTimer.paused = false
			MoleNode.CurrentGameState = Mole.GameState.Paused
	else:
		GameOverMenu.hide()
		_satTween.tween_property(worldEnvironment.environment, "adjustment_saturation", 1.0, 1.0) \
			.set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)


func ToggleMenuVisibility(state: Dictionary) -> void:
	MenuButtons.visible = state["MenuButtonsVisible"]
	SettingsMenu.visible = state["SettingsMenuVisible"]
	HelpMenu.visible = state["HelpMenuVisible"]
	AccountMenu.visible = state["AccountMenuVisible"]
	LoginScreen.visible = state["LoginScreenVisible"]
	RegistrationScreen.visible = state["RegistrationScreenVisible"]
	PowerUpUI.visible = state["PowerUpUIVisible"]
	ShopVisible.visible = state["ShopVisible"]
	LeaderboardUI.visible = state["LeaderboardVisible"]


func _on_restart_pressed() -> void:
	MoveToMenuState(GameState.PLAYING)
	MoleNode.CurrentGameState = Mole.GameState.Playing
	MoleNode.Restart()
	ComboCointTimer.start(RNG.randi_range(3, 6))
	GameOverMenu.hide()
	ToggleMenuVisibility(PlayingUIState)


func _on_main_menu_pressed() -> void:
	MoveToMenuState(GameState.MENU)
	MoleNode.CurrentGameState = Mole.GameState.Paused
	MoleNode.Restart()
	GameOverMenu.hide()
	MainMenu.show()
	PlayResume.text = "Play"
	ToggleMenuVisibility(MenuUIState)


func _on_shop_pressed() -> void:
	MoveToMenuState(GameState.SHOP)
	User.GetUnlockedContentRequest()
	MoleNode.CurrentGameState = Mole.GameState.Paused
	GameOverMenu.hide()
	MainMenu.show()
	ToggleMenuVisibility(ShopUIState)


# play/resume button
func _on_button_pressed() -> void:
	if MoleNode.CurrentGameState != Mole.GameState.Paused:
		MoleNode.Restart()
		ComboCointTimer.start(RNG.randi_range(3, 6))
		MoleNode.CurrentGameState = Mole.GameState.Playing
	MainMenu.hide()
	ComboCointTimer.start(RNG.randi_range(3, 6))
	MoveToMenuState(GameState.PLAYING)
	await get_tree().create_timer(MenuDelay + 1.0).timeout  # delay before gameplay resumes (default 1.5s)
	MoleNode.CurrentGameState = Mole.GameState.Playing
	ToggleMenuVisibility(PlayingUIState)


func _on_settings_pressed() -> void:
	ToggleMenuVisibility(SettingsUIState)


func _on_back_pressed() -> void:
	User.GetUnlockedContentRequest()
	SaveManagerRef.SaveConfig()
	MoveToMenuState(GameState.MENU)
	ToggleMenuVisibility(MenuUIState)


func _on_help_pressed() -> void:
	ToggleMenuVisibility(HelpUIState)


func _on_account_pressed() -> void:
	if User.LoggedIn:
		ToggleMenuVisibility(AccountUIState)
	else:
		ToggleMenuVisibility(LoginUIState)


func _on_go_to_reg_page_pressed() -> void:
	ToggleMenuVisibility(RegistrationUIState)


func _on_go_to_login_pressed() -> void:
	ToggleMenuVisibility(LoginUIState)


func _on_as_guest_pressed() -> void:
	ToggleMenuVisibility(MenuUIState)


func _on_exit_pressed() -> void:
	SaveManagerRef.SaveConfig()
	get_tree().quit()


func _on_unlocks_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		print("Successfully bought!")


func _on_unlocks_info_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		var json := JSON.new()
		json.parse(body.get_string_from_utf8())

		var data: Dictionary = json.data if json.data is Dictionary else {}

		var unlockDictionary: Variant = data.get("unlocks", {})
		User.AccountCurrency = int(data.get("collected_coins", 0))

		User.SetUnlocksDict(unlockDictionary)

		print("unlocks print in unlocks request: " + str(unlockDictionary))

		User.FinalBuyCheck()


func _on_currency_update_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		print("curreny update attempted")
	else:
		print("failed")


func _on_buy_attempt_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		print("Buy request 4")
		var json := JSON.new()
		json.parse(body.get_string_from_utf8())

		var data: Dictionary = json.data if json.data is Dictionary else {}

		var unlockDictionary: Variant = data.get("unlocks", {})

		User.SetUnlocksDict(unlockDictionary)
		print("\n" + str(unlockDictionary) + "\n")
		User.PurchasedItemUpdate(unlockDictionary)
	else:
		print(responseCode)


func _on_get_highscore_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		var json := JSON.new()
		json.parse(body.get_string_from_utf8())

		print("Raw JSON data " + str(json.data))
		var highscores: Dictionary = json.data if json.data is Dictionary else {}
		if User.LoggedIn:
			var hs_list: Array = highscores.get("highscore", [])
			if hs_list.is_empty():
				User.SetHighscore(0)
				MoleNode.SetHighScore(0)
			else:
				User.SetHighscore(hs_list[0])
				MoleNode.SetHighScore(hs_list[0])
	else:
		print(responseCode)

extends Area3D
class_name Mole

var User: Node # /root/LoggedInUser autoload
var SaveManagerRef: Node # /root/SaveManager autoload

signal OutTooLong(Position: Vector3)
## Fired when the mole bails out of a hole with the mallet already bearing down -
## a last-second dodge. Game.gd turns this into the hitstop + camera snap.
signal ClutchDodge

var Holes: Array[Vector3]
var Down: bool = true
var ChosenHole: Vector3
var PopOutTimer: Timer
var OutTimer: Timer
var DangerTimer: float = 0.0
var Lives: int = 3
var MoleMesh: MeshInstance3D
var LivesCounter: MeshInstance3D
var ScoreCounter: MeshInstance3D
var ComboCounter: MeshInstance3D
var HighScoreMesh: MeshInstance3D
var HighestComboMesh: MeshInstance3D
var EarlyPopCounter: MeshInstance3D
var ScoreAcceleration: float = 0.0
var TotalCollectedCoins: int = 0
var SCALE: Vector3

var Score: float = 0.0
## Setter-with-side-effect mirror of `Score` (was a C# property pair).
var score: float:
	get:
		return Score
	set(value):
		Score = value
		PlaySoundDelayed()

var CoinsAdded: bool
@export var CurrentHat: Node3D

var EarlyPops: int = 0

var IFrames: int = 60

enum GameState {
	Playing,
	Paused,
	GameOver,
}

var CurrentGameState: GameState = GameState.Paused
var CameraRef: Camera3D
var RNG: RandomNumberGenerator

## The single tween that owns the mole's position (pop out / duck down / recoil).
## Kept in one place so a new move cancels the previous one instead of fighting it.
var moveTween: Tween
var squishTween: Tween
var Bonk: AudioStreamPlayer3D
var Move: AudioStreamPlayer3D
var CounterSound: AudioStreamPlayer3D
var WarningIndicator: Node3D
var FinalScore: int
var HighScore: int
var ComboBonus: int
var HighestCombo: int
var PopSpeed: float
var OutTooLongTime: float
var CounterSound2: AudioStream
var PowerUpButton: TextureButton
var FinalScoreLabel: Label

## Per-instance copies of the LED text material so the score / combo counters can
## glow independently without touching the shared resource other labels use.
var _scoreMat: StandardMaterial3D
var _comboMat: StandardMaterial3D
const _LED_ENERGY := 3.29 # emission_energy_multiplier of the base RedLED material


func _ready() -> void:
	SCALE = scale
	OutTooLongTime = 1
	PopSpeed = 2
	User = get_node("/root/LoggedInUser")
	SaveManagerRef = get_node("/root/SaveManager")
	Score = 0
	ComboBonus = 1
	CounterSound = get_node("../%CounterSound")

	PowerUpButton = get_node("../UI/GameplayUI/Powerup/MarginContainer/PanelContainer/MarginContainer/%UsePowerup")
	CameraRef = get_node("../%Camera3D")
	MoleMesh = get_node("%MoleMesh")
	WarningIndicator = get_node("%WarningIndicator")
	LivesCounter = get_node("../%LivesCounter")
	ScoreCounter = get_node("../%ScoreCounter")
	ComboCounter = get_node("../%ComboCounter")
	EarlyPopCounter = get_node("../%EarlyPopCounter")
	HighScoreMesh = get_node("../%Highscore")
	HighestComboMesh = get_node("../%HighestCombo")
	Bonk = get_node("%BonkSound")
	Move = get_node("%MoveSound")
	FinalScoreLabel = get_node("../%FinalScore")

	RNG = RandomNumberGenerator.new()

	Holes = [
		Vector3(0, 0.85, -11.848), # top(W)
		Vector3(-0.24, 0.85, -11.638), # left (A)
		Vector3(0, 0.85, -11.428), # bottom (S)
		Vector3(0.24, 0.85, -11.638), # right (D)
	]
	ChosenHole = Holes[0]
	position = Holes[0]

	PopOutTimer = get_node("../%PopOutTimer")
	OutTimer = get_node("%OutTimer")
	var savedScore: Vector2 = SaveManagerRef.LoadScore(User.Username)
	HighScore = int(savedScore.x)
	HighestCombo = int(savedScore.y)
	PopOutTimer.start(PopSpeed)
	CounterSound2 = ResourceLoader.load("res://assets/audio/counter.wav")

	_scoreMat = _own_led_material(ScoreCounter)
	_comboMat = _own_led_material(ComboCounter)


## Give a counter its own copy of the LED material as a material_override so we can
## animate its glow without affecting every other counter sharing RedLED.tres.
func _own_led_material(counter: MeshInstance3D) -> StandardMaterial3D:
	var src := (counter.mesh as TextMesh).material as StandardMaterial3D
	var m: StandardMaterial3D = src.duplicate() if src else StandardMaterial3D.new()
	counter.material_override = m
	return m


## Only writes to a TextMesh when the string actually changed - each assignment
## re-tessellates the glyph mesh, and this runs for six labels every frame.
func _set_mesh_text(m: MeshInstance3D, value: String) -> void:
	var tm := m.mesh as TextMesh
	if tm.text != value:
		tm.text = value


func _process(delta: float) -> void:
	PowerUpButton.disabled = EarlyPops <= 0
	if Score > HighScore:
		HighScore = int(Score)
		User.SetHighscore(HighScore)
	if ComboBonus > HighestCombo:
		HighestCombo = ComboBonus

	_set_mesh_text(LivesCounter, str(Lives))
	_set_mesh_text(EarlyPopCounter, str(EarlyPops) + "/1")
	_set_mesh_text(ScoreCounter, str(round(Score)))
	_set_mesh_text(ComboCounter, "x" + str(ComboBonus))
	_set_mesh_text(HighScoreMesh, "Highscore: " + str(HighScore))
	_set_mesh_text(HighestComboMesh, "x" + str(HighestCombo))

	_updateCounterGlow(delta)


## Score counter flares while you're exposed and racking up points; combo counter
## shifts red -> gold -> white-hot as the multiplier climbs.
func _updateCounterGlow(delta: float) -> void:
	var comboT := clampf(float(ComboBonus - 1) / 15.0, 0.0, 1.0)
	_comboMat.albedo_color = Color(1.0, lerpf(0.0, 0.85, comboT), lerpf(0.02, 0.7, comboT))
	_comboMat.emission = Color(1.0, lerpf(0.0, 0.8, comboT), lerpf(0.0, 0.6, comboT))
	_comboMat.emission_energy_multiplier = lerpf(_LED_ENERGY, 7.0, comboT)

	var scoring := not Down and CurrentGameState == GameState.Playing
	var scoreTarget := lerpf(_LED_ENERGY + 1.0, 10.0, comboT) if scoring else _LED_ENERGY
	_scoreMat.emission_energy_multiplier = move_toward(_scoreMat.emission_energy_multiplier, scoreTarget, delta * 14.0)


func _physics_process(delta: float) -> void:
	match CurrentGameState:
		GameState.Playing:
			CoinsAdded = false
			ProcessInput()
			UpdateScore()
			CheckGameOver()
			if IFrames <= 60 and IFrames > 0:
				IFrames -= 1
		GameState.Paused:
			pass
		GameState.GameOver:
			if not CoinsAdded:
				CoinsAdded = true
				print("Coin update called")
				print(TotalCollectedCoins)
				User.UpdateUserCurrency(TotalCollectedCoins)
				TotalCollectedCoins = 0

	FinalScore = int(Score)
	FinalScoreLabel.text = str(FinalScore)


func ProcessInput() -> void:
	if Input.is_action_just_pressed("top_hole"):
		MoveMole(0)
	elif Input.is_action_just_pressed("left_hole"):
		MoveMole(1)
	elif Input.is_action_just_pressed("bottom_hole"):
		MoveMole(2)
	elif Input.is_action_just_pressed("right_hole"):
		MoveMole(3)
	if Input.is_action_just_released("popout"):
		EarlyPopOut()


func EarlyPopOut() -> void:
	if EarlyPops > 0:
		if Down and Lives > 0 and CurrentGameState == GameState.Playing:
			PopOut()
			EarlyPops -= 1


func _on_use_powerup_pressed() -> void:
	EarlyPopOut()


func UpdateScore() -> void:
	if not Down:
		ScoreAcceleration += 0.005 * ComboBonus
		score += ScoreAcceleration


func CheckGameOver() -> void:
	if Lives <= 0:
		CurrentGameState = GameState.GameOver


func _on_down_swipe(Hole: int) -> void:
	MoveMole(Hole)


func _on_right_swipe(Hole: int) -> void:
	MoveMole(Hole)


func _on_left_swipe(Hole: int) -> void:
	MoveMole(Hole)


func _on_up_swipe(Hole: int) -> void:
	MoveMole(Hole)


func MoveMole(Hole: int) -> void:
	Move.play()
	if Holes[Hole] != ChosenHole:
		ChosenHole = Holes[Hole]
		PopDown()


func PlaySoundDelayed() -> void:
	var scoreCounter := AudioStreamPlayer3D.new()
	scoreCounter.stream = CounterSound2
	scoreCounter.volume_db = -20
	add_child(scoreCounter)
	scoreCounter.play(0)

	await get_tree().create_timer(0.5).timeout
	scoreCounter.queue_free()


func Restart() -> void:
	Score = 0
	Lives = 3
	DangerTimer = 0
	if FinalScore > HighScore:
		HighScore = int(FinalScore)
		User.SetHighscore(HighScore)
	SaveManagerRef.SaveScore(User.Username, HighScore, HighestCombo)
	var requestResult: int = User.HighscoreUpdateRequest()
	print("Score sent to server.")


func _on_pop_out_timer_timeout() -> void:
	if Down and Lives > 0 and CurrentGameState == GameState.Playing:
		PopOut()
	PopOutTimer.start(PopSpeed)


func _reset_move_tween() -> Tween:
	if moveTween and moveTween.is_valid():
		moveTween.kill()
	moveTween = create_tween()
	return moveTween


func PopOut() -> void:
	var velTween := _reset_move_tween()
	velTween.tween_property(self, "position", Vector3(ChosenHole.x, 1.13, ChosenHole.z), 0.2).set_trans(Tween.TRANS_ELASTIC)
	Down = false
	OutTimer.start(OutTooLongTime * 0.25)


func _on_out_timer_timeout() -> void:
	if DangerTimer >= OutTooLongTime * 0.75 and CurrentGameState == GameState.Playing:
		if not Down:
			WarningIndicator.show()
		print(str(DangerTimer) + " : " + str(OutTooLongTime))
		emit_signal("OutTooLong", position)
	else:
		WarningIndicator.hide()
		DangerTimer += OutTooLongTime * 0.25


## Height the mole drops to before sliding sideways - well clear of the machine
## so it no longer scrapes through the cabinet between holes.
const DUCK_Y := 0.5
const REST_Y := 0.85


func PopDown() -> void:
	# bailing with the danger meter already past halfway = a last-second dodge
	if not Down and CurrentGameState == GameState.Playing and DangerTimer >= OutTooLongTime * 0.5:
		ClutchDodge.emit()
	WarningIndicator.hide()
	DangerTimer = 0
	var CurrentScale: Vector3 = SCALE

	var velTween := _reset_move_tween()
	# 1) drop straight down out of sight, accelerating
	velTween.tween_property(self, "position:y", DUCK_Y, 0.12).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	# 2) slide across to the new hole and ease back up to the hidden rest height
	velTween.tween_property(self, "position", Vector3(ChosenHole.x, REST_Y, ChosenHole.z), 0.18).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)

	if squishTween and squishTween.is_valid():
		squishTween.kill()
	squishTween = create_tween()
	squishTween.tween_property(self, "scale", Vector3(CurrentScale.x * 0.5, CurrentScale.y * 0.2, CurrentScale.z * 0.5), 0.1).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	squishTween.tween_property(self, "scale", Vector3(CurrentScale.x, CurrentScale.y, CurrentScale.z), 0.1)

	await velTween.finished
	Down = true
	OutTimer.stop()


func GotHit() -> void:
	OutTooLongTime = 1
	ScoreAcceleration = 0
	PopSpeed = 2
	ComboBonus = 1
	Bonk.play(0)
	ScreenShake()
	DangerTimer = 0
	var velTween := _reset_move_tween()
	velTween.tween_property(self, "position", Vector3(position.x, 1, position.z), 0.15).set_trans(Tween.TRANS_ELASTIC)
	MoleMesh.scale = Vector3(1.25, 0.5, 1.25)
	velTween.tween_property(self, "position", Vector3(position.x, 0.85, position.z), 0.15).set_trans(Tween.TRANS_ELASTIC)
	PopOutTimer.start(PopSpeed)
	OutTimer.stop()
	await get_tree().create_timer(0.3).timeout
	MoleMesh.scale = Vector3(1, 1, 1)
	PopDown()


func _on_area_entered(area: Area3D) -> void:
	if area is Mallet and IFrames <= 0:
		IFrames = 60
		GotHit()
		if Lives > 0:
			Lives -= 1
			AnimateHealth()


func ScreenShake() -> void:
	var camShakePosTween := get_tree().create_tween()
	camShakePosTween.tween_property(CameraRef, "position", Vector3(CameraRef.position.x + RNG.randf_range(-0.035, 0.035), CameraRef.position.y + RNG.randf_range(-0.025, 0.025), CameraRef.position.z + RNG.randf_range(-0.035, 0.035)), 0.1).set_trans(Tween.TRANS_ELASTIC)
	camShakePosTween.tween_property(CameraRef, "position", Vector3(CameraRef.position.x, CameraRef.position.y, CameraRef.position.z), 0.1).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

	var camShakeRotTween := get_tree().create_tween()
	camShakeRotTween.tween_property(CameraRef, "rotation", Vector3(CameraRef.rotation.x + RNG.randf_range(-0.035, 0.035), CameraRef.rotation.y + RNG.randf_range(-0.025, 0.025), CameraRef.rotation.z + RNG.randf_range(-0.035, 0.035)), 0.1).set_trans(Tween.TRANS_BOUNCE)
	camShakeRotTween.tween_property(CameraRef, "rotation", Vector3(CameraRef.rotation.x, CameraRef.rotation.y, CameraRef.rotation.z), 0.1).set_trans(Tween.TRANS_BOUNCE).set_ease(Tween.EASE_OUT)


func AnimateHealth() -> void:
	var defaultRot := Vector3.ZERO
	var lifeLostRot := Vector3(360, 0, 0)
	var scoreBoomRot := get_tree().create_tween()
	scoreBoomRot.tween_property(LivesCounter, "rotation", lifeLostRot, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	scoreBoomRot.tween_property(LivesCounter, "rotation", defaultRot, 0.1).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)


func AnimateScore() -> void:
	var defaultScale := Vector3.ONE
	var defaultPos := Vector3(0, 0, 0.01)
	var defaultRot := Vector3.ZERO

	var comboScale := Vector3(1.3, 1.3, 1.3)
	var comboPos := Vector3(0, 0.1, 0.25)
	var comboRot := Vector3(0, randf_range(-0.25, 0.25), randf_range(-0.6, 0.0))
	var scoreBoomPos := get_tree().create_tween()
	scoreBoomPos.tween_property(ScoreCounter, "position", comboPos, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	scoreBoomPos.tween_property(ScoreCounter, "position", defaultPos, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

	var scoreBoomScale := get_tree().create_tween()
	scoreBoomScale.tween_property(ScoreCounter, "scale", comboScale, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	scoreBoomScale.tween_property(ScoreCounter, "scale", defaultScale, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

	var scoreBoomRot := get_tree().create_tween()
	scoreBoomRot.tween_property(ScoreCounter, "rotation", comboRot, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	scoreBoomRot.tween_property(ScoreCounter, "rotation", defaultRot, 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)


func AnimateScoreCombo() -> void:
	ComboBonus += 1
	TotalCollectedCoins += 1
	print("Within Mole Class: " + str(TotalCollectedCoins))
	var defaultScale := Vector3.ONE
	var defaultPos := Vector3(0, -0.171, 0.01)
	var defaultRot := Vector3.ZERO

	if ComboBonus % 5 == 0:
		var comboScale := Vector3(1.3, 1.3, 1.3)
		var comboPos := Vector3(0, 0.1, 0.25)
		var comboRot := Vector3(0, randf_range(-0.25, 0.25), randf_range(-0.6, 0.0))
		var scoreBoomPos := get_tree().create_tween()
		scoreBoomPos.tween_property(ComboCounter, "position", comboPos, 0.5).set_trans(Tween.TRANS_ELASTIC)
		scoreBoomPos.tween_property(ComboCounter, "position", defaultPos, 0.5).set_trans(Tween.TRANS_ELASTIC)

		var scoreBoomScale := get_tree().create_tween()
		scoreBoomScale.tween_property(ComboCounter, "scale", comboScale, 0.5).set_trans(Tween.TRANS_ELASTIC)
		scoreBoomScale.tween_property(ComboCounter, "scale", defaultScale, 0.5).set_trans(Tween.TRANS_ELASTIC)

		var scoreBoomRot := get_tree().create_tween()
		scoreBoomRot.tween_property(ComboCounter, "rotation", comboRot, 0.5).set_trans(Tween.TRANS_ELASTIC)
		scoreBoomRot.tween_property(ComboCounter, "rotation", defaultRot, 0.5).set_trans(Tween.TRANS_ELASTIC)
	else:
		var smallComboScale := Vector3(1.4, 1.4, 1.4)
		var scoreBoomScale := get_tree().create_tween()
		scoreBoomScale.tween_property(ComboCounter, "scale", smallComboScale, 0.1).set_trans(Tween.TRANS_ELASTIC)
		scoreBoomScale.tween_property(ComboCounter, "scale", defaultScale, 0.1).set_trans(Tween.TRANS_ELASTIC)


func GetLives() -> int:
	return Lives


func AddLives(LivesToAdd: int) -> void:
	Lives += LivesToAdd


func GetChosenHole() -> Vector3:
	return ChosenHole


## True if `pos` is (horizontally) the hole the mole is currently in or sliding
## toward. Collectible holes sit at a different Y, so callers must compare X/Z
## only - never the whole vector.
func IsChosenHoleAt(pos: Vector3) -> bool:
	return is_equal_approx(pos.x, ChosenHole.x) and is_equal_approx(pos.z, ChosenHole.z)


func GetDownStatus() -> bool:
	return Down


func GetDangerTimer() -> float:
	return DangerTimer


func GetIFrames() -> float:
	return IFrames


func AddPops() -> void:
	EarlyPops += 1


func GetPops() -> int:
	return EarlyPops


func _on_login_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	var response: Variant = JSON.parse_string(body.get_string_from_utf8())
	await get_tree().create_timer(1.0).timeout
	if responseCode == 200:
		HighScore = int(User.GetHighscore())
		if User.CurrentHat != null:
			add_child(User.CurrentHat)


func GetHighScore() -> int:
	return HighScore


func SetHighScore(_highScore: int) -> void:
	HighScore = _highScore


func GetHighestCombo() -> int:
	return HighestCombo

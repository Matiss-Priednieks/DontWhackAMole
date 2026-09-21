extends Area3D
class_name Mole

var User: Node # /root/LoggedInUser autoload
var SaveManagerRef: Node # /root/SaveManager autoload

signal OutTooLong(Position: Vector3)
## Fired when the mole bails out of a hole with the mallet already bearing down -
## a last-second dodge. Game.gd turns this into the hitstop + camera snap.
signal ClutchDodge
## Fired on entering / leaving the "on fire" streak (no hit for a while).
signal OnFireChanged(active: bool)

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

## The score counter is reparented under this in _ready so it can be rattled /
## pulsed every frame without fighting AnimateScore()'s pickup punch.
var _scoreJitter: Node3D
var _scoreTime: float = 0.0
var _scoreGold: float = 0.0 # 0 = normal red, 1 = shining-hole gold, smoothed

## "On fire" streak: no hit for ON_FIRE_AFTER seconds while playing. Broken by
## GotHit() / Restart() / leaving play. `OnFireMesh` is the flame sprite sheet
## on the score counter (rides its jitter, `%OnFireMesh` in MainGame.tscn) -
## shown/hidden with the state. `OnFireChanged` is for anything fancier.
const ON_FIRE_AFTER := 12.0
var OnFire: bool = false
var _timeSinceHit: float = 0.0
var OnFireMesh: MeshInstance3D

## Once lit, fire has to be "maintained" by ducking late (same cutoff PopDown()
## already uses for ClutchDodge) rather than bailing the instant you pop out.
## FireMeter starts full on ignite, drains on an early/safe duck (scaled by how
## early), regens a little on a late one, and hits 0 -> fire goes out on its own.
const FIRE_METER_MAX := 50.0
const FIRE_METER_CLUTCH_RATIO := 0.5 # DangerTimer/OutTooLongTime cutoff for "pushed it"
const FIRE_METER_DRAIN_SCALE := 10.0 # points lost for an instant (ratio 0) duck
const FIRE_METER_REGEN := 2.0        # points gained per duck at/after the cutoff
var FireMeter: float = 0.0

## Losing fire mid-run (burnout or a hit) costs extra: _timeSinceHit starts
## back below zero instead of at 0, so ON_FIRE_AFTER effectively becomes
## ON_FIRE_AFTER + FIRE_LOSS_COOLDOWN before it can reignite. Not applied on
## Restart()/leaving play - those aren't a failure, just a reset.
const FIRE_LOSS_COOLDOWN := 6.0
var _fireToast: Label3D
var _fireToastTween: Tween
var _fireToastBasePos: Vector3

## Steam achievements - fire once per run, reset in Restart() / GotHit() as noted.
## All calls no-op unless Steam is actually running (see SteamManager).
const SCORE_ACH := [[10000, "ACH_SCORE_10K"], [50000, "ACH_SCORE_50K"], [100000, "ACH_SCORE_100K"]]
var _scoreAchIdx: int = 0     # how far up SCORE_ACH we've unlocked this run
var _noHit30Done: bool = false
var _shinyAchDone: bool = false


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

	var scoreBlock: Node3D = ScoreCounter.get_parent()
	_scoreJitter = Node3D.new()
	scoreBlock.add_child(_scoreJitter)
	ScoreCounter.reparent(_scoreJitter)

	OnFireMesh = get_node("../%OnFireMesh")
	OnFireMesh.visible = false

	# "You're on Fire!" billboard toast, sits to the right of the score counter.
	# Size / position are rough - tune _fireToastBasePos, font_size, pixel_size.
	_fireToast = Label3D.new()
	_fireToast.text = "You're on Fire!"
	_fireToast.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	_fireToast.no_depth_test = true
	_fireToast.pixel_size = 0.002
	_fireToast.font_size = 48
	_fireToast.outline_size = 0
	_fireToast.modulate = Color(1.0, 0.5, 0.12, 0.0)
	_fireToastBasePos = Vector3(0.55, 0.0, 0.05)
	scoreBlock.add_child(_fireToast)
	_fireToast.position = _fireToastBasePos
	_fireToast.visible = false


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
	if OnFire and CurrentGameState != GameState.Playing:
		_setOnFire(false)
	PowerUpButton.disabled = EarlyPops <= 0
	if Score > HighScore:
		HighScore = int(Score)
		User.SetHighscore(HighScore)
	if ComboBonus > HighestCombo:
		HighestCombo = ComboBonus

	while _scoreAchIdx < SCORE_ACH.size() and Score >= SCORE_ACH[_scoreAchIdx][0]:
		SteamManager.Unlock(SCORE_ACH[_scoreAchIdx][1])
		_scoreAchIdx += 1

	_set_mesh_text(LivesCounter, str(Lives))
	_set_mesh_text(EarlyPopCounter, str(EarlyPops) + "/1")
	_set_mesh_text(ScoreCounter, str(roundi(Score)))
	_set_mesh_text(ComboCounter, "x" + str(ComboBonus))
	_set_mesh_text(HighScoreMesh, "Highscore: " + str(HighScore))
	_set_mesh_text(HighestComboMesh, "x" + str(HighestCombo))

	_updateCounters(delta)


## Combo counter shifts red -> gold -> white-hot with the multiplier. The score
## counter rattles / pulses / brightens the faster points are pouring in, and goes
## gold and shakes harder while you're popped up in the shining hole.
func _updateCounters(delta: float) -> void:
	var comboT := clampf(float(ComboBonus - 1) / 15.0, 0.0, 1.0)
	_comboMat.albedo_color = Color(1.0, lerpf(0.0, 0.85, comboT), lerpf(0.02, 0.7, comboT))
	_comboMat.emission = Color(1.0, lerpf(0.0, 0.8, comboT), lerpf(0.0, 0.6, comboT))
	_comboMat.emission_energy_multiplier = lerpf(_LED_ENERGY, 7.0, comboT)

	_scoreTime += delta
	var scoring := not Down and CurrentGameState == GameState.Playing
	var onGold := scoring and ChosenHole == _goldenHole

	# 0 while safe, ramps toward ~1 with how fast the score is climbing, +0.9 on gold
	var intensity := 0.0
	if scoring:
		intensity = clampf(ScoreAcceleration / 1.5, 0.15, 2.0)
		if onGold:
			intensity += 0.9

	var amp := 0.0045 * intensity
	_scoreJitter.position = Vector3(randf_range(-amp, amp), randf_range(-amp, amp), 0.0)
	_scoreJitter.rotation.z = randf_range(-0.06, 0.06) * intensity
	var pulse := 1.0 + sin(_scoreTime * 42.0) * 0.07 * intensity
	_scoreJitter.scale = Vector3(pulse, pulse, 1.0)

	_scoreGold = move_toward(_scoreGold, 1.0 if onGold else 0.0, delta * 6.0)
	_scoreMat.emission = Color(1.0, lerpf(0.0, 0.72, _scoreGold), lerpf(0.0, 0.16, _scoreGold))
	_scoreMat.albedo_color = Color(1.0, lerpf(0.0, 0.78, _scoreGold), lerpf(0.016, 0.22, _scoreGold))
	var energyTarget := _LED_ENERGY
	if scoring:
		energyTarget = lerpf(_LED_ENERGY + 1.5, 11.0, clampf(intensity, 0.0, 1.0)) + _scoreGold * 3.0
	_scoreMat.emission_energy_multiplier = move_toward(_scoreMat.emission_energy_multiplier, energyTarget, delta * 16.0)


func IsOnFire() -> bool:
	return OnFire


func GetFireMeter() -> float:
	return FireMeter


func _setOnFire(active: bool, add_cooldown: bool = false) -> void:
	if OnFire == active:
		return
	OnFire = active
	if active:
		FireMeter = FIRE_METER_MAX
	elif add_cooldown:
		_timeSinceHit = -FIRE_LOSS_COOLDOWN
	OnFireMesh.visible = active
	OnFireChanged.emit(active)
	if active:
		_showFireToast()
		SteamManager.Unlock("ACH_ON_FIRE")


func _updateFireMeter(dangerRatio: float) -> void:
	if dangerRatio >= FIRE_METER_CLUTCH_RATIO:
		FireMeter = minf(FireMeter + FIRE_METER_REGEN, FIRE_METER_MAX)
	else:
		FireMeter -= (1.0 - dangerRatio) * FIRE_METER_DRAIN_SCALE
		if FireMeter <= 0.0:
			_setOnFire(false, true)


func _showFireToast() -> void:
	if _fireToastTween and _fireToastTween.is_valid():
		_fireToastTween.kill()
	_fireToast.position = _fireToastBasePos
	_fireToast.modulate.a = 0.0
	_fireToast.visible = true
	_fireToastTween = create_tween()
	_fireToastTween.tween_property(_fireToast, "modulate:a", 1.0, 0.15)
	_fireToastTween.parallel().tween_property(_fireToast, "position:y", _fireToastBasePos.y + 0.1, 1.5).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_OUT)
	_fireToastTween.tween_interval(0.9)
	_fireToastTween.tween_property(_fireToast, "modulate:a", 0.0, 0.45)
	_fireToastTween.tween_callback(func() -> void: _fireToast.visible = false)


func _physics_process(delta: float) -> void:
	match CurrentGameState:
		GameState.Playing:
			CoinsAdded = false
			ProcessInput()
			UpdateScore()
			CheckGameOver()
			if IFrames <= 60 and IFrames > 0:
				IFrames -= 1
			_timeSinceHit += delta
			if not OnFire and _timeSinceHit >= ON_FIRE_AFTER:
				_setOnFire(true)
			if not _noHit30Done and _timeSinceHit >= 30.0:
				_noHit30Done = true
				SteamManager.Unlock("ACH_UNTOUCHABLE")
		GameState.Paused:
			pass
		GameState.GameOver:
			if not CoinsAdded:
				CoinsAdded = true
				print("Coin update called")
				print(TotalCollectedCoins)
				SteamManager.AddStat("coins_total", TotalCollectedCoins)
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


## Position of the current shining hole (set by Game.gd); INF when there isn't one.
var _goldenHole: Vector3 = Vector3.INF
const GOLDEN_MULT := 2.0


func SetGoldenHole(index: int) -> void:
	_goldenHole = Holes[index] if index >= 0 else Vector3.INF


func UpdateScore() -> void:
	if not Down:
		var onGold := ChosenHole == _goldenHole
		var mult := GOLDEN_MULT if onGold else 1.0
		ScoreAcceleration += 0.005 * ComboBonus * mult
		score += ScoreAcceleration
		if onGold and not _shinyAchDone:
			_shinyAchDone = true
			SteamManager.Unlock("ACH_SHINY")


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
	_timeSinceHit = 0.0
	_noHit30Done = false
	_shinyAchDone = false
	_scoreAchIdx = 0
	_setOnFire(false)
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
	var dangerRatio := DangerTimer / OutTooLongTime if OutTooLongTime > 0.0 else 0.0
	var isRealDodge := not Down and CurrentGameState == GameState.Playing
	# bailing with the danger meter already past halfway = a last-second dodge
	if isRealDodge and dangerRatio >= FIRE_METER_CLUTCH_RATIO:
		ClutchDodge.emit()
	if OnFire and isRealDodge:
		_updateFireMeter(dangerRatio)
	# Flip state immediately (mirrors PopOut()'s Down = false) - if this duck
	# gets interrupted by another key press, _reset_move_tween() kills our
	# tween below and its `await ... finished` never fires, so Down must not
	# depend on that await or spamming input leaves Down stuck false forever
	# (mole reads as permanently "out" - score keeps climbing while hidden).
	Down = true
	OutTimer.stop()
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


func GotHit() -> void:
	_timeSinceHit = 0.0
	_noHit30Done = false
	_setOnFire(false, true)
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
	if ComboBonus == 25:
		SteamManager.Unlock("ACH_COMBO_25")
	elif ComboBonus == 50:
		SteamManager.Unlock("ACH_COMBO_50")
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


func GetHighScore() -> int:
	return HighScore


func SetHighScore(_highScore: int) -> void:
	HighScore = _highScore


func GetHighestCombo() -> int:
	return HighestCombo

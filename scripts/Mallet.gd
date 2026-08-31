extends Area3D
class_name Mallet

var Player: Mole
var Holes: Array[Vector3]
var HasHit: bool
var MoleOutTooLong: bool
var StartPosition: Vector3 = Vector3.ZERO
var HoleDictionary: Dictionary
var NextHit: Vector3
var MalletCollider: CollisionShape3D
var HoleIndex: int
var PopOutTimer: Timer
var CameraRef: Camera3D
var RNG: RandomNumberGenerator
var Miss: AudioStreamPlayer3D
var AboutToHit: AudioStreamPlayer
var MoleHit: bool = false
var MalletSlowSpeed: float = 0.3
var MalletFastSpeed: float = 0.2

var LowLifeAmplifier: float = 0.0


func _ready() -> void:
	Miss = get_node("%Miss")
	AboutToHit = get_node("%AboutToHit")
	RNG = RandomNumberGenerator.new()
	CameraRef = get_node("%Camera3D")
	MalletCollider = get_node("%MalletCollider")
	Player = get_node("%Mole")
	PopOutTimer = get_node("%PopOutTimer")
	StartPosition = Vector3(-0.8, 1.17, -11.688)
	Holes = [
		Vector3(0, 1.142, -11.848),       # top(W)
		Vector3(-0.24, 1.142, -11.638),   # left (A)
		Vector3(0, 1.142, -11.428),       # bottom (S)
		Vector3(0.24, 1.142, -11.638),    # right (D)
	]
	HoleDictionary = {
		0: "%TopBulb",
		1: "%LeftBulb",
		2: "%BottomBulb",
		3: "%RightBulb",
	}
	HoleIndex = randi_range(0, 3)
	NextHit = Holes[HoleIndex]


func _process(delta: float) -> void:
	if Player.CurrentGameState == Mole.GameState.Playing:
		if PopOutTimer.time_left < 0.75:
			(get_node(str(HoleDictionary[HoleIndex])) as Hole).Flash(true)
			if not AboutToHit.playing:
				AboutToHit.play(0)
		else:
			(get_node(str(HoleDictionary[HoleIndex])) as Hole).Flash(false)
			AboutToHit.stop()
	else:
		if AboutToHit.playing:
			AboutToHit.stop()


func _on_pop_out_timer_timeout() -> void:
	if Player.CurrentGameState == Mole.GameState.Playing:
		MoveMallet(NextHit)

		Hit()

		await get_tree().create_timer(MalletSlowSpeed).timeout
		MoveMallet(StartPosition)
		HoleIndex = randi_range(0, 3)
		NextHit = Holes[HoleIndex]


func _on_mole_out_too_long(playerPosition: Vector3) -> void:
	if Player.CurrentGameState == Mole.GameState.Playing:
		MoleOutTooLong = true
		MoveMallet(playerPosition)
		if not HasHit:
			if Player.GetDownStatus() == false:
				Hit()
				MoleOutTooLong = false
			await get_tree().create_timer(MalletSlowSpeed).timeout
			MoveMallet(StartPosition)
		HasHit = false


func _on_area_entered(area: Area3D) -> void:
	Miss.play()
	MoleHit = false


func MoveMallet(toLocation: Vector3) -> void:
	MalletCollider.disabled = true
	# node-bound tween (not get_tree()) so it freezes with the node during a Hitstop
	var velTween := create_tween()
	velTween.tween_property(self, "position", Vector3(toLocation.x - 0.35, position.y, toLocation.z), MalletFastSpeed).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_IN_OUT)
	await get_tree().create_timer(MalletFastSpeed).timeout
	MalletCollider.disabled = false


func Hit() -> void:
	await get_tree().create_timer(0.1).timeout
	ScreenShake()
	var velTween := create_tween()
	velTween.tween_property(self, "rotation", Vector3(rotation.x, rotation.y, rotation.z - 1.4), MalletFastSpeed).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	velTween.tween_property(self, "rotation", Vector3(0, 0, 0), MalletSlowSpeed).set_trans(Tween.TRANS_SINE).set_trans(Tween.TRANS_BACK)
	HasHit = true


## Freeze-frame the mallet (and its swing) for a beat without touching global
## time - the mole and camera keep running at full speed. This is the "slow-mo"
## on a clutch dodge: the threat hangs while you slide clear.
func Hitstop(seconds: float) -> void:
	if not is_processing():
		return
	process_mode = Node.PROCESS_MODE_DISABLED
	await get_tree().create_timer(seconds).timeout
	process_mode = Node.PROCESS_MODE_INHERIT


func ScreenShake() -> void:
	# shake harder the closer the player is to losing: 3 lives -> 1x, 1 life -> 3x
	LowLifeAmplifier = clampf(4.0 - Player.GetLives(), 1.0, 3.0)

	var camShakePosTween := get_tree().create_tween()
	camShakePosTween.tween_property(CameraRef, "position", Vector3(CameraRef.position.x + RNG.randf_range(-0.010, 0.010), CameraRef.position.y + RNG.randf_range(-0.001, 0.001), CameraRef.position.z + RNG.randf_range(-0.010, 0.010)), 0.1).set_trans(Tween.TRANS_ELASTIC)
	camShakePosTween.tween_property(CameraRef, "position", Vector3(CameraRef.position.x, CameraRef.position.y, CameraRef.position.z), 0.1 * LowLifeAmplifier).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

	var camShakeRotTween := get_tree().create_tween()
	camShakeRotTween.tween_property(CameraRef, "rotation", Vector3(CameraRef.rotation.x + RNG.randf_range(-0.010, 0.010), CameraRef.rotation.y + RNG.randf_range(-0.001, 0.001), CameraRef.rotation.z + RNG.randf_range(-0.010, 0.010)), 0.1).set_trans(Tween.TRANS_BOUNCE)
	camShakeRotTween.tween_property(CameraRef, "rotation", Vector3(CameraRef.rotation.x, CameraRef.rotation.y, CameraRef.rotation.z), 0.1 * LowLifeAmplifier).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)

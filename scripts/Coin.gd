extends Area3D
class_name Coin

var Holes: Array[Vector3]
var HoleDictionary: Dictionary
var HoleIndex: int
var NextLocation: Vector3
var DownPosition: Vector3
var UpPosition: Vector3
var Down: bool
var Out: bool
var Finished: bool
var IsCollected: bool
var CoinCollectionBonus: float = 1000

const MALLETSPEEDCHANGE: float = 0.0025
const POPOUTSPEEDCHANGE: float = 0.025
var YPos: float

var ComboCounter: MeshInstance3D
var CoinMesh: Node3D
var CoinBreakParticle: GPUParticles3D
var CoinCollider: CollisionShape3D
var Shatter: AudioStreamPlayer3D
var Collected: AudioStreamPlayer3D
var RNG: RandomNumberGenerator
var RandomChosenHole: Vector3

var MoleRef: Mole
var MalletRef: Mallet


func _ready() -> void:
	RNG = RandomNumberGenerator.new()

	Collected = get_node("%Collected")
	Shatter = get_node("%Shatter")
	Holes = [
		Vector3(0, 1.142, -11.848),       # top(W)
		Vector3(-0.24, 1.142, -11.638),   # left (A)
		Vector3(0, 1.142, -11.428),       # bottom (S)
		Vector3(0.24, 1.142, -11.638),    # right (D)
	]
	MoleRef = get_node("../Mole")
	MalletRef = get_node("../Mallet")
	ComboCounter = get_node("../NewWhackMachine/ComboBonus/ComboText/ComboCounter")
	CoinMesh = get_node("%CoinMesh")
	CoinBreakParticle = get_node("%CoinBreakParticle")
	CoinCollider = get_node("%CoinCollider")

	ChangeRandomHole()
	NextLocation = RandomChosenHole
	# PopOut()


func _physics_process(delta: float) -> void:
	if not Finished and MoleRef.CurrentGameState == Mole.GameState.Playing:
		PopOut()
		Finished = true
	position = Vector3(position.x, YPos, position.z)
	rotation_degrees.y += 180.0 * delta


func PopOut() -> void:
	position = NextLocation
	UpPosition = Vector3(position.x, 1.25, position.z)
	var tempRotation := Vector3(randf_range(-10, 10), randf_range(-10, 10), randf_range(-10, 10))

	YPos = position.y
	var tempTween := create_tween()
	tempTween.parallel().tween_property(self, "YPos", UpPosition.y, 1.25).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	tempTween.parallel().tween_property(self, "rotation", tempRotation, 2.15).set_trans(Tween.TRANS_CIRC).set_ease(Tween.EASE_OUT_IN)
	await get_tree().create_timer(0.3).timeout
	Out = true

	# area_entered only fires on the enter transition. If the mole was already
	# sitting on this spot when the coin appeared (before Out was true), that
	# signal was ignored and won't fire again - so collect it now.
	for a in get_overlapping_areas():
		_on_area_entered(a)


func _on_despawn_timer_timeout() -> void:
	if not IsCollected:
		call_deferred("DisableCollisions")
		CoinMesh.hide()
		CoinBreakParticle.emitting = true
		Shatter.play(0)
		await get_tree().create_timer(0.5).timeout
		call_deferred("queue_free")


func _on_area_entered(area: Area3D) -> void:
	if area is Mole and Out and not IsCollected:
		var mole := area as Mole
		if mole.PopSpeed > 0.75:
			mole.PopSpeed -= POPOUTSPEEDCHANGE
			mole.Score += CoinCollectionBonus
		if MalletRef.MalletFastSpeed >= 0.1:
			MalletRef.MalletFastSpeed -= MALLETSPEEDCHANGE
		if MalletRef.MalletSlowSpeed >= 0.2:
			MalletRef.MalletSlowSpeed -= MALLETSPEEDCHANGE
		if mole.OutTooLongTime > 0.5:
			mole.OutTooLongTime -= POPOUTSPEEDCHANGE
		Collected.play(0)
		IsCollected = true
		call_deferred("DisableCollisions")
		var tempTween := create_tween()
		tempTween.parallel().tween_property(self, "position", Vector3(ComboCounter.global_position.x + 0.05, ComboCounter.global_position.y, ComboCounter.global_position.z + 0.05), 0.2).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
		tempTween.parallel().tween_property(self, "scale", Vector3(0.01, 0.01, 0.01), 0.5).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
		mole.AnimateScoreCombo()
		mole.AnimateScore()
		await get_tree().create_timer(0.5).timeout
		CoinMesh.hide()
		call_deferred("queue_free")


func DisableCollisions() -> void:
	CoinCollider.disabled = true


func ChangeRandomHole() -> Vector3:
	var randomHoleIndex := RNG.randi_range(0, Holes.size() - 1)
	RandomChosenHole = Holes[randomHoleIndex]
	while MoleRef.IsChosenHoleAt(RandomChosenHole):
		randomHoleIndex = RNG.randi_range(0, Holes.size() - 1)
		RandomChosenHole = Holes[randomHoleIndex]
	return RandomChosenHole

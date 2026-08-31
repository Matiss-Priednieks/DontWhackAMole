extends Area3D
class_name HeartContainer

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

const RNG_RANGE: int = 10
var YPos: float

var ComboCounter: MeshInstance3D
var HeartMesh: Node3D
var HeartBreakParticle: GPUParticles3D
var HeartCollider: CollisionShape3D
var Shatter: AudioStreamPlayer3D
var Collected: AudioStreamPlayer3D

var MoleRef: Mole
var MalletRef: Mallet


func _ready() -> void:
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
	HeartMesh = get_node("%HeartMesh")
	HeartBreakParticle = get_node("%HeartBreakParticle")
	HeartCollider = get_node("%HeartCollider")

	HoleIndex = randi_range(0, 3)
	NextLocation = Holes[HoleIndex]
	while MoleRef.IsChosenHoleAt(NextLocation):
		HoleIndex = randi_range(0, 3)
		NextLocation = Holes[HoleIndex]
	# PopOut()


func _physics_process(delta: float) -> void:
	if not Finished and MoleRef.CurrentGameState == Mole.GameState.Playing:
		PopOut()
		Finished = true
	position = Vector3(position.x, YPos, position.z)


func PopOut() -> void:
	position = NextLocation
	UpPosition = Vector3(position.x, 1.25, position.z)
	var tempRotation := Vector3(randi_range(-RNG_RANGE, RNG_RANGE), randi_range(-RNG_RANGE, RNG_RANGE), randi_range(-RNG_RANGE, RNG_RANGE))

	YPos = position.y
	var tempTween := create_tween()
	tempTween.parallel().tween_property(self, "YPos", UpPosition.y, 1.25).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
	tempTween.parallel().tween_property(self, "rotation", tempRotation, 2.15).set_trans(Tween.TRANS_CIRC).set_ease(Tween.EASE_OUT)

	await get_tree().create_timer(0.3).timeout
	Out = true

	# area_entered only fires on the enter transition; if the mole was already
	# overlapping this spot before Out became true, collect it now.
	for a in get_overlapping_areas():
		_on_area_entered(a)


func _on_despawn_timer_timeout() -> void:
	if not IsCollected:
		call_deferred("DisableCollisions")
		HeartMesh.hide()
		HeartBreakParticle.emitting = true
		Shatter.play(0)
		await get_tree().create_timer(0.5).timeout
		call_deferred("queue_free")


func _on_area_entered(area: Area3D) -> void:
	if area is Mole and Out and not IsCollected:
		var mole := area as Mole
		if mole.GetLives() < 3:
			mole.AddLives(1)
		Collected.play(0)
		IsCollected = true
		call_deferred("DisableCollisions")
		var tempTween := create_tween()
		tempTween.parallel().tween_property(self, "position", Vector3(ComboCounter.global_position.x + 0.05, ComboCounter.global_position.y, ComboCounter.global_position.z + 0.05), 0.2).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
		tempTween.parallel().tween_property(self, "scale", Vector3(0.01, 0.01, 0.01), 0.5).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)
		mole.AnimateScoreCombo()
		await get_tree().create_timer(0.5).timeout
		HeartMesh.hide()
		call_deferred("queue_free")


func DisableCollisions() -> void:
	HeartCollider.disabled = true

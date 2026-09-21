extends MeshInstance3D
class_name SelectedHoleHighlighter

var MoleRef: Mole


func _ready() -> void:
	MoleRef = get_node("%Mole")


func _process(delta: float) -> void:
	var chosen: Vector3 = MoleRef.GetChosenHole()
	position = Vector3(chosen.x, 1.22, chosen.z)

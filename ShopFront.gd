extends Node3D
class_name ShopFront

var User: Node
var StartPos: Vector3


func _ready() -> void:
	StartPos = Vector3(-2, 0.5, 0)
	User = get_node("/root/LoggedInUser")
	for key in User.UnlockablesArray:
		var contentInstance: Node3D = key.ContentScene.instantiate()
		contentInstance.position = StartPos
		contentInstance.scale = Vector3(3, 3, 3)
		add_child(contentInstance)
		StartPos.x += 1

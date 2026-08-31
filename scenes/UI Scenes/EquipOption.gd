extends PanelContainer
class_name EquipOption

var EquipID: int = 0
var User: Node


func _ready() -> void:
	User = get_node("/root/LoggedInUser")


func _on_equip_pressed() -> void:
	print("Buy request 1")
	User.EquipHat(EquipID)
	call_deferred("queue_free")


func _on_cancel_pressed() -> void:
	call_deferred("queue_free")

extends PanelContainer
class_name UnequipOption

var User: Node


func _ready() -> void:
	User = get_node("/root/LoggedInUser")


func _on_unequip_pressed() -> void:
	print("Buy request 1")
	User.UnequipHat()
	call_deferred("queue_free")


func _on_cancel_pressed() -> void:
	call_deferred("queue_free")

extends PanelContainer
class_name BuyOption

var BuyID: int = 0
var shopMenuRef: ShopMenu


func _ready() -> void:
	shopMenuRef = get_parent() as ShopMenu


func _on_buy_pressed() -> void:
	print("Buy request 1")
	shopMenuRef.ConfirmBuyItem(BuyID)
	call_deferred("queue_free")


func _on_cancel_pressed() -> void:
	call_deferred("queue_free")

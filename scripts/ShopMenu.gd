extends PanelContainer
class_name ShopMenu

var user: Node
var itemListNode: ItemList
var BuyItem: PackedScene
var EquipItem: PackedScene
var UnequipItem: PackedScene
var UserCoins: Label


func _ready() -> void:
	BuyItem = ResourceLoader.load("res://scenes/UI Scenes/BuyOption.tscn")
	EquipItem = ResourceLoader.load("res://scenes/UI Scenes/EquipOption.tscn")
	UnequipItem = ResourceLoader.load("res://scenes/UI Scenes/UnequipOption.tscn")
	user = get_node("/root/LoggedInUser")
	itemListNode = get_node("ShopMenuContainer/VBoxContainer/ItemList")
	UserCoins = get_node("ShopMenuContainer/VBoxContainer/UserCoins")
	InitialiseItemList()
	UpdateCoinLabel()


func InitialiseItemList() -> void:
	for i in user.UnlockablesArray.size():
		itemListNode.add_item(user.UnlockablesArray[i].ContentName, user.UnlockablesArray[i].ContentIcon, true)


func UpdateCoinLabel() -> void:
	UserCoins.text = "Coins:  " + str(user.AccountCurrency)


func ConfirmBuyItem(contentID: int) -> void:
	user.CheckUnlockedContent(contentID)


func _on_item_list_item_clicked(index: int, position: Vector2, mb_index: int) -> void:
	if user.UnlockablesArray[index].IsUnlocked == false:
		var buyItemInst := BuyItem.instantiate() as BuyOption
		buyItemInst.BuyID = user.UnlockablesArray[index].ContentID
		(buyItemInst.get_node("VBoxContainer/MarginContainer/TextureRect") as TextureRect).texture = user.UnlockablesArray[index].ContentIcon
		(buyItemInst.get_node("VBoxContainer/Label") as Label).text = "Buy " + user.UnlockablesArray[index].ContentName + " for " + str(user.UnlockablesArray[index].ContentPrice) + "?"
		add_child(buyItemInst)
	elif user.UnlockablesArray[index].IsUnlocked == true and user.EquippedHatIndex != index:
		var equipItemInst := EquipItem.instantiate() as EquipOption
		equipItemInst.EquipID = user.UnlockablesArray[index].ContentID
		(equipItemInst.get_node("VBoxContainer/MarginContainer/TextureRect") as TextureRect).texture = user.UnlockablesArray[index].ContentIcon
		(equipItemInst.get_node("VBoxContainer/Label") as Label).text = "Equip " + user.UnlockablesArray[index].ContentName + "?"
		add_child(equipItemInst)
	elif user.UnlockablesArray[index].IsUnlocked == true and user.EquippedHatIndex == index:
		var unequipItemInst := UnequipItem.instantiate() as UnequipOption
		(unequipItemInst.get_node("VBoxContainer/MarginContainer/TextureRect") as TextureRect).texture = user.UnlockablesArray[index].ContentIcon
		(unequipItemInst.get_node("VBoxContainer/Label") as Label).text = "Unequip " + user.UnlockablesArray[index].ContentName + "?"
		add_child(unequipItemInst)
	UpdateCoinLabel()


func _on_check_account_timeout() -> void:
	user.UpdateShopUI()
	UpdateCoinLabel()

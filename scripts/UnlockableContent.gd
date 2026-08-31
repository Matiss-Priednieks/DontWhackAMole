class_name UnlockableContent
extends RefCounted
## Plain data holder for a purchasable/unlockable cosmetic. Was a C# `Node`
## subclass but is never added to the tree, so RefCounted is the right base.

var ContentID: int
var ContentName: String
var Description: String
var IsUnlocked: bool
var ContentIcon: Texture2D
var ContentScene: PackedScene
var ContentPrice: int


func _init(contentId: int, name: String, description: String, isUnlocked: bool, contentIcon: Texture2D, contentScene: PackedScene, contentPrice: int) -> void:
	ContentID = contentId
	ContentName = name
	Description = description
	IsUnlocked = isUnlocked
	ContentIcon = contentIcon
	ContentScene = contentScene
	ContentPrice = contentPrice

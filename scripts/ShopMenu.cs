using Godot;
using System;

public partial class ShopMenu : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	LoggedInUser user;
	ItemList itemListNode;
	PackedScene BuyItem;
	public override void _Ready()
	{
		BuyItem = ResourceLoader.Load<PackedScene>("res://scenes/UI Scenes/BuyOption.tscn");
		user = GetNode<LoggedInUser>("/root/LoggedInUser");
		itemListNode = GetNode<ItemList>("ShopMenuContainer/VBoxContainer/ItemList");
		for (int i = 0; i < user.unlockables.Length; i++)
		{
			itemListNode.AddItem(user.unlockables[i].ContentName, user.unlockables[i].ContentIcon, true);
			itemListNode.SetItemDisabled(i, user.unlockables[i].IsUnlocked);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public void _on_item_list_item_clicked(int index, Vector2 position, int mb_index)
	{
		GD.Print(index, position, mb_index);

		if (user.unlockables[index].IsUnlocked == false)
		{
			GD.Print(user.unlockables[index].ContentName);
			var buyItemInst = BuyItem.Instantiate<BuyOption>();
			buyItemInst.BuyID = user.unlockables[index].ContentID;
			buyItemInst.GetNode<TextureRect>("VBoxContainer/MarginContainer/TextureRect").Texture = user.unlockables[index].ContentIcon;
			buyItemInst.GetNode<Label>("VBoxContainer/Label").Text = "Buy " + user.unlockables[index].ContentName + " for " + user.unlockables[index].ContentPrice + "?";
			AddChild(buyItemInst);
		}
	}

	public void ConfirmBuyItem(int contentID)
	{
		GD.Print("Buy request 2");
		user.CheckItems(contentID);
	}
}

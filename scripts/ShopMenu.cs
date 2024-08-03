using Godot;
using System;

public partial class ShopMenu : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	LoggedInUser user;
	ItemList itemListNode;
	public override void _Ready()
	{
		user = GetNode<LoggedInUser>("/root/LoggedInUser");
		itemListNode = GetNode<ItemList>("ShopMenuContainer/VBoxContainer/ItemList");
		for (int i = 0; i < user.unlockables.Length; i++)
		{
			itemListNode.AddItem(user.unlockables[i].ContentName, user.unlockables[i].ContentIcon, true);
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
		}
	}
}

using Godot;
using System;

public partial class ShopMenu : PanelContainer
{
	LoggedInUser user;
	ItemList itemListNode;
	PackedScene BuyItem, EquipItem;
	Label UserCoins;
	public override void _Ready()
	{
		BuyItem = ResourceLoader.Load<PackedScene>("res://scenes/UI Scenes/BuyOption.tscn");
		EquipItem = ResourceLoader.Load<PackedScene>("res://scenes/UI Scenes/EquipOption.tscn");
		user = GetNode<LoggedInUser>("/root/LoggedInUser");
		itemListNode = GetNode<ItemList>("ShopMenuContainer/VBoxContainer/ItemList");
		UserCoins = GetNode<Label>("ShopMenuContainer/VBoxContainer/UserCoins");
		InitialiseItemList();
		UpdateCoinLabel();
	}

	private void InitialiseItemList()
	{
		for (int i = 0; i < user.UnlockablesArray.Length; i++)
		{
			itemListNode.AddItem(user.UnlockablesArray[i].ContentName, user.UnlockablesArray[i].ContentIcon, true);
		}
		UpdateDisabledState();
	}

	private void UpdateDisabledState()
	{
		// for (int i = 0; i < itemListNode.ItemCount; i++)
		// {
		// 	itemListNode.SetItemDisabled(i, user.UnlockablesArray[i].IsUnlocked);
		// }
	}
	private void UpdateCoinLabel()
	{
		UserCoins.Text = "Coins:  " + user.AccountCurrency;
	}

	public void ConfirmBuyItem(int contentID)
	{
		GD.Print("Buy request 2");
		user.CheckUnlockedContent(contentID);
	}
	public void _on_item_list_item_clicked(int index, Vector2 position, int mb_index)
	{
		if (user.UnlockablesArray[index].IsUnlocked == false)
		{
			GD.Print(user.UnlockablesArray[index].ContentName);
			var buyItemInst = BuyItem.Instantiate<BuyOption>();
			buyItemInst.BuyID = user.UnlockablesArray[index].ContentID;
			buyItemInst.GetNode<TextureRect>("VBoxContainer/MarginContainer/TextureRect").Texture = user.UnlockablesArray[index].ContentIcon;
			buyItemInst.GetNode<Label>("VBoxContainer/Label").Text = "Buy " + user.UnlockablesArray[index].ContentName + " for " + user.UnlockablesArray[index].ContentPrice + "?";
			AddChild(buyItemInst);
		}
		else
		{
			GD.Print(user.UnlockablesArray[index].ContentName);
			var equipItemInst = EquipItem.Instantiate<EquipOption>();
			equipItemInst.EquipID = user.UnlockablesArray[index].ContentID;
			equipItemInst.GetNode<TextureRect>("VBoxContainer/MarginContainer/TextureRect").Texture = user.UnlockablesArray[index].ContentIcon;
			equipItemInst.GetNode<Label>("VBoxContainer/Label").Text = "Equip " + user.UnlockablesArray[index].ContentName + "?";
			AddChild(equipItemInst);
		}
		UpdateDisabledState();
		UpdateCoinLabel();
	}

	public void _on_check_account_timeout()
	{
		user.UpdateShopUI();
		UpdateDisabledState();
		UpdateCoinLabel();
	}
}

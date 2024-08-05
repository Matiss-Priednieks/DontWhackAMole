using Godot;
using System;

public partial class ShopMenu : PanelContainer
{
	LoggedInUser user;
	ItemList itemListNode;
	PackedScene BuyItem;
	Label UserCoins;
	public override void _Ready()
	{
		BuyItem = ResourceLoader.Load<PackedScene>("res://scenes/UI Scenes/BuyOption.tscn");
		user = GetNode<LoggedInUser>("/root/LoggedInUser");
		itemListNode = GetNode<ItemList>("ShopMenuContainer/VBoxContainer/ItemList");
		UserCoins = GetNode<Label>("ShopMenuContainer/VBoxContainer/UserCoins");
		InitialiseItemList();
		UpdateCoinLabel();
	}

	private void InitialiseItemList()
	{
		for (int i = 0; i < user.unlockables.Length; i++)
		{
			itemListNode.AddItem(user.unlockables[i].ContentName, user.unlockables[i].ContentIcon, true);
		}
		UpdateDisabledState();
	}

	private void UpdateDisabledState()
	{
		for (int i = 0; i < itemListNode.ItemCount; i++)
		{
			itemListNode.SetItemDisabled(i, user.unlockables[i].IsUnlocked);
		}
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
		if (user.unlockables[index].IsUnlocked == false)
		{
			GD.Print(user.unlockables[index].ContentName);
			var buyItemInst = BuyItem.Instantiate<BuyOption>();
			buyItemInst.BuyID = user.unlockables[index].ContentID;
			buyItemInst.GetNode<TextureRect>("VBoxContainer/MarginContainer/TextureRect").Texture = user.unlockables[index].ContentIcon;
			buyItemInst.GetNode<Label>("VBoxContainer/Label").Text = "Buy " + user.unlockables[index].ContentName + " for " + user.unlockables[index].ContentPrice + "?";
			AddChild(buyItemInst);
		}
		UpdateDisabledState();
		UpdateCoinLabel();
	}

	public void _on_check_account_timeout()
	{
		user.UpdateShopUI();
		UpdateDisabledState();
	}
}

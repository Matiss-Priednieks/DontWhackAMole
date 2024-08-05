using Godot;
using System;

public partial class BuyOption : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	public int BuyID = 0;
	ShopMenu shopMenuRef;
	public override void _Ready()
	{
		shopMenuRef = (ShopMenu)GetParent();
	}


	public void _on_buy_pressed()
	{
		GD.Print("Buy request 1");
		shopMenuRef.ConfirmBuyItem(BuyID);
		CallDeferred(MethodName.QueueFree);
	}
	public void _on_cancel_pressed()
	{
		CallDeferred(MethodName.QueueFree);
	}
}

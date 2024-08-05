using Godot;
using System;

public partial class EquipOption : PanelContainer
{
	public int EquipID = 0;
	LoggedInUser User;
	public override void _Ready()
	{
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
	}


	public void _on_equip_pressed()
	{
		GD.Print("Buy request 1");
		User.EquipHat(EquipID);
		CallDeferred(MethodName.QueueFree);
	}
	public void _on_cancel_pressed()
	{
		CallDeferred(MethodName.QueueFree);
	}
}

using Godot;
using System;

public partial class UnequipOption : PanelContainer
{
	LoggedInUser User;
	public override void _Ready()
	{
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
	}

	public void _on_unequip_pressed()
	{
		GD.Print("Buy request 1");
		User.UnequipHat();
		CallDeferred(MethodName.QueueFree);
	}
	public void _on_cancel_pressed()
	{
		CallDeferred(MethodName.QueueFree);
	}
}

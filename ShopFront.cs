using Godot;
using System;

public partial class ShopFront : Node3D
{
	// Called when the node enters the scene tree for the first time.
	LoggedInUser User;
	Vector3 StartPos;
	public override void _Ready()
	{
		StartPos = new(-2, 0.5f, 0);
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		foreach (var key in User.UnlockablesArray)
		{
			Node3D contentInstance = key.ContentScene.Instantiate<Node3D>();
			contentInstance.Position = StartPos;
			contentInstance.Scale = new(3, 3, 3);
			AddChild(contentInstance);
			StartPos.X += 1;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

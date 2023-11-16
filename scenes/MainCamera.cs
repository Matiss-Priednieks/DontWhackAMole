using Godot;
using System;

public partial class MainCamera : Camera3D
{
	// Called when the node enters the scene tree for the first time.
	int Length = 100;
	Vector2 StartPos;
	Vector2 CurPos;
	bool Swiping;

	[Signal] public delegate void LeftSwipeEventHandler(int Hole);
	[Signal] public delegate void RightSwipeEventHandler(int Hole);
	[Signal] public delegate void UpSwipeEventHandler(int Hole);
	[Signal] public delegate void DownSwipeEventHandler(int Hole);

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("press"))
		{
			if (!Swiping)
			{
				Swiping = true;
				StartPos = GetViewport().GetMousePosition();
			}
		}
		if (Input.IsActionPressed("press"))
		{
			if (Swiping)
			{
				CurPos = GetViewport().GetMousePosition();
				if (StartPos.DistanceTo(CurPos) >= Length)
				{
					var Direction = new Vector2((float)(CurPos.X - StartPos.X), CurPos.Y - StartPos.Y);
					var NormalisedDirection = Direction.Normalized();

					if (NormalisedDirection.X > -1 && NormalisedDirection.X < 0 && NormalisedDirection.Y < 0.5f && NormalisedDirection.Y > -0.5f)
					{
						GD.Print("Left Swipe");
						EmitSignal("LeftSwipe", 1);
					}
					else if (NormalisedDirection.X > 0 && NormalisedDirection.X < 1 && NormalisedDirection.Y < 0.5f && NormalisedDirection.Y > -0.5f)
					{
						GD.Print("Right Swipe");
						EmitSignal("RightSwipe", 3);
					}
					else if (NormalisedDirection.Y > -1 && NormalisedDirection.Y < 0 && NormalisedDirection.X < 0.5f && NormalisedDirection.X > -0.5f)
					{
						GD.Print("Up Swipe");
						EmitSignal("UpSwipe", 0);
					}
					else if (NormalisedDirection.Y > 0 && NormalisedDirection.Y < 1 && NormalisedDirection.X < 0.5f && NormalisedDirection.X > -0.5f)
					{
						GD.Print("Down Swipe");
						EmitSignal("DownSwipe", 2);
					}

					GD.Print("Swipe detected");
					Swiping = false;
				}
			}
		}
		else
		{
			Swiping = false;
		}
	}
}

using Godot;
using System;

public partial class UI : CanvasLayer
{
	// Called when the node enters the scene tree for the first time.
	public bool Menu = true;
	public bool Settings = false;
	public bool Playing = false;
	public bool GameOver = false;
	public bool Login = false;
	public bool Register = false;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

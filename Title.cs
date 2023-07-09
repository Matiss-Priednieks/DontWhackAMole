using Godot;
using System;

public partial class Title : Node3D
{
    Vector3 PlayPos, MenuPos;
    public override void _Ready()
    {
        PlayPos = new Vector3(0, 0.181f, 0.026f);
        MenuPos = new Vector3(0, 2.157f, 1.743f);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}

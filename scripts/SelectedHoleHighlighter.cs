using Godot;
using System;

public partial class SelectedHoleHighlighter : MeshInstance3D
{
    // Called when the node enters the scene tree for the first time.
    Mole MoleRef;
    public override void _Ready()
    {
        MoleRef = GetNode<Mole>("%Mole");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var tempPos = Position;
        tempPos = new Vector3(MoleRef.GetChosenHole().X, 0.62f, MoleRef.GetChosenHole().Z);
        Position = tempPos;
    }
}

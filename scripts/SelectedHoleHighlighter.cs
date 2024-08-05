using Godot;
using System;

public partial class SelectedHoleHighlighter : MeshInstance3D
{
    Mole MoleRef;
    public override void _Ready()
    {
        MoleRef = GetNode<Mole>("%Mole");
    }

    public override void _Process(double delta)
    {
        var tempPos = Position;
        tempPos = new Vector3(MoleRef.GetChosenHole().X, 1.22f, MoleRef.GetChosenHole().Z);
        Position = tempPos;
        // GD.Print(Position);
    }
}

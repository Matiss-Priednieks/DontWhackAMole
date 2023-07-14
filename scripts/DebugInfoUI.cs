using Godot;
using System;

public partial class DebugInfoUI : CanvasLayer
{
    // Called when the node enters the scene tree for the first time.
    Mole Player;
    Label FPS, Timer, ChosenHole, DownOrOut, OutTimer, IFrames;
    public override void _Ready()
    {
        Player = GetNode<Mole>("%Mole");
        FPS = GetNode<Label>("%FPS");
        Timer = GetNode<Label>("%PopTimer");
        OutTimer = GetNode<Label>("%DangerTimer");
        ChosenHole = GetNode<Label>("%ChosenHole");
        DownOrOut = GetNode<Label>("%DownOrOut");
        IFrames = GetNode<Label>("%IFrames");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        FPS.Text = "FPS: " + Engine.GetFramesPerSecond().ToString();
        Timer.Text = "Time until pop: " + Math.Round(GetNode<Timer>("%PopOutTimer").TimeLeft).ToString();
        OutTimer.Text = "Time until Danger: " + (1 - Player.GetDangerTimer()).ToString();
        ChosenHole.Text = "Chosen Hole: " + Player.GetChosenHole().ToString();
        DownOrOut.Text = "Down: " + Player.GetDownStatus().ToString();
        IFrames.Text = "IFrames: " + Player.GetIFrames().ToString();

        if (Input.IsActionJustReleased("debug_info"))
        {
            Visible = !Visible;
        }
    }
}

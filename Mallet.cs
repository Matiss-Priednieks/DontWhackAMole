using Godot;
using System;

public partial class Mallet : CharacterBody3D
{
    // Called when the node enters the scene tree for the first time.
    Mole Player;
    Vector3[] Holes;
    bool HasHit, MoleOutTooLong;
    Vector3 StartPosition = Vector3.Zero;
    public override void _Ready()
    {
        StartPosition = new Vector3(-0.8f, 1.325f, -0.115f);
        Holes = new Vector3[]{
            new Vector3(0, 0.85f, 0.35f),
            new Vector3(0.35f, 0.85f, 0),
            new Vector3(0, 0.85f, -0.35f),
            new Vector3(-0.35f, 0.85f, 0)
            };
        Player = GetNode<Mole>("%Mole");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (MoleOutTooLong)
        {
            MoleOutTooLong = !Player.GetDownStatus();
        }
    }
    public async void _on_pop_out_timer_timeout()
    {
        // if (!MoleOutTooLong)
        // {
        var NextHit = Holes[GD.RandRange(0, 3)];
        MoveMallet(NextHit);

        Hit();

        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        MoveMallet(StartPosition);
        // }

    }
    public async void _on_mole_out_too_long(Vector3 playerPosition)
    {
        MoleOutTooLong = true;
        MoveMallet(playerPosition);
        if (!HasHit)
        {
            Hit();
            await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
            MoveMallet(StartPosition);
        }
        HasHit = false;
    }

    public void MoveMallet(Vector3 toLocation)
    {
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(toLocation.X - 0.35f, Position.Y, toLocation.Z), 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.InOut);
    }

    public async void Hit()
    {
        GD.Print("???");
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "rotation", new Vector3(Rotation.X, Rotation.Y, Rotation.Z - 1.25f), 0.2f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        velTween.TweenProperty(this, "rotation", new Vector3(0, 0, 0), 0.3f).SetTrans(Tween.TransitionType.Sine).SetTrans(Tween.TransitionType.Back);
        HasHit = true;
    }
}


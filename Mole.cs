using Godot;
using Godot.Collections;
using System;

public partial class Mole : CharacterBody3D
{
    [Signal] public delegate void OutTooLongEventHandler(Vector3 Position);
    Vector3[] Holes;
    bool Down = true;
    Vector3 ChosenHole;
    Timer PopOutTimer, OutTimer;
    float DangerTimer = 0;
    public override void _Ready()
    {
        Holes = new Vector3[]{
            new Vector3(0, 0.85f, 0.35f), //up (W)
            new Vector3(0.35f, 0.85f, 0), //left (A)
            new Vector3(0, 0.85f, -0.35f), //down (S)
            new Vector3(-0.35f, 0.85f, 0) //right (D))
            };
        ChosenHole = Holes[0];
        Position = Holes[0];

        PopOutTimer = GetNode<Timer>("%PopOutTimer");
        OutTimer = GetNode<Timer>("%OutTimer");
    }

    public override void _PhysicsProcess(double delta)
    {
        // Holes[GD.RandRange(0, Holes.Count)];

        if (Input.IsActionJustPressed("top_hole"))
        {
            ChosenHole = Holes[0];
        }
        if (Input.IsActionJustPressed("left_hole"))
        {
            ChosenHole = Holes[1];
        }
        if (Input.IsActionJustPressed("bottom_hole"))
        {
            ChosenHole = Holes[2];
        }
        if (Input.IsActionJustPressed("right_hole"))
        {
            ChosenHole = Holes[3];
        }

        if (Input.IsActionJustPressed("ui_accept") && !Down)
        {
            PopDown();
        }


    }

    public void _on_pop_out_timer_timeout()
    {
        //Pop Mole out
        if (Down)
        {
            Position = ChosenHole;
            var velocity = Velocity;
            Tween velTween = GetTree().CreateTween();
            velTween.TweenProperty(this, "position", new Vector3(ChosenHole.X, 1.13f, ChosenHole.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
            Velocity = velocity;
            MoveAndSlide();
            Down = false;
            OutTimer.Start(0.25f);
        }
    }
    public void _on_out_timer_timeout()
    {
        if (DangerTimer >= 0.75f)
        {
            //Call the smack!
            EmitSignal("OutTooLong", Position);
            // GD.Print("Danger Timer Exceeded, time for smack");
        }
        else
        {
            DangerTimer += 0.25f;
        }
    }

    public void PopDown()
    {
        DangerTimer = 0;
        var velocity = Velocity;
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(Position.X, 0.85f, Position.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
        Velocity = velocity;
        MoveAndSlide();
        Down = true;
        PopOutTimer.Start(2);
        OutTimer.Stop();
    }



    public Vector3 GetChosenHole()
    {
        return ChosenHole;
    }
    public bool GetDownStatus()
    {
        return Down;
    }
    public float GetDangerTimer()
    {
        return DangerTimer;
    }
}

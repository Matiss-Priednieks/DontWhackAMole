using Godot;
using Godot.Collections;
using System;

public partial class Mole : Area3D
{
    [Signal] public delegate void OutTooLongEventHandler(Vector3 Position);
    Vector3[] Holes;
    bool Down = true;
    Vector3 ChosenHole;
    Timer PopOutTimer, OutTimer;
    float DangerTimer = 0;
    int Lives = 3;
    MeshInstance3D MoleMesh;
    MeshInstance3D LivesCounter;

    int IFrames = 30;
    public override void _Ready()
    {
        MoleMesh = GetNode<MeshInstance3D>("%MoleMesh");
        LivesCounter = GetNode<MeshInstance3D>("%LivesCounter");
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
        if (IFrames <= 30 && IFrames > 0)
        {
            IFrames--;
        }
        // Holes[GD.RandRange(0, Holes.Count)];
        var TextM = (TextMesh)LivesCounter.Mesh;
        TextM.Text = Lives.ToString();
        LivesCounter.Mesh = TextM;

        if (Input.IsActionJustPressed("top_hole"))
        {
            ChosenHole = Holes[0];
            PopDown();
        }
        if (Input.IsActionJustPressed("left_hole"))
        {
            ChosenHole = Holes[1];
            PopDown();
        }
        if (Input.IsActionJustPressed("bottom_hole"))
        {
            ChosenHole = Holes[2];
            PopDown();
        }
        if (Input.IsActionJustPressed("right_hole"))
        {
            ChosenHole = Holes[3];
            PopDown();
        }

    }

    public void _on_pop_out_timer_timeout()
    {
        //Pop Mole out
        if (Down)
        {
            Position = ChosenHole;
            Tween velTween = GetTree().CreateTween();
            velTween.TweenProperty(this, "position", new Vector3(ChosenHole.X, 1.13f, ChosenHole.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
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
        }
        else
        {
            DangerTimer += 0.25f;
        }
    }

    public void PopDown()
    {
        DangerTimer = 0;
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(Position.X, 0.85f, Position.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
        Down = true;
        // PopOutTimer.Start(2);
        OutTimer.Stop();
    }

    public async void GotHit()
    {
        DangerTimer = 0;
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(Position.X, 1f, Position.Z), 0.15f).SetTrans(Tween.TransitionType.Elastic);
        var tempScale = MoleMesh.Scale;
        tempScale = new Vector3(1.25f, 0.5f, 1.25f);
        MoleMesh.Scale = tempScale;
        velTween.TweenProperty(this, "position", new Vector3(Position.X, 0.85f, Position.Z), 0.15f).SetTrans(Tween.TransitionType.Elastic);
        Down = true;
        PopOutTimer.Start(2);
        OutTimer.Stop();
        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        tempScale = new Vector3(1, 1, 1);
        MoleMesh.Scale = tempScale;
    }

    public void _on_area_entered(Area3D area)
    {
        if (area is Mallet && IFrames <= 0)
        {
            IFrames = 30;
            GotHit();
            Lives--;
        }
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
    public float GetIFrames()
    {
        return IFrames;
    }
}

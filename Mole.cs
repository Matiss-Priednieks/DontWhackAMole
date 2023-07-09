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
    MeshInstance3D LivesCounter, ScoreCounter;
    Dictionary HoleDictionary;
    float ScoreAcceleration = 0;

    float Score = 0;

    int IFrames = 60;
    bool GameOver = false;
    Camera3D CameraRef;
    RandomNumberGenerator RNG;
    AudioStreamPlayer3D Bonk;
    public override void _Ready()
    {
        CameraRef = GetNode<Camera3D>("%Camera3D");
        MoleMesh = GetNode<MeshInstance3D>("%MoleMesh");
        LivesCounter = GetNode<MeshInstance3D>("%LivesCounter");
        ScoreCounter = GetNode<MeshInstance3D>("%ScoreCounter");
        Bonk = GetNode<AudioStreamPlayer3D>("%BonkSound");

        RNG = new RandomNumberGenerator();

        Holes = new Vector3[]{
            new Vector3(0, 0.85f, -0.35f), //up (W)
            new Vector3(-0.35f, 0.85f, 0), //left (A)
            new Vector3(0, 0.85f, 0.35f), //down (S)
            new Vector3(0.35f, 0.85f, 0) //right (D))
            };
        HoleDictionary = new Dictionary()
        {
            {"Top",Holes[0]},
            {"Left",Holes[1]},
            {"Down",Holes[2]},
            {"Right",Holes[3]}
        };
        ChosenHole = Holes[0];
        Position = Holes[0];

        PopOutTimer = GetNode<Timer>("%PopOutTimer");
        OutTimer = GetNode<Timer>("%OutTimer");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IFrames <= 60 && IFrames > 0)
        {
            IFrames--;
        }
        // Holes[GD.RandRange(0, Holes.Count)];
        var LivesMesh = (TextMesh)LivesCounter.Mesh;
        LivesMesh.Text = Lives.ToString();
        LivesCounter.Mesh = LivesMesh;

        var ScoreMesh = (TextMesh)ScoreCounter.Mesh;
        ScoreMesh.Text = Math.Round(Score).ToString();
        ScoreCounter.Mesh = ScoreMesh;

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
        if (!Down)
        {
            ScoreAcceleration += 0.01f;
            Score += ScoreAcceleration;
        }


        if (Lives == 0)
        {
            GameOver = true;
        }
    }

    public void _on_pop_out_timer_timeout()
    {
        //Pop Mole out
        if (Down && Lives > 0)
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
        Bonk.Play(0);
        ScreenShake();
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
        // Bonk.Stop();
    }

    public void _on_area_entered(Area3D area)
    {
        if (area is Mallet mallet && IFrames <= 0)
        {
            IFrames = 60;
            GotHit();
            if (Lives > 0)
            {
                // Lives--;
            }
        }
    }


    public void ScreenShake()
    {
        var OriginalTransform = CameraRef;
        Tween camShakePosTween = GetTree().CreateTween();
        camShakePosTween.TweenProperty(CameraRef, "position", new Vector3(CameraRef.Position.X + RNG.RandfRange(-0.035f, 0.035f), CameraRef.Position.Y + RNG.RandfRange(-0.025f, 0.025f), CameraRef.Position.Z + RNG.RandfRange(-0.035f, 0.035f)), 0.1f).SetTrans(Tween.TransitionType.Elastic);
        camShakePosTween.TweenProperty(CameraRef, "position", new Vector3(CameraRef.Position.X, CameraRef.Position.Y, CameraRef.Position.Z), 0.1f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        Tween camShakeRotTween = GetTree().CreateTween();
        camShakeRotTween.TweenProperty(CameraRef, "rotation", new Vector3(CameraRef.Rotation.X + RNG.RandfRange(-0.035f, 0.035f), CameraRef.Rotation.Y + RNG.RandfRange(-0.025f, 0.025f), CameraRef.Rotation.Z + RNG.RandfRange(-0.035f, 0.035f)), 0.1f).SetTrans(Tween.TransitionType.Bounce);
        camShakeRotTween.TweenProperty(CameraRef, "rotation", new Vector3(CameraRef.Rotation.X, CameraRef.Rotation.Y, CameraRef.Rotation.Z), 0.1f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
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
    public bool GetGameOver()
    {
        return GameOver;
    }
}

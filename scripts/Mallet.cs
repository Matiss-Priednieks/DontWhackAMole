using Godot;
using Godot.Collections;

public partial class Mallet : Area3D
{
    // Called when the node enters the scene tree for the first time.
    Mole Player;
    Vector3[] Holes;
    bool HasHit, MoleOutTooLong;
    Vector3 StartPosition = Vector3.Zero;
    Dictionary HoleDictionary;
    Vector3 NextHit;
    public CollisionShape3D MalletCollider;
    int HoleIndex;
    Timer PopOutTimer;
    public bool Playing;
    Camera3D CameraRef;
    RandomNumberGenerator RNG;
    AudioStreamPlayer3D Miss;
    AudioStreamPlayer AboutToHit;
    bool MoleHit = false;
    public float MalletSlowSpeed = 0.3f;
    public float MalletFastSpeed = 0.2f;
    public override void _Ready()
    {
        GD.Randomize();
        Miss = GetNode<AudioStreamPlayer3D>("%Miss");
        AboutToHit = GetNode<AudioStreamPlayer>("%AboutToHit");
        RNG = new RandomNumberGenerator();
        CameraRef = GetNode<Camera3D>("%Camera3D");
        MalletCollider = GetNode<CollisionShape3D>("%MalletCollider");
        Player = GetNode<Mole>("%Mole");
        PopOutTimer = GetNode<Timer>("%PopOutTimer");
        StartPosition = new Vector3(-0.8f, 1.17f, -11.688f);
        Holes = new Vector3[]{
            new (0, 1.142f, -11.848f), 		//top(W)
			new (-0.24f, 1.142f, -11.638f), //left (A)
			new (0, 1.142f, -11.428f), 		//bottom (S)
			new (0.24f, 1.142f, -11.638f) 	//right (D))
			};
        HoleDictionary = new Dictionary()
        {
            {0,"%TopBulb"},
            {1,"%LeftBulb"},
            {2,"%BottomBulb"},
            {3,"%RightBulb"}
        };
        HoleIndex = GD.RandRange(0, 3);
        NextHit = Holes[HoleIndex];
    }

    public override void _Process(double delta)
    {
        Playing = Player.Playing;
        if (Playing && !Player.Paused)
        {
            // if (MoleOutTooLong)
            // {
            //     MoleOutTooLong = !Player.GetDownStatus();
            // }

            if (PopOutTimer.TimeLeft < 0.75f && !Player.GetGameOver())
            {
                GetNode<Hole>(HoleDictionary[HoleIndex].ToString()).Flash(true);

                if (!AboutToHit.Playing)
                {
                    AboutToHit.Play(0);
                }
            }
            else
            {
                GetNode<Hole>(HoleDictionary[HoleIndex].ToString()).Flash(false);
                AboutToHit.Stop();
            }
        }
        else if (!Playing || Player.Paused)
        {
            if (AboutToHit.Playing)
            {
                AboutToHit.Stop();
            }
        }
    }
    public async void _on_pop_out_timer_timeout()
    {
        if (!Player.GetGameOver() && Playing && !Player.Paused)
        {
            MoveMallet(NextHit);

            Hit();

            await ToSignal(GetTree().CreateTimer(MalletSlowSpeed), "timeout");
            MoveMallet(StartPosition);
            HoleIndex = GD.RandRange(0, 3);
            NextHit = Holes[HoleIndex];
        }
    }
    public async void _on_mole_out_too_long(Vector3 playerPosition)
    {
        if (!Player.GetGameOver() && Playing && !Player.Paused)
        {
            MoleOutTooLong = true;
            MoveMallet(playerPosition);
            if (!HasHit)
            {
                if (Player.GetDownStatus() == false)
                {
                    Hit();
                    MoleOutTooLong = false;
                }
                await ToSignal(GetTree().CreateTimer(MalletSlowSpeed), "timeout");
                MoveMallet(StartPosition);
            }
            HasHit = false;
        }
    }

    public void _on_area_entered(Area3D area)
    {
        Miss.Play();
        MoleHit = false;
    }

    public async void MoveMallet(Vector3 toLocation)
    {
        MalletCollider.Disabled = true;
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(toLocation.X - 0.35f, Position.Y, toLocation.Z), MalletFastSpeed).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.InOut);
        await ToSignal(GetTree().CreateTimer(MalletFastSpeed), "timeout");
        MalletCollider.Disabled = false;
    }

    public async void Hit()
    {
        // GD.Print("Test");
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        ScreenShake();
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "rotation", new Vector3(Rotation.X, Rotation.Y, Rotation.Z - 1.4f), MalletFastSpeed).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        velTween.TweenProperty(this, "rotation", new Vector3(0, 0, 0), MalletSlowSpeed).SetTrans(Tween.TransitionType.Sine).SetTrans(Tween.TransitionType.Back);
        HasHit = true;
    }

    public void ScreenShake()
    {
        var OriginalTransform = CameraRef;
        Tween camShakePosTween = GetTree().CreateTween();
        camShakePosTween.TweenProperty(CameraRef, "position", new Vector3(CameraRef.Position.X + RNG.RandfRange(-0.005f, 0.005f), CameraRef.Position.Y + RNG.RandfRange(-0.001f, 0.001f), CameraRef.Position.Z + RNG.RandfRange(-0.005f, 0.005f)), 0.1f).SetTrans(Tween.TransitionType.Elastic);
        camShakePosTween.TweenProperty(CameraRef, "position", new Vector3(CameraRef.Position.X, CameraRef.Position.Y, CameraRef.Position.Z), 0.1f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

        Tween camShakeRotTween = GetTree().CreateTween();
        camShakeRotTween.TweenProperty(CameraRef, "rotation", new Vector3(CameraRef.Rotation.X + RNG.RandfRange(-0.005f, 0.005f), CameraRef.Rotation.Y + RNG.RandfRange(-0.001f, 0.001f), CameraRef.Rotation.Z + RNG.RandfRange(-0.005f, 0.005f)), 0.1f).SetTrans(Tween.TransitionType.Bounce);
        camShakeRotTween.TweenProperty(CameraRef, "rotation", new Vector3(CameraRef.Rotation.X, CameraRef.Rotation.Y, CameraRef.Rotation.Z), 0.1f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
    }
}


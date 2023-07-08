using Godot;
using Godot.Collections;
using System;

public partial class Mallet : Area3D
{
    // Called when the node enters the scene tree for the first time.
    Mole Player;
    Vector3[] Holes;
    bool HasHit, MoleOutTooLong;
    Vector3 StartPosition = Vector3.Zero;
    Dictionary HoleDictionary;
    Vector3 NextHit;
    CollisionShape3D MalletCollider;
    int HoleIndex;
    Timer PopOutTimer;
    public override void _Ready()
    {
        GD.Randomize();
        MalletCollider = GetNode<CollisionShape3D>("%MalletCollider");
        Player = GetNode<Mole>("%Mole");
        PopOutTimer = GetNode<Timer>("%PopOutTimer");
        StartPosition = new Vector3(-0.8f, 1.325f, -0.115f);
        Holes = new Vector3[]{
            new Vector3(0, 0.85f, 0.35f),
            new Vector3(0.35f, 0.85f, 0),
            new Vector3(0, 0.85f, -0.35f),
            new Vector3(-0.35f, 0.85f, 0)
            };

        HoleDictionary = new Dictionary()
        {
            {0,"%Top"},
            {1,"%Left"},
            {2,"%Bottom"},
            {3,"%Right"}
        };
        HoleIndex = GD.RandRange(0, 3);
        NextHit = Holes[HoleIndex];
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (MoleOutTooLong)
        {
            MoleOutTooLong = !Player.GetDownStatus();
        }

        if (PopOutTimer.TimeLeft < 1.5f)
        {
            (GetNode<MeshInstance3D>(HoleDictionary[HoleIndex].ToString()).Mesh.SurfaceGetMaterial(0).NextPass as ShaderMaterial).SetShaderParameter("Flashing", true);
            ShaderMaterial mat = (ShaderMaterial)GetNode<MeshInstance3D>(HoleDictionary[HoleIndex].ToString()).Mesh.SurfaceGetMaterial(0).NextPass;
            // MeshInstance3D mat = GetNode<MeshInstance3D>(HoleDictionary[HoleIndex].ToString());
            // mat.Mesh.SurfaceGetMaterial(0);
            // (Material as ShaderMaterial).SetShaderParam("whiten", true);

        }
        else
        {
            (GetNode<MeshInstance3D>(HoleDictionary[HoleIndex].ToString()).Mesh.SurfaceGetMaterial(0).NextPass as ShaderMaterial).SetShaderParameter("Flashing", false);
        }
    }
    public async void _on_pop_out_timer_timeout()
    {
        MoveMallet(NextHit);

        Hit();

        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        MoveMallet(StartPosition);
        HoleIndex = GD.RandRange(0, 3);
        NextHit = Holes[HoleIndex];
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

    public async void MoveMallet(Vector3 toLocation)
    {
        MalletCollider.Disabled = true;
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "position", new Vector3(toLocation.X - 0.35f, Position.Y, toLocation.Z), 0.2f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.InOut);
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        MalletCollider.Disabled = false;
    }

    public async void Hit()
    {
        // GD.Print("???");
        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        Tween velTween = GetTree().CreateTween();
        velTween.TweenProperty(this, "rotation", new Vector3(Rotation.X, Rotation.Y, Rotation.Z - 1.25f), 0.2f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
        velTween.TweenProperty(this, "rotation", new Vector3(0, 0, 0), 0.3f).SetTrans(Tween.TransitionType.Sine).SetTrans(Tween.TransitionType.Back);
        HasHit = true;
    }
}


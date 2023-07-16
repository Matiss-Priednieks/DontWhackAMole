using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class Coin : Area3D
{
    public Vector3[] Holes { get; private set; }
    public Dictionary HoleDictionary { get; private set; }
    public int HoleIndex { get; private set; }
    public Vector3 NextLocation { get; private set; }
    public Vector3 DownPosition { get; private set; }
    public Vector3 UpPosition { get; private set; }
    public bool Down { get; private set; }
    public bool Out { get; private set; }
    public bool Finished { get; private set; }
    public bool IsCollected { get; private set; }

    MeshInstance3D ComboCounter;
    MeshInstance3D CoinMesh;
    GpuParticles3D CoinBreakParticle;
    CollisionShape3D CoinCollider;
    AudioStreamPlayer3D Shatter, Collected;

    Mole MoleRef;
    public override void _Ready()
    {
        Collected = GetNode<AudioStreamPlayer3D>("%Collected");
        Shatter = GetNode<AudioStreamPlayer3D>("%Shatter");
        Holes = new Vector3[]{
            new Vector3(0, 0.85f, -0.35f), //up (W)
            new Vector3(-0.35f, 0.85f, 0), //left (A)
            new Vector3(0, 0.85f, 0.35f), //down (S)
            new Vector3(0.35f, 0.85f, 0) //right (D))
            };
        GD.Randomize();
        MoleRef = GetNode<Mole>("../Mole");
        ComboCounter = GetNode<MeshInstance3D>("../WHACKmachine/ComboBonus/Comboboard/ComboText/ComboCounter");
        CoinMesh = GetNode<MeshInstance3D>("%CoinMesh");
        CoinBreakParticle = GetNode<GpuParticles3D>("%CoinBreakParticle");
        CoinCollider = GetNode<CollisionShape3D>("%CoinCollider");

        HoleIndex = GD.RandRange(0, 3);
        NextLocation = Holes[HoleIndex];
        while (NextLocation == MoleRef.GetChosenHole())
        {
            HoleIndex = GD.RandRange(0, 3);
            NextLocation = Holes[HoleIndex];
        }
        // PopOut();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Finished && MoleRef.Playing)
        {
            PopOut();
            Finished = true;
        }
    }

    public async void PopOut()
    {
        Position = NextLocation;
        UpPosition = new Vector3(Position.X, 1.25f, Position.Z);
        var tempRotation = new Vector3(GD.RandRange(-10, 10), GD.RandRange(-10, 10), GD.RandRange(-10, 10));

        Tween tempTween = CreateTween();
        tempTween.Parallel().TweenProperty(this, "position", UpPosition, 1.25f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        tempTween.Parallel().TweenProperty(this, "rotation", tempRotation, 2.15f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

        await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
        Out = true;
    }
    public async void _on_despawn_timer_timeout()
    {
        if (!IsCollected)
        {
            GD.Print(CoinBreakParticle.GlobalPosition);
            CoinMesh.Hide();
            CoinBreakParticle.Emitting = true;
            Shatter.Play(0);
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            CallDeferred("queue_free");
        }
    }

    public async void _on_area_entered(Area3D area)
    {
        if (area is Mole mole && Out)
        {
            Collected.Play(0);
            IsCollected = true;
            CallDeferred("DisableCollisions");
            Tween tempTween = CreateTween();
            tempTween.Parallel().TweenProperty(this, "position", new Vector3(ComboCounter.GlobalPosition.X + 0.05f, ComboCounter.GlobalPosition.Y, ComboCounter.GlobalPosition.Z + 0.05f), 0.2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
            tempTween.Parallel().TweenProperty(this, "scale", new Vector3(0.01f, 0.01f, 0.01f), 0.5f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
            mole.AnimateScoreCombo();
            await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
            CoinMesh.Hide();
            CallDeferred("queue_free");
        }
    }
    public void DisableCollisions()
    {
        CoinCollider.Disabled = true;
    }
}

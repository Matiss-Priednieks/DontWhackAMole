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
	public float CoinCollectionBonus { get; private set; } = 1000;

	private const float MALLETSPEEDCHANGE = 0.0025f;
	private const float POPOUTSPEEDCHANGE = 0.025f;
	float YPos;

	MeshInstance3D ComboCounter;
	Node3D CoinMesh;
	GpuParticles3D CoinBreakParticle;
	CollisionShape3D CoinCollider;
	AudioStreamPlayer3D Shatter, Collected;
	RandomNumberGenerator RNG;
	Vector3 RandomChosenHole;

	Mole MoleRef;
	Mallet MalletRef;
	public override void _Ready()
	{
		RNG = new RandomNumberGenerator();

		Collected = GetNode<AudioStreamPlayer3D>("%Collected");
		Shatter = GetNode<AudioStreamPlayer3D>("%Shatter");
		Holes = new Vector3[]{
			new (0, 1.142f, -11.848f), 		//top(W)
			new (-0.24f, 1.142f, -11.638f), 	//left (A)
			new (0, 1.142f, -11.428f), 		//bottom (S)
			new (0.24f, 1.142f, -11.638f) 	//right (D))
			};
		GD.Randomize();
		MoleRef = GetNode<Mole>("../Mole");
		MalletRef = GetNode<Mallet>("../Mallet");
		ComboCounter = GetNode<MeshInstance3D>("../NewWhackMachine/ComboBonus/ComboText/ComboCounter");
		CoinMesh = GetNode<Node3D>("%CoinMesh");
		CoinBreakParticle = GetNode<GpuParticles3D>("%CoinBreakParticle");
		CoinCollider = GetNode<CollisionShape3D>("%CoinCollider");

		ChangeRandomHole();
		NextLocation = RandomChosenHole;
		// PopOut();
	}

	public override void _PhysicsProcess(double delta)
	{
		// GD.Print(CoinBreakParticle.Position);
		if (!Finished && MoleRef.CurrentGameState == Mole.GameState.Playing)
		{
			PopOut();
			Finished = true;
		}
		Position = new(Position.X, YPos, Position.Z);
		RotationDegrees += new Vector3(GD.RandRange(-10, 10), GD.RandRange(-10, 10), GD.RandRange(-10, 10));
	}

	public async void PopOut()
	{
		Position = NextLocation;
		UpPosition = new Vector3(Position.X, 1.25f, Position.Z);
		var tempRotation = new Vector3(GD.RandRange(-10, 10), GD.RandRange(-10, 10), GD.RandRange(-10, 10));

		YPos = Position.Y;
		Tween tempTween = CreateTween();
		tempTween.Parallel().TweenProperty(this, "YPos", UpPosition.Y, 1.25f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		tempTween.Parallel().TweenProperty(this, "rotation", tempRotation, 2.15f).SetTrans(Tween.TransitionType.Circ).SetEase(Tween.EaseType.OutIn);
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
		Out = true;
	}
	public async void _on_despawn_timer_timeout()
	{
		if (!IsCollected)
		{
			CallDeferred("DisableCollisions");
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
			if (mole.PopSpeed > 0.75f)
			{
				mole.PopSpeed -= POPOUTSPEEDCHANGE;
				mole.Score += CoinCollectionBonus;
			}
			if (MalletRef.MalletFastSpeed >= 0.1)
			{
				MalletRef.MalletFastSpeed -= MALLETSPEEDCHANGE;
			}
			if (MalletRef.MalletSlowSpeed >= 0.2)
			{
				MalletRef.MalletSlowSpeed -= MALLETSPEEDCHANGE;
			}
			if (mole.OutTooLongTime > 0.5)
			{
				mole.OutTooLongTime -= POPOUTSPEEDCHANGE;
			}
			Collected.Play(0);
			IsCollected = true;
			CallDeferred("DisableCollisions");
			Tween tempTween = CreateTween();
			tempTween.Parallel().TweenProperty(this, "position", new Vector3(ComboCounter.GlobalPosition.X + 0.05f, ComboCounter.GlobalPosition.Y, ComboCounter.GlobalPosition.Z + 0.05f), 0.2f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
			tempTween.Parallel().TweenProperty(this, "scale", new Vector3(0.01f, 0.01f, 0.01f), 0.5f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
			mole.AnimateScoreCombo();
			mole.AnimateScore();
			await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
			CoinMesh.Hide();
			CallDeferred("queue_free");
		}
	}
	public void DisableCollisions()
	{
		CoinCollider.Disabled = true;
	}
	public Vector3 ChangeRandomHole()
	{
		int randomHoleIndex = RNG.RandiRange(0, Holes.Length - 1);
		RandomChosenHole = Holes[randomHoleIndex];
		while (MoleRef.GetChosenHole() == RandomChosenHole)
		{
			randomHoleIndex = RNG.RandiRange(0, Holes.Length - 1);
			RandomChosenHole = Holes[randomHoleIndex];
		}
		return RandomChosenHole;
	}
}

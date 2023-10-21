using Godot;
using Godot.Collections;
using System;

public partial class Mole : Area3D
{
	LoggedInUser User;

	[Signal] public delegate void OutTooLongEventHandler(Vector3 Position);
	Vector3[] Holes;
	bool Down = true;
	Vector3 ChosenHole;
	Timer PopOutTimer, OutTimer;
	float DangerTimer = 0;
	int Lives = 3;
	MeshInstance3D MoleMesh;
	MeshInstance3D LivesCounter, ScoreCounter, ComboCounter, HighScoreMesh, HighestComboMesh;
	Dictionary HoleDictionary;
	float ScoreAcceleration = 0;

	SaveManager SaveManager;

	public float Score { get; set; }

	int IFrames = 60;
	bool GameOver = false;
	public bool Playing, Paused = false;
	Camera3D CameraRef;
	RandomNumberGenerator RNG;
	AudioStreamPlayer3D Bonk, Move, CounterSound;
	int FinalScore, HighScore;
	public int ComboBonus { get; set; }
	public int HighestCombo;
	public float PopSpeed { get; set; }
	public float OutTooLongTime { get; set; }
	public bool RequestNotSent { get; private set; } = true;


	int CurrentHole = 0;
	public override void _Ready()
	{
		OutTooLongTime = 1;
		PopSpeed = 2;
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		this.SaveManager = GetNode<SaveManager>("/root/SaveManager");
		Score = 0;
		ComboBonus = 1;
		CounterSound = GetNode<AudioStreamPlayer3D>("%CounterSound");

		CameraRef = GetNode<Camera3D>("%Camera3D");
		MoleMesh = GetNode<MeshInstance3D>("%MoleMesh");
		LivesCounter = GetNode<MeshInstance3D>("%LivesCounter");
		ScoreCounter = GetNode<MeshInstance3D>("%ScoreCounter");
		ComboCounter = GetNode<MeshInstance3D>("%ComboCounter");
		HighScoreMesh = GetNode<MeshInstance3D>("%Highscore");
		HighestComboMesh = GetNode<MeshInstance3D>("%HighestCombo");
		Bonk = GetNode<AudioStreamPlayer3D>("%BonkSound");
		Move = GetNode<AudioStreamPlayer3D>("%MoveSound");

		RNG = new RandomNumberGenerator();

		Holes = new Vector3[]{
			new (-0.3f, 1.142f, -0.21f), //top left (W)
			new (0, 1.142f, -0.21f), //top middle (A)
			new (0.3f, 1.142f, -0.21f), //top right (S)
			new (-0.15f, 1.142f, 0.05f), //bottom left (D))
			new (0.15f, 1.142f, 0.05f) //bottom right (D))
			};
		HoleDictionary = new Dictionary()
		{
			{"TopLeft",Holes[0]},
			{"TopMiddle",Holes[1]},
			{"TopRight",Holes[2]},
			{"BottomLeft",Holes[3]},
			{"BottomRight",Holes[4]}
		};
		ChosenHole = Holes[0];
		Position = Holes[0];

		PopOutTimer = GetNode<Timer>("%PopOutTimer");
		OutTimer = GetNode<Timer>("%OutTimer");
		HighScore = (int)this.SaveManager.LoadScore(User.Username).X;
		HighestCombo = (int)this.SaveManager.LoadScore(User.Username).Y;
		PopOutTimer.Start(PopSpeed);
	}

	public override void _Process(double delta)
	{

		if (Score > HighScore)
		{
			HighScore = (int)Score;
			User.SetHighscore(HighScore);
			this.SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		}
		if (ComboBonus > HighestCombo)
		{
			HighestCombo = ComboBonus;
			this.SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		}
		var LivesMesh = (TextMesh)LivesCounter.Mesh;
		LivesMesh.Text = Lives.ToString();
		LivesCounter.Mesh = LivesMesh;

		var ScoreMesh = (TextMesh)ScoreCounter.Mesh;
		ScoreMesh.Text = Math.Round(Score).ToString();
		ScoreCounter.Mesh = ScoreMesh;


		var ComboMesh = (TextMesh)ComboCounter.Mesh;
		ComboMesh.Text = "x" + ComboBonus.ToString();
		ComboCounter.Mesh = ComboMesh;

		var HSMesh = (TextMesh)HighScoreMesh.Mesh;
		HSMesh.Text = "Highscore: " + HighScore.ToString();
		HighScoreMesh.Mesh = HSMesh;

		var HCMesh = (TextMesh)HighestComboMesh.Mesh;
		HCMesh.Text = "x" + HighestCombo.ToString();
		HighestComboMesh.Mesh = HCMesh;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Playing && !Paused)
		{

			if (IFrames <= 60 && IFrames > 0)
			{
				IFrames--;
			}

			if (Input.IsActionJustPressed("top_hole"))
			{
				Move.Play();
				if (CurrentHole == 3)
				{
					CurrentHole = 0;
				}
				if (CurrentHole == 4)
				{
					CurrentHole = 2;
				}
				ChosenHole = Holes[CurrentHole];
				PopDown();
			}
			if (Input.IsActionJustPressed("left_hole"))
			{
				Move.Play();
				if (CurrentHole > 0)
				{
					CurrentHole--;
				}
				if (CurrentHole > 3)
				{
					CurrentHole--;
				}
				ChosenHole = Holes[CurrentHole];
				PopDown();
			}
			if (Input.IsActionJustPressed("bottom_hole"))
			{
				Move.Play();
				if (CurrentHole == 0)
				{
					CurrentHole = 3;
				}
				if (CurrentHole == 2)
				{
					CurrentHole = 4;
				}
				if (CurrentHole == 1)
				{
					CurrentHole = 4;
				}
				ChosenHole = Holes[CurrentHole];
				PopDown();
			}
			if (Input.IsActionJustPressed("right_hole"))
			{
				Move.Play();
				if (CurrentHole < 4)
				{
					CurrentHole++;
				}
				ChosenHole = Holes[CurrentHole];
				PopDown();
			}

			if (!Down && !Paused)
			{
				ScoreAcceleration += 0.005f * ComboBonus;
				Score += ScoreAcceleration;
				PlaySoundDelayed();
			}

			if (Lives <= 0)
			{
				SetGameOver(true);

				Playing = false;
			}
		}
		FinalScore = (int)Score;
		GetNode<Label>("%FinalScore").Text = FinalScore.ToString();


	}
	public async void PlaySoundDelayed()
	{
		CounterSound.Play(0);
		await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
	}

	public void Restart()
	{
		Score = 0;
		Lives = 3;
		DangerTimer = 0;
		HighScore = (int)FinalScore;
		User.SetHighscore(HighScore);
		this.SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		Error requestResult = User.HighscoreUpdateRequest();
		GD.Print("Score sent to server.");
	}

	public void _on_pop_out_timer_timeout()
	{
		//Pop Mole out
		if (Down && Lives > 0 && Playing && !Paused)
		{
			Position = ChosenHole;
			Tween velTween = GetTree().CreateTween();
			velTween.TweenProperty(this, "position", new Vector3(ChosenHole.X, 1.13f, ChosenHole.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
			Down = false;
			OutTimer.Start(OutTooLongTime * 0.25f);
		}
		PopOutTimer.Start(PopSpeed);
	}
	public void _on_out_timer_timeout()
	{
		if (DangerTimer >= OutTooLongTime * 0.75f && Playing && !Paused)
		{
			//Call the smack!
			GD.Print(DangerTimer, OutTooLongTime);
			EmitSignal("OutTooLong", Position);
		}
		else
		{
			DangerTimer += OutTooLongTime * 0.25f;
		}
	}

	public void PopDown()
	{
		DangerTimer = 0;
		Tween velTween = GetTree().CreateTween();
		velTween.TweenProperty(this, "position", new Vector3(Position.X, 0.85f, Position.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
		Down = true;
		OutTimer.Stop();
	}

	public async void GotHit()
	{
		OutTooLongTime = 1;
		ScoreAcceleration = 0;
		PopSpeed = 2;
		ComboBonus = 1;
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
		PopOutTimer.Start(PopSpeed);
		OutTimer.Stop();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
		tempScale = new Vector3(1, 1, 1);
		MoleMesh.Scale = tempScale;
	}

	public void _on_area_entered(Area3D area)
	{
		if (area is Mallet mallet && IFrames <= 0)
		{
			IFrames = 60;
			GotHit();
			if (Lives > 0)
			{
				Lives--;
				AnimateHealth();
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

	public void AnimateHealth()
	{
		Vector3 defaultRot = Vector3.Zero;
		Vector3 lifeLostRot = new Vector3(360, 0, 0);
		Tween scoreBoomRot = GetTree().CreateTween();
		scoreBoomRot.TweenProperty(LivesCounter, "rotation", lifeLostRot, 0.5f).SetTrans(Tween.TransitionType.Elastic);
		scoreBoomRot.TweenProperty(LivesCounter, "rotation", defaultRot, 0.1f).SetTrans(Tween.TransitionType.Elastic);
	}
	public void AnimateScoreCombo()
	{
		ComboBonus++;

		Vector3 defaultScale = Vector3.One;
		Vector3 defaultPos = new Vector3(0, -0.171f, 0.01f);
		Vector3 defaultRot = Vector3.Zero;

		if (ComboBonus % 5 == 0)
		{
			Vector3 comboScale = new Vector3(1.3f, 1.3f, 1.3f);
			Vector3 comboPos = new Vector3(0, 0.1f, 0.25f);
			Vector3 comboRot = new Vector3(0, (float)GD.RandRange(-0.25f, 0.25f), (float)GD.RandRange(-0.6f, 0f));
			Tween scoreBoomPos = GetTree().CreateTween();
			scoreBoomPos.TweenProperty(ComboCounter, "position", comboPos, 0.5f).SetTrans(Tween.TransitionType.Elastic);
			scoreBoomPos.TweenProperty(ComboCounter, "position", defaultPos, 0.5f).SetTrans(Tween.TransitionType.Elastic);

			Tween scoreBoomScale = GetTree().CreateTween();
			scoreBoomScale.TweenProperty(ComboCounter, "scale", comboScale, 0.5f).SetTrans(Tween.TransitionType.Elastic);
			scoreBoomScale.TweenProperty(ComboCounter, "scale", defaultScale, 0.5f).SetTrans(Tween.TransitionType.Elastic);

			Tween scoreBoomRot = GetTree().CreateTween();
			scoreBoomRot.TweenProperty(ComboCounter, "rotation", comboRot, 0.5f).SetTrans(Tween.TransitionType.Elastic);
			scoreBoomRot.TweenProperty(ComboCounter, "rotation", defaultRot, 0.5f).SetTrans(Tween.TransitionType.Elastic);

			//TODO: Make this only increase when combo item is picked up or some more specific action is taken, rather than arbitrary score increase!
		}
		else
		{
			Vector3 smallComboScale = new Vector3(1.4f, 1.4f, 1.4f);
			Tween scoreBoomScale = GetTree().CreateTween();
			scoreBoomScale.TweenProperty(ComboCounter, "scale", smallComboScale, 0.1f).SetTrans(Tween.TransitionType.Elastic);
			scoreBoomScale.TweenProperty(ComboCounter, "scale", defaultScale, 0.1f).SetTrans(Tween.TransitionType.Elastic);
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
	public bool GetGameOver()
	{
		return GameOver;
	}
	public void SetGameOver(bool value)
	{
		GameOver = value;
	}
	public async void _on_login_request_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		var response = Json.ParseString(body.GetStringFromUtf8());
		await ToSignal(GetTree().CreateTimer(1f), SceneTreeTimer.SignalName.Timeout);
		if (responseCode == 200)
		{
			HighScore = (int)User.GetHighscore();
		}
	}
}

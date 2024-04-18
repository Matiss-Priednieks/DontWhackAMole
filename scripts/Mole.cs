using Godot;
using Godot.Collections;
using System;
using System.Xml.Schema;

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
	MeshInstance3D LivesCounter, ScoreCounter, ComboCounter, HighScoreMesh, HighestComboMesh, EarlyPopCounter;
	Dictionary HoleDictionary;
	float ScoreAcceleration = 0;

	SaveManager SaveManager;

	public float Score { get; set; }
	public float score
	{
		get => Score;
		set
		{
			Score = value;
			PlaySoundDelayed();
		}
	}
	public int EarlyPops = 0;

	int IFrames = 60;
	bool GameOver = false;

	//TODO: make safer
	public enum GameState
	{
		Playing,
		Paused,
		GameOver
	}
	public GameState CurrentGameState = GameState.Paused; //TODO: make safer
	Camera3D CameraRef;
	RandomNumberGenerator RNG;
	AudioStreamPlayer3D Bonk, Move, CounterSound;
	Node3D WarningIndicator;
	int FinalScore, HighScore;
	public int ComboBonus { get; set; }
	public int HighestCombo;
	public float PopSpeed { get; set; }
	public float OutTooLongTime { get; set; }
	public bool RequestNotSent { get; private set; } = true;
	AudioStream CounterSound2;

	int CurrentHole = 0;
	public override void _Ready()
	{
		OutTooLongTime = 1;
		PopSpeed = 2;
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		SaveManager = GetNode<SaveManager>("/root/SaveManager");
		Score = 0;
		ComboBonus = 1;
		CounterSound = GetNode<AudioStreamPlayer3D>("../%CounterSound");

		CameraRef = GetNode<Camera3D>("../%Camera3D");
		MoleMesh = GetNode<MeshInstance3D>("%MoleMesh");
		WarningIndicator = GetNode<Node3D>("%WarningIndicator");
		LivesCounter = GetNode<MeshInstance3D>("../%LivesCounter");
		ScoreCounter = GetNode<MeshInstance3D>("../%ScoreCounter");
		ComboCounter = GetNode<MeshInstance3D>("../%ComboCounter");
		EarlyPopCounter = GetNode<MeshInstance3D>("../%EarlyPopCounter");
		HighScoreMesh = GetNode<MeshInstance3D>("../%Highscore");
		HighestComboMesh = GetNode<MeshInstance3D>("../%HighestCombo");
		Bonk = GetNode<AudioStreamPlayer3D>("%BonkSound");
		Move = GetNode<AudioStreamPlayer3D>("%MoveSound");

		RNG = new RandomNumberGenerator();

		Holes = [
			new (0, 1.142f, -11.848f), 		//top(W)
			new (-0.24f, 1.142f, -11.638f), 	//left (A)
			new (0, 1.142f, -11.428f), 		//bottom (S)
			new (0.24f, 1.142f, -11.638f) 	//right (D))
			];
		HoleDictionary = new Dictionary()
		{
			{"Top",Holes[0]},
			{"Left",Holes[1]},
			{"Down",Holes[2]},
			{"Right",Holes[3]}
		};
		ChosenHole = Holes[0];
		Position = Holes[0];

		PopOutTimer = GetNode<Timer>("../%PopOutTimer");
		OutTimer = GetNode<Timer>("%OutTimer");
		HighScore = (int)SaveManager.LoadScore(User.Username).X;
		HighestCombo = (int)SaveManager.LoadScore(User.Username).Y;
		PopOutTimer.Start(PopSpeed);
		CounterSound2 = (AudioStream)ResourceLoader.Load("res://assets/audio/counter.wav");
	}

	public override void _Process(double delta)
	{

		if (Score > HighScore)
		{
			HighScore = (int)Score;
			User.SetHighscore(HighScore);
			// SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		}
		if (ComboBonus > HighestCombo)
		{
			HighestCombo = ComboBonus;
			// SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		}
		var LivesMesh = (TextMesh)LivesCounter.Mesh;
		LivesMesh.Text = Lives.ToString();
		LivesCounter.Mesh = LivesMesh;


		var EarlyPopMesh = (TextMesh)EarlyPopCounter.Mesh;
		EarlyPopMesh.Text = EarlyPops.ToString() + "/1";
		EarlyPopCounter.Mesh = EarlyPopMesh;

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
		switch (CurrentGameState)
		{
			case GameState.Playing:
				ProcessInput();
				UpdateScore();
				CheckGameOver();
				if (IFrames <= 60 && IFrames > 0)
				{
					IFrames--;
				}
				break;
			case GameState.Paused:
				// Handle paused state if needed
				break;
			case GameState.GameOver:
				// Handle game over state if needed
				break;
		}

		FinalScore = (int)Score;
		GetNode<Label>("../%FinalScore").Text = FinalScore.ToString();
	}


	private void ProcessInput()
	{
		if (Input.IsActionJustPressed("top_hole"))
		{
			MoveMole(0);
		}
		else if (Input.IsActionJustPressed("left_hole"))
		{
			MoveMole(1);
		}
		else if (Input.IsActionJustPressed("bottom_hole"))
		{
			MoveMole(2);
		}
		else if (Input.IsActionJustPressed("right_hole"))
		{
			MoveMole(3);
		}
		if (Input.IsActionJustReleased("popout") && EarlyPops > 0)
		{
			if (Down && Lives > 0 && CurrentGameState == GameState.Playing)
			{
				PopOut();
				EarlyPops--;
			}
		}
	}

	private void UpdateScore()
	{
		if (!Down)
		{
			ScoreAcceleration += 0.005f * ComboBonus;
			score += ScoreAcceleration;
			// if (Score / Mathf.Round(Score) >= 1)
			// {
			// 	PlaySoundDelayed();
			// }
			// PlaySoundDelayed();
		}
	}

	private void CheckGameOver()
	{
		if (Lives <= 0)
		{
			CurrentGameState = GameState.GameOver;
		}
	}


	public void _on_down_swipe(int Hole)
	{
		MoveMole(Hole);
	}
	public void _on_right_swipe(int Hole)
	{
		MoveMole(Hole);
	}
	public void _on_left_swipe(int Hole)
	{
		MoveMole(Hole);
	}
	public void _on_up_swipe(int Hole)
	{
		MoveMole(Hole);
	}

	public void MoveMole(int Hole)
	{
		Move.Play();
		ChosenHole = Holes[Hole];
		PopDown();
	}

	public async void PlaySoundDelayed()
	{
		AudioStreamPlayer3D scoreCounter = new()
		{
			Stream = CounterSound2,
			VolumeDb = -20
		};
		AddChild(scoreCounter);
		scoreCounter.Play(0);


		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		scoreCounter.QueueFree();
		// CounterSound.Play(0);
	}

	public void Restart()
	{
		Score = 0;
		Lives = 3;
		DangerTimer = 0;
		// HighScore = (int)FinalScore;
		// User.SetHighscore(HighScore);
		if (FinalScore > HighScore)
		{
			HighScore = (int)FinalScore;
			User.SetHighscore(HighScore);
			// SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		}
		SaveManager.SaveScore(User.Username, HighScore, HighestCombo);
		Error requestResult = User.HighscoreUpdateRequest();
		GD.Print("Score sent to server.");
	}

	public void _on_pop_out_timer_timeout()
	{
		//Pop Mole out
		if (Down && Lives > 0 && CurrentGameState == GameState.Playing)
		{
			PopOut();
		}
		PopOutTimer.Start(PopSpeed);
	}

	public void PopOut()
	{
		Position = ChosenHole;
		Tween velTween = GetTree().CreateTween();
		velTween.TweenProperty(this, "position", new Vector3(ChosenHole.X, 1.13f, ChosenHole.Z), 0.2f).SetTrans(Tween.TransitionType.Elastic);
		Down = false;
		OutTimer.Start(OutTooLongTime * 0.25f);
	}
	public void _on_out_timer_timeout()
	{
		if (DangerTimer >= OutTooLongTime * 0.75f && CurrentGameState == GameState.Playing)
		{
			//Call the smack!
			if (!Down)
			{
				WarningIndicator.Show();
			}

			GD.Print(DangerTimer + " : " + OutTooLongTime);
			EmitSignal("OutTooLong", Position);
		}
		else
		{
			WarningIndicator.Hide();
			DangerTimer += OutTooLongTime * 0.25f;
		}
	}

	public void PopDown()
	{
		WarningIndicator.Hide();
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
		PopOutTimer.Start(PopSpeed);
		OutTimer.Stop();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout");
		tempScale = new Vector3(1, 1, 1);
		MoleMesh.Scale = tempScale;
		PopDown();
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
		scoreBoomRot.TweenProperty(LivesCounter, "rotation", lifeLostRot, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		scoreBoomRot.TweenProperty(LivesCounter, "rotation", defaultRot, 0.1f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
	}
	public void AnimateScore()
	{
		Vector3 defaultScale = Vector3.One;
		Vector3 defaultPos = new(0, 0, 0.01f);
		Vector3 defaultRot = Vector3.Zero;

		Vector3 comboScale = new(1.3f, 1.3f, 1.3f);
		Vector3 comboPos = new(0, 0.1f, 0.25f);
		Vector3 comboRot = new(0, (float)GD.RandRange(-0.25f, 0.25f), (float)GD.RandRange(-0.6f, 0f));
		Tween scoreBoomPos = GetTree().CreateTween();
		scoreBoomPos.TweenProperty(ScoreCounter, "position", comboPos, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		scoreBoomPos.TweenProperty(ScoreCounter, "position", defaultPos, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

		Tween scoreBoomScale = GetTree().CreateTween();
		scoreBoomScale.TweenProperty(ScoreCounter, "scale", comboScale, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		scoreBoomScale.TweenProperty(ScoreCounter, "scale", defaultScale, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);

		Tween scoreBoomRot = GetTree().CreateTween();
		scoreBoomRot.TweenProperty(ScoreCounter, "rotation", comboRot, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
		scoreBoomRot.TweenProperty(ScoreCounter, "rotation", defaultRot, 0.5f).SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
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
	public int GetLives()
	{
		return Lives;
	}
	public void AddLives(int LivesToAdd)
	{
		Lives += LivesToAdd;
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

	public void AddPops()
	{
		EarlyPops++;
	}
	public int GetPops()
	{
		return EarlyPops;
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
	public int GetHighScore()
	{
		return HighScore;
	}
	public int GetHighestCombo()
	{
		return HighestCombo;
	}
}

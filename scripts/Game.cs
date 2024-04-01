using Godot;
using System;

public partial class Game : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public enum GameState
	{
		MENU,
		PLAYING,
		PAUSED
	}
	public enum GameMode
	{
		STORY,
		ARCADE
	}
	GameMode CurrentGameMode = GameMode.ARCADE;
	private GameState currentState = GameState.MENU;

	float CamPlayFOV;
	float CamMenuFOV;
	Camera3D MainCam;
	Vector3 CamPlayPos, CamPlayRot, CamMenuPos, CamMenuRot;
	RandomNumberGenerator RNG;
	SaveManager SaveManager;
	LoggedInUser User;
	Mallet Mallet;
	Mole Mole;
	Control MainMenu;
	Control GameOverMenu;
	Button PlayResume;
	PanelContainer MenuButtons, HelpMenu, SettingsMenu, AccountMenu;
	Panel LoginScreen, RegistrationScreen;
	Timer ComboCointTimer;

	WorldEnvironment worldEnvironment;

	Timer PopOutTimer;

	PackedScene Coin, Heart, EarlyPop;
	public bool Login, Register, CameraMoving = false;

	public bool MenuJustPressed { get; private set; }
	int[] CollectableSpawnTimes = { 4, 4, 6, 8, 8, 8, 16 };

	PackedScene[] Collectables;

	public override void _Ready()
	{
		Collectables = new PackedScene[3];
		worldEnvironment = GetNode<WorldEnvironment>("%WorldEnvironment");
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		User.LoggedInFakeReady();
		SaveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
		SaveManager.LoadConfig();

		LoginScreen = GetNode<Panel>("%LoginScreen");
		RegistrationScreen = GetNode<Panel>("%RegistrationScreen");

		RNG = new RandomNumberGenerator();
		MenuButtons = GetNode<PanelContainer>("%MenuButtons");
		AccountMenu = GetNode<PanelContainer>("%AccountMenu");
		HelpMenu = GetNode<PanelContainer>("%HelpScreen");
		SettingsMenu = GetNode<PanelContainer>("%SettingsMenu");
		MainMenu = GetNode<Control>("%Menu");
		MainCam = GetNode<Camera3D>("Camera3D");
		Mallet = GetNode<Mallet>("%Mallet");
		Mole = GetNode<Mole>("%Mole");
		GameOverMenu = GetNode<Control>("%GameOver");
		PlayResume = GetNode<Button>("%PlayResume");
		ComboCointTimer = GetNode<Timer>("%ComboCoinTimer");
		PopOutTimer = GetNode<Timer>("%PopOutTimer");

		Coin = ResourceLoader.Load<PackedScene>("scenes/Coin.tscn");
		Heart = ResourceLoader.Load<PackedScene>("scenes/HeartContainer.tscn");
		EarlyPop = ResourceLoader.Load<PackedScene>("scenes/EarlyPop.tscn");

		CamPlayPos = new Vector3(0, 2.403f, -9.956f);
		CamPlayRot = new Vector3(-28.7f, 0, 0);
		CamPlayFOV = 55f;

		CamMenuPos = new Vector3(-3.25f, 1.53f, -9.841f); //old menu
		CamMenuRot = new Vector3(0, 90, 0);             //old menu
		CamMenuFOV = 80f;

		PlayResume.Text = "Play";
		MainCam.Position = CamMenuPos;

		Mole.CurrentGameState = Mole.GameState.Paused;

		Collectables[0] = Coin;
		Collectables[1] = Heart;
		Collectables[2] = EarlyPop;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("menu"))
		{
			if (!CameraMoving)
			{
				ToggleMenu();
			}
		}

		if (Mole.CurrentGameState == Mole.GameState.GameOver)
		{
			HandleGameOver();
		}
	}

	public async void MoveCamera(Vector3 Pos, Vector3 Rot, float CamFOV)
	{
		CameraMoving = true;
		Tween MoveCam = GetTree().CreateTween();
		MoveCam.TweenProperty(MainCam, "position", Pos, 1.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		Tween RotCam = GetTree().CreateTween();
		RotCam.TweenProperty(MainCam, "rotation_degrees", Rot, 1.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		Tween FovCam = GetTree().CreateTween();
		FovCam.TweenProperty(MainCam, "fov", CamFOV, 1.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
		CameraMoving = false;
	}


	public void _on_combo_coin_timer_timeout()
	{
		//Generates random number from 0 to 100, this is then used to determine whether or not a regular coin or an extra life coin will be spawned.
		//Extra life coin is approx 1 in 10.

		int coinFavour = RNG.RandiRange(0, 100);
		Node sceneInstance;

		//Extra life only has a chance to spawn if your lives are below 3.
		if (Mole.GetLives() < 3)
		{
			if (coinFavour > 10)
			{
				sceneInstance = Collectables[PickRandomPickup()].Instantiate<Area3D>();
			}
			else
			{
				sceneInstance = Collectables[1].Instantiate<Area3D>();
			}
		}
		else
		{
			sceneInstance = Collectables[PickRandomPickup()].Instantiate<Area3D>();
		}
		if (Mole.CurrentGameState == Mole.GameState.Playing)
		{
			// GD.Print(Mole.CurrentGameState);
			AddChild(sceneInstance);
			ComboCointTimer.Start(CollectableSpawnTimes[RNG.RandiRange(0, CollectableSpawnTimes.Length - 1)]);
		}
	}
	int PickRandomPickup()
	{
		int secondCoinFavour = RNG.RandiRange(0, 100);
		int randomIndex;
		if (secondCoinFavour <= 60)
		{
			randomIndex = 0;
		}
		else if (Mole.EarlyPops < 1)
		{
			randomIndex = 2;
		}
		else
		{
			randomIndex = 0;
		}
		return randomIndex;
	}

	private async void ToggleMenu()
	{
		switch (currentState)
		{
			case GameState.MENU:
				SaveManager.SaveConfig();
				ToggleMenuVisibility(true, false, false, false, false, false);
				break;
			case GameState.PLAYING:
				SaveManager.SaveConfig();
				if (!MenuJustPressed)
				{
					MenuJustPressed = true;
					PopOutTimer.Paused = false;
					Mole.CurrentGameState = Mole.GameState.Paused;
					MoveToMenuState(GameState.PAUSED);
					await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
					MainMenu.Show();
					MenuJustPressed = false;
				}
				break;

			case GameState.PAUSED:
				SaveManager.SaveConfig();
				if (!MenuJustPressed)
				{
					MenuJustPressed = true;
					MoveToMenuState(GameState.PLAYING);
					MainMenu.Hide();
					await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
					PopOutTimer.Paused = false;
					Mole.CurrentGameState = Mole.GameState.Playing;
					MenuJustPressed = false;
				}
				break;
		}
	}

	// Method to transition to a new game state
	private void MoveToMenuState(GameState newState)
	{
		if (currentState != newState)
		{
			switch (newState)
			{
				case GameState.MENU:
					MoveCamera(CamMenuPos, CamMenuRot, CamMenuFOV);
					break;

				case GameState.PLAYING:
					MoveCamera(CamPlayPos, CamPlayRot, CamPlayFOV);
					break;
				case GameState.PAUSED:
					MoveCamera(CamMenuPos, CamMenuRot, CamMenuFOV);
					break;
			}
			currentState = newState;
		}
	}

	// Method to handle game over
	private async void HandleGameOver()
	{
		if (Mole.CurrentGameState == Mole.GameState.GameOver)
		{
			GameOverMenu.Show();
			Tween saturationTween = CreateTween();
			saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 0, 1f)
						  .SetTrans(Tween.TransitionType.Quad)
						  .SetEase(Tween.EaseType.Out);
			saturationTween.Play();


			if (currentState == GameState.PAUSED)
			{
				PopOutTimer.Paused = false;
				Mole.CurrentGameState = Mole.GameState.Paused;
			}

			saturationTween.Stop();
			saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 1, 1f)
						  .SetTrans(Tween.TransitionType.Expo)
						  .SetEase(Tween.EaseType.Out);
			saturationTween.Play();
			await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
			saturationTween.Stop();
		}
	}
	public void ToggleMenuVisibility(bool menuButtonsVisible, bool settingsMenuVisible, bool helpMenuVisible, bool accountMenuVisible, bool loginScreenVisible, bool registrationScreenVisible)
	{
		MenuButtons.Visible = menuButtonsVisible;
		SettingsMenu.Visible = settingsMenuVisible;
		HelpMenu.Visible = helpMenuVisible;
		AccountMenu.Visible = accountMenuVisible;
		LoginScreen.Visible = loginScreenVisible;
		RegistrationScreen.Visible = registrationScreenVisible;
	}


	public void _on_restart_pressed()
	{
		// GD.Print(SaveManager.SaveScore(User.Username, Mole.GetHighScore(), Mole.GetHighestCombo()));
		MoveToMenuState(GameState.PLAYING);
		Mole.CurrentGameState = Mole.GameState.Playing;
		Mole.Restart();
		ComboCointTimer.Start(RNG.RandiRange(3, 6));
		// Mole.SetGameOver(false);
		// Mole.Paused = false;
		GameOverMenu.Hide();
	}

	public void _on_main_menu_pressed()
	{
		// GD.Print(SaveManager.SaveScore(User.Username, Mole.GetHighScore(), Mole.GetHighestCombo()));
		MoveToMenuState(GameState.MENU);
		Mole.CurrentGameState = Mole.GameState.Paused;
		Mole.Restart();
		GameOverMenu.Hide();
		MainMenu.Show();
		PlayResume.Text = "Play";
	}

	//play/resume button
	public async void _on_button_pressed()
	{
		if (Mole.CurrentGameState != Mole.GameState.Paused)
		{
			Mole.Restart();
			ComboCointTimer.Start(RNG.RandiRange(3, 6));
			Mole.CurrentGameState = Mole.GameState.Playing;
		}
		MainMenu.Hide();
		ComboCointTimer.Start(RNG.RandiRange(3, 6));
		MoveToMenuState(GameState.PLAYING);
		await ToSignal(GetTree().CreateTimer(2f), "timeout");
		Mole.CurrentGameState = Mole.GameState.Playing;
	}

	public void _on_settings_pressed()
	{
		ToggleMenuVisibility(false, true, false, false, false, false);
	}

	public void _on_settings_back_pressed()
	{
		SaveManager.SaveConfig();
		ToggleMenuVisibility(true, false, false, false, false, false);
	}

	public void _on_help_pressed()
	{
		ToggleMenuVisibility(false, false, true, false, false, false);
	}

	public void _on_back_help_pressed()
	{
		ToggleMenuVisibility(true, false, false, false, false, false);
	}

	public void _on_account_pressed()
	{
		ToggleMenuVisibility(false, false, false, true, true, false);
	}

	public void _on_go_to_reg_page_pressed()
	{
		ToggleMenuVisibility(false, false, false, true, true, true);
	}

	public void _on_go_to_login_pressed()
	{
		ToggleMenuVisibility(false, false, false, true, true, false);
	}

	public void _on_as_guest_pressed()
	{
		ToggleMenuVisibility(true, false, false, false, false, false);
	}
	public void _on_exit_pressed()
	{
		SaveManager.SaveConfig();
		GetTree().Quit();
	}


}

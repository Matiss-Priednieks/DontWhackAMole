using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Linq;

public partial class Game : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public enum GameState
	{
		MENU,
		PLAYING,
		PAUSED,
		SHOP
	}
	public enum GameMode
	{
		STORY,
		ARCADE
	}

	public struct UIVisibilityState
	{
		public bool MenuButtonsVisible;
		public bool SettingsMenuVisible;
		public bool HelpMenuVisible;
		public bool AccountMenuVisible;
		public bool LoginScreenVisible;
		public bool RegistrationScreenVisible;
		public bool PowerUpUIVisible;
		public bool ShopVisible;
		public bool LeaderboardVisible;
	}
	GameMode CurrentGameMode = GameMode.ARCADE;
	private GameState currentState = GameState.MENU;
	float MenuDelay = 0.5f;
	float CamPlayFOV;
	float CamMenuFOV;
	Camera3D MainCam;
	Vector3 CamPlayPos, CamPlayRot, CamMenuPos, CamMenuRot, CamShopPos, CamShopRot;
	RandomNumberGenerator RNG;
	SaveManager SaveManager;
	LoggedInUser User;
	Mallet Mallet;
	Mole Mole;
	Control MainMenu;
	Control GameOverMenu;
	Button PlayResume;
	PanelContainer MenuButtons, HelpMenu, SettingsMenu, AccountMenu, ShopVisible;
	Panel LoginScreen, RegistrationScreen;
	Timer ComboCointTimer;

	WorldEnvironment worldEnvironment;

	Timer PopOutTimer;
	MarginContainer PowerUpUI, LeaderboardUI;

	PackedScene Coin, Heart, EarlyPop;
	public bool Login, Register, CameraMoving = false;

	public bool MenuJustPressed { get; private set; }
	int[] CollectableSpawnTimes = { 4, 4, 6, 8, 8, 8, 16 };

	PackedScene[] Collectables;

	UIVisibilityState MenuUIState, PlayingUIState, ShopUIState, SettingsUIState, LoginUIState, RegistrationUIState, HelpUIState, AccountUIState;

	public override void _Ready()
	{
		PowerUpUI = GetNode<MarginContainer>("%Powerup");
		Collectables = new PackedScene[3];
		worldEnvironment = GetNode<WorldEnvironment>("%WorldEnvironment");
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		User.LoginInitialisation();
		SaveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");

		LeaderboardUI = GetNode<MarginContainer>("UI/Menu/%Leaderboard");

		LoginScreen = GetNode<Panel>("%LoginScreen");
		ShopVisible = GetNode<PanelContainer>("%ShopMenu");
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

		CamMenuPos = new Vector3(-3.25f, 1.63f, -9.841f); //old menu
		CamMenuRot = new Vector3(0, 90, 0);             //old menu
		CamMenuFOV = 80f;

		CamShopPos = new Vector3(3.5f, 1.5f, -7.1f);
		CamShopRot = new Vector3(0, -91.1f, 0);

		PlayResume.Text = "Play";
		MainCam.Position = CamMenuPos;

		Mole.CurrentGameState = Mole.GameState.Paused;

		Collectables[0] = Coin;
		Collectables[1] = Heart;
		Collectables[2] = EarlyPop;
		SaveManager.PostTitleScreen = true;
		SaveManager.LoadConfig();

		MenuUIState = new UIVisibilityState
		{
			MenuButtonsVisible = true,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = false,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = true
		};
		PlayingUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = false,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = true,
			ShopVisible = false,
			LeaderboardVisible = false
		};
		ShopUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = false,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = true,
			LeaderboardVisible = false
		};
		SettingsUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = true,
			HelpMenuVisible = false,
			AccountMenuVisible = false,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = false
		};
		LoginUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = true,
			LoginScreenVisible = true,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = false
		};
		RegistrationUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = true,
			LoginScreenVisible = false,
			RegistrationScreenVisible = true,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = false
		};
		HelpUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = true,
			AccountMenuVisible = false,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = true
		};
		AccountUIState = new UIVisibilityState
		{
			MenuButtonsVisible = false,
			SettingsMenuVisible = false,
			HelpMenuVisible = false,
			AccountMenuVisible = true,
			LoginScreenVisible = false,
			RegistrationScreenVisible = false,
			PowerUpUIVisible = false,
			ShopVisible = false,
			LeaderboardVisible = false
		};
		ToggleMenuVisibility(MenuUIState);
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
		else
		{
			GameOverMenu.Hide();
			Tween saturationTween = CreateTween();
			saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 1, 1f)
						  .SetTrans(Tween.TransitionType.Quad)
						  .SetEase(Tween.EaseType.Out);
			saturationTween.Play();
		}
	}

	public async void MoveCamera(Vector3 Pos, Vector3 Rot, float CamFOV)
	{
		CameraMoving = true;
		Tween MoveCam = GetTree().CreateTween();
		MoveCam.TweenProperty(MainCam, "position", Pos, MenuDelay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		Tween RotCam = GetTree().CreateTween();
		RotCam.TweenProperty(MainCam, "rotation_degrees", Rot, MenuDelay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		Tween FovCam = GetTree().CreateTween();
		FovCam.TweenProperty(MainCam, "fov", CamFOV, MenuDelay).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		await ToSignal(GetTree().CreateTimer(MenuDelay), "timeout");
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
			// CallDeferred(Node3D.MethodName.AddChild, sceneInstance); //neat tidbit
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
				ToggleMenuVisibility(MenuUIState);
				break;
			case GameState.PLAYING:
				SaveManager.SaveConfig();

				ToggleMenuVisibility(MenuUIState);

				if (!MenuJustPressed)
				{
					MenuJustPressed = true;
					PopOutTimer.Paused = false;
					Mole.CurrentGameState = Mole.GameState.Paused;
					MoveToMenuState(GameState.PAUSED);
					await ToSignal(GetTree().CreateTimer(MenuDelay), "timeout");
					MainMenu.Show();
					MenuJustPressed = false;
				}
				break;

			case GameState.PAUSED:
				SaveManager.SaveConfig();

				ToggleMenuVisibility(PlayingUIState);
				if (!MenuJustPressed)
				{
					MenuJustPressed = true;
					MoveToMenuState(GameState.PLAYING);
					MainMenu.Hide();
					await ToSignal(GetTree().CreateTimer(MenuDelay), "timeout");
					PopOutTimer.Paused = false;
					Mole.CurrentGameState = Mole.GameState.Playing;
					MenuJustPressed = false;
				}
				break;
			case GameState.SHOP:
				SaveManager.SaveConfig();

				ToggleMenuVisibility(MenuUIState);

				if (!MenuJustPressed)
				{
					MenuJustPressed = true;
					PopOutTimer.Paused = false;
					Mole.CurrentGameState = Mole.GameState.Paused;
					MoveToMenuState(GameState.MENU);
					await ToSignal(GetTree().CreateTimer(MenuDelay), "timeout");
					MainMenu.Show();
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
				case GameState.SHOP:
					MoveCamera(CamShopPos, CamShopRot, CamMenuFOV);
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
			await ToSignal(GetTree().CreateTimer(MenuDelay), "timeout");
			saturationTween.Stop();
		}

	}


	private void ToggleMenuVisibility(UIVisibilityState state)
	{
		MenuButtons.Visible = state.MenuButtonsVisible;
		SettingsMenu.Visible = state.SettingsMenuVisible;
		HelpMenu.Visible = state.HelpMenuVisible;
		AccountMenu.Visible = state.AccountMenuVisible;
		LoginScreen.Visible = state.LoginScreenVisible;
		RegistrationScreen.Visible = state.RegistrationScreenVisible;
		PowerUpUI.Visible = state.PowerUpUIVisible;
		ShopVisible.Visible = state.ShopVisible;
		LeaderboardUI.Visible = state.LeaderboardVisible;
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
		ToggleMenuVisibility(PlayingUIState);

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
		ToggleMenuVisibility(MenuUIState);
	}
	public void _on_shop_pressed()
	{
		MoveToMenuState(GameState.SHOP);
		User.GetUnlockedContentRequest();
		Mole.CurrentGameState = Mole.GameState.Paused;
		GameOverMenu.Hide();
		MainMenu.Show();
		ToggleMenuVisibility(ShopUIState);
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
		await ToSignal(GetTree().CreateTimer(MenuDelay + 1f), "timeout"); //how long it takes for gameplay to resume (default 1.5s)
		Mole.CurrentGameState = Mole.GameState.Playing;
		ToggleMenuVisibility(PlayingUIState);
	}

	public void _on_settings_pressed()
	{
		ToggleMenuVisibility(SettingsUIState);
	}

	public void _on_back_pressed()
	{
		User.GetUnlockedContentRequest();
		SaveManager.SaveConfig();
		MoveToMenuState(GameState.MENU);
		ToggleMenuVisibility(MenuUIState);
	}

	public void _on_help_pressed()
	{
		ToggleMenuVisibility(HelpUIState);
	}

	public void _on_account_pressed()
	{
		if (User.LoggedIn)
		{
			ToggleMenuVisibility(AccountUIState);
		}
		else
		{
			ToggleMenuVisibility(LoginUIState);
		}
	}

	public void _on_go_to_reg_page_pressed()
	{
		ToggleMenuVisibility(RegistrationUIState);
	}

	public void _on_go_to_login_pressed()
	{
		ToggleMenuVisibility(LoginUIState);
	}

	public void _on_as_guest_pressed()
	{
		ToggleMenuVisibility(MenuUIState);
	}
	public void _on_exit_pressed()
	{
		SaveManager.SaveConfig();
		GetTree().Quit();
	}

	public void _on_unlocks_request_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200 || responseCode == 201)
		{
			GD.Print("Successfully bought!");
		}
	}
	public void _on_unlocks_info_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200 || responseCode == 201)
		{
			Json json = new();
			json.Parse(body.GetStringFromUtf8());

			var unlocks = (Dictionary<string, Dictionary<string, bool>>)json.Data;
			var coins = (Dictionary<string, int>)json.Data;

			var unlockDictionary = unlocks["unlocks"];
			User.AccountCurrency = coins["collected_coins"];

			User.SetUnlocksDict(unlockDictionary);

			GD.Print("unlocks print in unlocks request: " + unlockDictionary);

			User.FinalBuyCheck();
		}

	}

	public void _on_currency_update_request_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200 || responseCode == 201)
		{
			GD.Print("curreny update attempted");
		}
		else
		{
			GD.Print("failed");
		}
	}

	public void _on_buy_attempt_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200 || responseCode == 201)
		{
			GD.Print("Buy request 4");
			Json json = new();
			json.Parse(body.GetStringFromUtf8());

			var unlocks = (Dictionary<string, Dictionary<string, bool>>)json.Data;
			// var coins = (Dictionary<string, int>)json.Data;

			var unlockDictionary = unlocks["unlocks"];

			User.SetUnlocksDict(unlockDictionary);
			GD.Print("\n" + unlockDictionary + "\n");
			User.PurchasedItemUpdate(unlockDictionary);
		}
		else
		{
			GD.Print(responseCode);
		}
	}
	public void _on_get_highscore_request_completed(long result, long responseCode, string[] headers, byte[] body)
	{
		if (responseCode == 200 || responseCode == 201)
		{

			Json json = new();
			json.Parse(body.GetStringFromUtf8());

			GD.Print("Raw JSON data " + json.Data.ToString());
			var highscores = (Dictionary<string, int[]>)json.Data;
			// GD.Print(highscores["highscore"]);
			if (User.LoggedIn)
			{
				if (highscores["highscore"].IsEmpty())
				{
					User.SetHighscore(0);
					Mole.SetHighScore(0);
				}
				else
				{
					User.SetHighscore(highscores["highscore"][0]);
					Mole.SetHighScore(highscores["highscore"][0]);
				}
			}


		}
		else
		{
			GD.Print(responseCode);
		}
	}
}

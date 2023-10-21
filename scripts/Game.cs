using Godot;
using System;

public partial class Game : Node3D
{
	// Called when the node enters the scene tree for the first time.
	bool Menu = true;
	bool Playing = false;
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


	Godot.WorldEnvironment worldEnvironment;

	Timer PopOutTimer;

	PackedScene Coin;
	public bool Login, Register = false;

	public bool MenuJustPressed { get; private set; }

	public override void _Ready()
	{
		worldEnvironment = GetNode<Godot.WorldEnvironment>("%WorldEnvironment");
		User = GetNode<LoggedInUser>("/root/LoggedInUser");
		User.LoggedInFakeReady();
		this.SaveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
		this.SaveManager.LoadConfig();

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

		CamPlayPos = new Vector3(0, 2.403f, 1.516f);
		CamPlayRot = new Vector3(-36.8f, 0, 0);

		CamMenuPos = new Vector3(-3.25f, 1.53f, -0.9f); //old menu
		CamMenuRot = new Vector3(0, 90, 0);             //old menu
		PlayResume.Text = "Play";
		MainCam.Position = CamMenuPos;

		Mole.Paused = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public async override void _Process(double delta)
	{
		if (Input.IsActionJustReleased("menu") && !Menu && !Mole.GetGameOver())
		{
			this.SaveManager.SaveConfig();
			if (!MenuJustPressed)
			{
				MenuJustPressed = true;
				MoveCamera(CamMenuPos, CamMenuRot);
				PlayResume.Text = "Resume";
				Menu = true;
				Mole.Paused = true;
				PopOutTimer.Paused = true;
				await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
				MainMenu.Show();
				MenuJustPressed = false;
			}
		}
		else if (Input.IsActionJustReleased("menu") && Menu)
		{
			this.SaveManager.SaveConfig();
			if (!MenuJustPressed)
			{
				MenuJustPressed = true;
				MainMenu.Hide();
				MoveCamera(CamPlayPos, CamPlayRot);
				Menu = false;
				await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
				PopOutTimer.Paused = false;
				Mole.Paused = false;
				MenuJustPressed = false;
			}
		}

		if (Mole.GetGameOver())
		{
			GameOverMenu.Show();
			Menu = false;
			Tween saturationTween = CreateTween();
			saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 0, 1f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			saturationTween.Play();
			if (Input.IsActionJustReleased("menu"))
			{
				this.SaveManager.SaveConfig();
				Mole.SetGameOver(false);
				Mole.Playing = false;
				GameOverMenu.Hide();
				MoveCamera(CamMenuPos, CamMenuRot);
				PlayResume.Text = "Play";
				await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
				MainMenu.Show();
				Menu = true;
				// Tween saturationTween = CreateTween();
				saturationTween.Stop();
				saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 1, 1f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
				saturationTween.Play();
			}
			await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
			saturationTween.Stop();
		}
	}

	public void _on_restart_pressed()
	{
		Tween saturationTween = CreateTween();
		saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 1, 1f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		saturationTween.Play();
		Mole.Playing = true;
		Mole.Restart();
		ComboCointTimer.Start(RNG.RandiRange(3, 6));
		Mole.SetGameOver(false);
		Mole.Paused = false;
		GameOverMenu.Hide();
		Menu = false;
	}
	public void _on_main_menu_pressed()
	{
		Tween saturationTween = CreateTween();
		saturationTween.TweenProperty(worldEnvironment.Environment, "adjustment_saturation", 1, 1f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		saturationTween.Play();
		Mole.SetGameOver(false);
		Mole.Playing = false;
		Mole.Restart();
		GameOverMenu.Hide();
		MainMenu.Show();
		MoveCamera(CamMenuPos, CamMenuRot);
		PlayResume.Text = "Play";
		Menu = true;
	}

	//play/resume button
	public async void _on_button_pressed()
	{
		if (!Mole.Paused)
		{
			Mole.Restart();
			ComboCointTimer.Start(RNG.RandiRange(3, 6));

			Mole.Paused = false;
		}
		MainMenu.Hide();
		ComboCointTimer.Start(RNG.RandiRange(3, 6));
		MoveCamera(CamPlayPos, CamPlayRot);
		await ToSignal(GetTree().CreateTimer(2f), "timeout");
		Mole.Playing = true;
		Mole.Paused = false;
		Menu = false;
	}
	public void _on_settings_pressed()
	{
		SettingsMenu.Show();
		MenuButtons.Hide();
	}

	public void _on_settings_back_pressed()
	{
		this.SaveManager.SaveConfig();
		SettingsMenu.Hide();
		MenuButtons.Show();
	}

	public void _on_help_pressed()
	{
		MenuButtons.Hide();
		HelpMenu.Show();
	}
	public void _on_back_help_pressed()
	{
		MenuButtons.Show();
		HelpMenu.Hide();
	}

	public void MoveCamera(Vector3 Pos, Vector3 Rot)
	{
		Tween MoveCam = GetTree().CreateTween();
		MoveCam.TweenProperty(MainCam, "position", Pos, 1.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		Tween RotCam = GetTree().CreateTween();
		RotCam.TweenProperty(MainCam, "rotation_degrees", Rot, 1.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
	}

	public void _on_exit_pressed()
	{
		this.SaveManager.SaveConfig();
		GetTree().Quit();
	}

	public void _on_combo_coin_timer_timeout()
	{
		var coinInstance = Coin.Instantiate<Area3D>();
		AddChild(coinInstance);
		ComboCointTimer.Start(4);
	}
	public void _on_account_pressed()
	{
		MenuButtons.Hide();
		SettingsMenu.Hide();
		HelpMenu.Hide();
		AccountMenu.Show();
	}

	public void _on_go_to_reg_page_pressed()
	{
		MenuButtons.Hide();
		SettingsMenu.Hide();
		HelpMenu.Hide();
		LoginScreen.Hide();
		AccountMenu.Show();
		RegistrationScreen.Show();
	}
	public void _on_go_to_login_pressed()
	{
		MenuButtons.Hide();
		SettingsMenu.Hide();
		HelpMenu.Hide();
		RegistrationScreen.Hide();
		AccountMenu.Show();
		LoginScreen.Show();
	}
	public void _on_as_guest_pressed()
	{
		MenuButtons.Show();
		SettingsMenu.Hide();
		HelpMenu.Hide();
		AccountMenu.Hide();
	}
}

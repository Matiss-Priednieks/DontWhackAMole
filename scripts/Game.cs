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

    Mallet Mallet;
    Mole Mole;
    Panel MainMenu;
    Control GameOverMenu;
    Button PlayResume;
    VBoxContainer MenuButtons, HelpMenu, SettingsMenu;
    Timer ComboCointTimer;
    PackedScene Coin;

    public override void _Ready()
    {
        this.SaveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        this.SaveManager.LoadConfig();

        RNG = new RandomNumberGenerator();
        MenuButtons = GetNode<VBoxContainer>("%MenuButtons");
        HelpMenu = GetNode<VBoxContainer>("%HelpScreen");
        SettingsMenu = GetNode<VBoxContainer>("%SettingsMenu");
        MainMenu = GetNode<Panel>("%Intro");
        MainCam = GetNode<Camera3D>("Camera3D");
        Mallet = GetNode<Mallet>("%Mallet");
        Mole = GetNode<Mole>("%Mole");
        GameOverMenu = GetNode<Control>("%GameOver");
        PlayResume = GetNode<Button>("%PlayResume");
        ComboCointTimer = GetNode<Timer>("%ComboCoinTimer");

        Coin = ResourceLoader.Load<PackedScene>("scenes/Coin.tscn");

        CamPlayPos = new Vector3(0, 2.403f, 1.516f);
        CamPlayRot = new Vector3(-36.8f, 0, 0);

        // CamMenuPos = new Vector3(0, 1.621f, 0.667f); //old menu
        // CamMenuRot = new Vector3(0, 30, 0);         //old menu
        CamMenuPos = new Vector3(-3.25f, 1.53f, -0.9f); //old menu
        CamMenuRot = new Vector3(0, 90, 0);         //old menu
        PlayResume.Text = "Play";
        MainCam.Position = CamMenuPos;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _Process(double delta)
    {
        if (Input.IsActionJustReleased("menu") && !Menu && !Mole.GetGameOver())
        {
            MoveCamera(CamMenuPos, CamMenuRot);
            Mole.Paused = true;
            PlayResume.Text = "Resume";
            await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
            MainMenu.Show();
            Menu = true;
        }
        else if (Input.IsActionJustReleased("menu") && Menu)
        {
            MainMenu.Hide();
            MoveCamera(CamPlayPos, CamPlayRot);
            await ToSignal(GetTree().CreateTimer(2.5f), "timeout");
            Mole.Paused = false;
            Menu = false;
        }

        if (Mole.GetGameOver())
        {
            GameOverMenu.Show();
            Menu = false;
            if (Input.IsActionJustReleased("menu"))
            {
                Mole.SetGameOver(false);
                // Mole.Paused = false;
                Mole.Playing = false;
                GameOverMenu.Hide();
                MoveCamera(CamMenuPos, CamMenuRot);
                PlayResume.Text = "Play";
                await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
                MainMenu.Show();
                Menu = true;
            }
        }
    }

    public void _on_restart_pressed()
    {
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
        SettingsMenu.Hide();
        MenuButtons.Show();
        this.SaveManager.SaveConfig();
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
        GetTree().Quit();
    }

    public void _on_combo_coin_timer_timeout()
    {
        var coinInstance = Coin.Instantiate<Area3D>();
        AddChild(coinInstance);
        // ComboCointTimer.Start(RNG.RandiRange(1, 2) * 2);
        ComboCointTimer.Start(4);
    }
}

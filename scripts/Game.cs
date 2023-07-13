using Godot;
using System;

public partial class Game : Node3D
{
    // Called when the node enters the scene tree for the first time.
    bool Menu = true;
    bool Playing = false;
    Camera3D MainCam;
    Vector3 CamPlayPos, CamPlayRot, CamMenuPos, CamMenuRot;

    Mallet Mallet;
    Mole Mole;
    Panel Intro;
    Control GameOverMenu, InGameUI;
    Button PlayResume;
    VBoxContainer MenuButtons, HelpMenu, SettingsMenu;
    public override void _Ready()
    {
        MenuButtons = GetNode<VBoxContainer>("%MenuButtons");
        HelpMenu = GetNode<VBoxContainer>("%HelpScreen");
        SettingsMenu = GetNode<VBoxContainer>("%SettingsMenu");
        Intro = GetNode<Panel>("%Intro");
        MainCam = GetNode<Camera3D>("Camera3D");
        Mallet = GetNode<Mallet>("%Mallet");
        Mole = GetNode<Mole>("%Mole");
        GameOverMenu = GetNode<Control>("%GameOver");
        InGameUI = GetNode<Control>("%InGameUI");
        PlayResume = GetNode<Button>("%PlayResume");


        CamPlayPos = new Vector3(0, 2.403f, 1.516f);
        CamPlayRot = new Vector3(-36.8f, 0, 0);

        // CamMenuPos = new Vector3(0, 1.621f, 0.667f); //old menu
        // CamMenuRot = new Vector3(0, 30, 0);         //old menu
        CamMenuPos = new Vector3(-3.25f, 1.53f, -0.9f); //old menu
        CamMenuRot = new Vector3(0, 90, 0);         //old menu

        InGameUI.Hide();
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
            InGameUI.Hide();
            await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
            Intro.Show();
            Menu = true;
        }
        else if (Input.IsActionJustReleased("menu") && Menu)
        {
            Intro.Hide();
            MoveCamera(CamPlayPos, CamPlayRot);
            await ToSignal(GetTree().CreateTimer(2.5f), "timeout");
            Mole.Paused = false;
            InGameUI.Show();
            Menu = false;
        }

        if (Mole.GetGameOver())
        {
            GameOverMenu.Show();
            Menu = false;
            GD.Print(Input.IsActionJustReleased("menu"));
            if (Input.IsActionJustReleased("menu"))
            {
                Mole.SetGameOver(false);
                Mole.Paused = true;
                GameOverMenu.Hide();
                Intro.Show();
                MoveCamera(CamMenuPos, CamMenuRot);
                PlayResume.Text = "Play";
                InGameUI.Hide();
                Menu = true;
            }
        }
    }
    public void _on_h_slider_value_changed(float value)
    {
        var busIndex = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusVolumeDb(busIndex, value);
    }
    public void _on_restart_pressed()
    {
        Mole.Playing = true;
        Mallet.Playing = true;
        Mole.Restart();

        Mole.SetGameOver(false);
        Mole.Paused = false;
        GameOverMenu.Hide();
        InGameUI.Show();
        Menu = false;
    }
    public void _on_main_menu_pressed()
    {
        Mole.SetGameOver(false);
        Mole.Playing = false;
        Mole.Restart();
        GameOverMenu.Hide();
        Intro.Show();
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
            Mole.Paused = false;
        }
        InGameUI.Show();
        Intro.Hide();
        MoveCamera(CamPlayPos, CamPlayRot);
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        Mole.Playing = true;
        Mallet.Playing = true;
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
}

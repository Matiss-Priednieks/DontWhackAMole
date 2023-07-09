using Godot;
using System;

public partial class Game : Node3D
{
    // Called when the node enters the scene tree for the first time.
    bool Menu = true;
    bool Playing = false;
    Camera3D MainCam;
    Vector3 CamPlayPos, CamPlayRot, CamMenuPos, CamMenuRot;

    Mallet mallet;
    Mole mole;
    Panel Intro;

    public override void _Ready()
    {
        Intro = GetNode<Panel>("%Intro");
        MainCam = GetNode<Camera3D>("Camera3D");
        mallet = GetNode<Mallet>("%Mallet");
        mole = GetNode<Mole>("%Mole");

        CamPlayPos = new Vector3(0, 2.403f, 1.516f);
        CamMenuPos = new Vector3(0, 1.768f, -0.108f);

        CamPlayRot = new Vector3(-36.8f, 0, 0);
        CamMenuRot = Vector3.Zero;

        MainCam.Position = CamMenuPos;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public async override void _Process(double delta)
    {
        if (Input.IsActionJustReleased("ui_accept") && Menu)
        {
            Intro.Hide();
            Tween MoveCam = GetTree().CreateTween();
            MoveCam.TweenProperty(MainCam, "position", CamPlayPos, 3f).SetTrans(Tween.TransitionType.Circ);
            Tween RotCam = GetTree().CreateTween();
            RotCam.TweenProperty(MainCam, "rotation_degrees", CamPlayRot, 3f).SetTrans(Tween.TransitionType.Circ);
            await ToSignal(GetTree().CreateTimer(2f), "timeout");
            mole.Playing = true;
            mallet.Playing = true;
        }
    }
}

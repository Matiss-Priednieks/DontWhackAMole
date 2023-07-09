using Godot;
using System;

public partial class MainCam : Camera3D
{
    Vector3 CamPlayPos, CamPlayRot, CamMenuPos, CamMenuRot;

    public override void _Ready()
    {
        CamPlayPos = new Vector3(0, 1.969f, 0.932f);
        CamMenuPos = new Vector3(0, 1.969f, 1.597f);

        CamPlayRot = new Vector3(-36.8f, 0, 0);
        CamMenuRot = Vector3.Zero;
        //xrot = -36.8
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}

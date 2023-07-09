using Godot;
using System;

public partial class Hole : MeshInstance3D
{
    public void Flash(bool value)
    {
        var mat = (ShaderMaterial)Mesh.SurfaceGetMaterial(0).NextPass;
        mat.SetShaderParameter("Flashing", value);
    }
}

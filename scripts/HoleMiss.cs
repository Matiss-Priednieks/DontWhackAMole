using Godot;
using System;

public partial class HoleMiss : Area3D
{
    // Called when the node enters the scene tree for the first time.
    PackedScene SmokeScene;
    public override void _Ready()
    {
        SmokeScene = ResourceLoader.Load<PackedScene>("scenes/smoke_particles.tscn");
    }

    public void _on_area_entered(Area3D area)
    {
        if (area is Mallet mallet)
        {
            var smokeInstance = SmokeScene.Instantiate<GpuParticles3D>();
            AddChild(smokeInstance);
            smokeInstance.Emitting = true;
        }
    }
}

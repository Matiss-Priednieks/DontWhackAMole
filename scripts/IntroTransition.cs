using Godot;
using System;

public partial class IntroTransition : CanvasLayer
{
    public float ScreenAlpha { get; private set; }

    // Called when the node enters the scene tree for the first time.
    Color colour;
    ColorRect colorRect;
    public override void _Ready()
    {
        ScreenAlpha = 1;
        colorRect = GetNode<ColorRect>("BlackBox");
        colorRect.Show();
        FadeFirstScreen();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        colour = new Color(0.04f, 0.04f, 0.04f, ScreenAlpha);
        colorRect.Color = colour;
    }
    public async void FadeFirstScreen()
    {
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        FadeIn();
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        colorRect.Hide();
    }


    public void FadeIn()
    {
        Tween tempTween = CreateTween();
        tempTween.TweenProperty(this, "ScreenAlpha", 0, 2).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);

    }
    public void _on_black_box_gui_input(InputEvent _event)
    {

    }

}

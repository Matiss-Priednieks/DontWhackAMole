using Godot;
using System;

public partial class BootSplash : CanvasLayer
{
    public float ScreenAlpha { get; private set; }
    bool Fading;
    PackedScene MainGameScene;
    // Called when the node enters the scene tree for the first time.
    Color colour;
    ColorRect colorRect;
    SaveManager SaveManager;
    public override void _Ready()
    {
        this.SaveManager = GetTree().Root.GetNode<SaveManager>("SaveManager");
        this.SaveManager.LoadConfig();
        ScreenAlpha = 1;
        FadeFirstScreen();
        GetNode<Control>("%FirstScreen").Show();
        GetNode<Control>("%SecondScreen").Hide();
        MainGameScene = ResourceLoader.Load<PackedScene>("scenes/MainGame.tscn");
        colorRect = GetNode<ColorRect>("BlackBox");

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
        FadeOut();
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
        GetNode<Control>("%FirstScreen").Hide();
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        GetNode<Control>("%SecondScreen").Show();
        FadeSecondScreen();
    }
    public async void FadeSecondScreen()
    {
        FadeIn();
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        FadeOut();
        await ToSignal(GetTree().CreateTimer(2f), "timeout");

        GetTree().ChangeSceneToPacked(MainGameScene);
    }

    public void FadeIn()
    {
        Tween tempTween = CreateTween();
        tempTween.TweenProperty(this, "ScreenAlpha", 0, 2).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
    }
    public void FadeOut()
    {
        Tween tempTween = CreateTween();
        tempTween.TweenProperty(this, "ScreenAlpha", 1, 2).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
    }
}

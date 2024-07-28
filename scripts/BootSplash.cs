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
	SceneTree _SceneTree;
	bool BannerDisplayed;
	public override void _Ready()
	{
		_SceneTree = GetTree();
		this.SaveManager = _SceneTree.Root.GetNode<SaveManager>("SaveManager");
		this.SaveManager.LoadConfig();
		ScreenAlpha = 1;
		FadeFirstScreen();
		GetNode<Control>("%FirstScreen").Show();
		GetNode<Control>("%SecondScreen").Hide();
		MainGameScene = ResourceLoader.Load<PackedScene>("scenes/MainGame.tscn");
		colorRect = GetNode<ColorRect>("BlackBox");
		var BusLayout = ResourceLoader.Load<AudioBusLayout>("resources/default_bus_layout.tres");
		AudioServer.SetBusLayout(BusLayout);
		FadeFirstScreen();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		colour = new Color(0.04f, 0.04f, 0.04f, ScreenAlpha);
		colorRect.Color = colour;

		if (Input.IsActionJustPressed("press") && BannerDisplayed)
		{
			_SceneTree.ChangeSceneToPacked(MainGameScene);
		}
	}
	public async void FadeFirstScreen()
	{
		await ToSignal(_SceneTree.CreateTimer(1f), "timeout");
		FadeIn();

		await ToSignal(_SceneTree.CreateTimer(2f), "timeout");
		FadeOut();
		BannerDisplayed = true;
		await ToSignal(_SceneTree.CreateTimer(1.5f), "timeout");
		GetNode<Control>("%FirstScreen").Hide();
		await ToSignal(_SceneTree.CreateTimer(2f), "timeout");
		GetNode<Control>("%SecondScreen").Show();
		FadeSecondScreen();
	}
	public async void FadeSecondScreen()
	{
		FadeIn();
		await ToSignal(_SceneTree.CreateTimer(2f), "timeout");
		FadeOut();
		await ToSignal(_SceneTree.CreateTimer(2f), "timeout");

		// GD.Print(MainGameScene + "\n");
		// GD.Print(_SceneTree);
		_SceneTree.ChangeSceneToPacked(MainGameScene);
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

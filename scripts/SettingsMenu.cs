using Godot;
using System;

public partial class SettingsMenu : VBoxContainer
{
    OptionButton ResOptions, WinOptions;
    Vector2I[] ResArray;
    Window.ModeEnum[] WinModeArray;
    Godot.Collections.Dictionary<string, Vector2I> Resolutions = new Godot.Collections.Dictionary<string, Vector2I>(){
            {"1280×720", new Vector2I(1280, 720)},
            {"1920x1080", new Vector2I(1920, 1080)},
            { "2560x1440", new Vector2I(2560, 1440)},
            { "3840x2160", new Vector2I(3840, 2160)}};

    Godot.Collections.Dictionary<string, Window.ModeEnum> WindowMode = new Godot.Collections.Dictionary<string, Window.ModeEnum>(){
        {"Fullscreen", Window.ModeEnum.Fullscreen},
        {"Windowed", Window.ModeEnum.Windowed}
    };
    public override void _Ready()
    {

        ResArray = new Vector2I[4];
        ResOptions = GetNode<OptionButton>("Resolution");
        int optionIndex = 0;
        foreach (var item in Resolutions)
        {
            ResOptions.AddItem(item.Key);
            if (item.Value == GetWindow().Size) ResOptions.Selected = optionIndex;
            optionIndex++;
        }
        Resolutions.Values.CopyTo(ResArray, 0);
        GetTree().Root.ContentScaleSize = ResArray[1];
        ResOptions.Selected = 1;

        WinModeArray = new Window.ModeEnum[2];
        WinOptions = GetNode<OptionButton>("WindowMode");
        int winOptionIndex = 0;

        // var values = Window.ModeEnum.GetValues(typeof(Window));
        foreach (var item in WindowMode)
        {
            WinOptions.AddItem(item.Key);
            // if (item.Value == Window.ModeEnum.Fullscreen) WinOptions.Selected = winOptionIndex;
            winOptionIndex++;
        }
        WindowMode.Values.CopyTo(WinModeArray, 0);
    }


    public void _on_resolution_item_selected(int index)
    {

        // GetTree().Root.ContentScaleAspect
        GetTree().Root.ContentScaleSize = ResArray[index];
        // GetWindow().Size = ResArray[index];
        // GetWindow().Mode = WinModeArray[0];
        // GetWindow().Scaling3DScale = index;
        // GetTree().SetScreenStretch(SceneTree.StretchMode.Viewport, SceneTree.StretchAspect.Keep, ResArray[index]);
    }
    public void _on_window_mode_item_selected(int index)
    {
        GetWindow().Mode = WinModeArray[index];
    }

}

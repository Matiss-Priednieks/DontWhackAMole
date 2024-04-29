using Godot;
using System;

public partial class SettingsMenu : VBoxContainer
{
    OptionButton ResOptions, WinOptions;
    Vector2I[] ResArray;
    Window.ModeEnum[] WinModeArray;
    Label WindowLabel, ResolutionLabel;
    OptionButton WindowOption, ResolutionOption;
    public enum CurrentOS
    {
        ANDROID,
        PC
    }
    CurrentOS currentOS = new CurrentOS();
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
        WindowLabel = GetNode<Label>("WindowModeLabel");
        ResolutionLabel = GetNode<Label>("ResolutionLabel");
        WindowOption = GetNode<OptionButton>("WindowMode");
        ResolutionOption = GetNode<OptionButton>("Resolution");

        OS.HasFeature("mobile");

        if (OS.HasFeature("mobile"))
        {
            currentOS = CurrentOS.ANDROID;
        }
        else if (OS.HasFeature("pc"))
        {
            currentOS = CurrentOS.PC;
        }

        ResArray = new Vector2I[4];
        ResOptions = GetNode<OptionButton>("Resolution");
        int optionIndex = 0;
        ResOptions.Selected = 1;
        foreach (var item in Resolutions)
        {
            ResOptions.AddItem(item.Key);
            if (item.Value == GetWindow().Size) ResOptions.Selected = optionIndex;
            optionIndex++;
        }
        Resolutions.Values.CopyTo(ResArray, 0);
        GetTree().Root.ContentScaleSize = ResArray[1];


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


        switch (currentOS)
        {
            case CurrentOS.ANDROID:
                //Hide settings only pc should see.
                WindowLabel.Hide();
                WindowOption.Hide();
                // ResolutionLabel.Hide();
                // ResolutionOption.Hide();
                break;
            case CurrentOS.PC:
                //show settings for pc build#
                WindowLabel.Show();
                WindowOption.Show();
                // ResolutionLabel.Show();
                // ResolutionOption.Show();
                break;
            default:
                GD.Print("Invalid operating system");
                break;
        }
    }
    public void _on_resolution_item_selected(int index)
    {
        GetTree().Root.ContentScaleSize = ResArray[index];
        // GD.Print(ResArray[index]);
    }
    public void _on_window_mode_item_selected(int index)
    {
        GetWindow().Mode = WinModeArray[index];
    }
    public void _on_h_slider_value_changed(float value)
    {
        var busIndex = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusVolumeDb(busIndex, value);
    }
    public void _on_music_slider_value_changed(float value)
    {
        var busIndex = AudioServer.GetBusIndex("Music");
        AudioServer.SetBusVolumeDb(busIndex, value);
    }
    public void _on_sfx_value_changed(float value)
    {
        var busIndex = AudioServer.GetBusIndex("SFX");
        AudioServer.SetBusVolumeDb(busIndex, value);
    }
}

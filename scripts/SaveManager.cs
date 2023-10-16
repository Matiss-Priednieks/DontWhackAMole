using Godot;
using System;

public partial class SaveManager : Node
{

	const string CONFIG_SAVE_PATH = "user://settings.cfg";
	const string PLAYER_SCORE_SAVE_PATH = "user://playerscore.cfg";

	public void LoadConfig()
	{

		var config = new ConfigFile();

		// Load data from a file.
		Error err = config.Load(CONFIG_SAVE_PATH);

		// If the file didn't load, ignore it.
		if (err != Error.Ok)
		{
			return;
		}
		// Fetch the data for each section.
		// var Resolution = (Vector2I)config.GetValue("Settings", "Resolution");
		// var WindowMode = (String)config.GetValue("Settings", "WindowMode");
		var MasterVolume = (float)config.GetValue("Volume", "Master");
		var MusicVolume = (float)config.GetValue("Volume", "Music");
		var SFXVolume = (float)config.GetValue("Volume", "SFX");


		// GetTree().Root.ContentScaleSize = Resolution;

		// if (Window.ModeEnum.Fullscreen.ToString().Equals(WindowMode))
		// {
		// 	GetWindow().Mode = Window.ModeEnum.Fullscreen;
		// }
		// if (Window.ModeEnum.Windowed.ToString().Equals(WindowMode))
		// {
		// 	GetWindow().Mode = Window.ModeEnum.Windowed;
		// }


		var busIndex = AudioServer.GetBusIndex("Master");
		AudioServer.SetBusVolumeDb(busIndex, MasterVolume);

		busIndex = AudioServer.GetBusIndex("Music");
		AudioServer.SetBusVolumeDb(busIndex, MusicVolume);

		busIndex = AudioServer.GetBusIndex("SFX");
		AudioServer.SetBusVolumeDb(busIndex, SFXVolume);

	}

	public void SaveConfig()
	{
		var config = new ConfigFile();

		// config.SetValue("Settings", "Resolution", GetTree().Root.ContentScaleSize);
		// config.SetValue("Settings", "WindowMode", GetWindow().Mode.ToString());

		var busIndex = AudioServer.GetBusIndex("Master");
		config.SetValue("Volume", "Master", AudioServer.GetBusVolumeDb(busIndex));

		busIndex = AudioServer.GetBusIndex("Music");
		config.SetValue("Volume", "Music", AudioServer.GetBusVolumeDb(busIndex));

		busIndex = AudioServer.GetBusIndex("SFX");
		config.SetValue("Volume", "SFX", AudioServer.GetBusVolumeDb(busIndex));

		config.Save(CONFIG_SAVE_PATH);
	}

	public void SaveScore(int highScore, int highestCombo)
	{
		var score = new ConfigFile();
		score.SetValue("Player", "Score", highScore);
		score.SetValue("Player", "HighestCombo", highestCombo);

		score.Save(PLAYER_SCORE_SAVE_PATH);

	}
	public Vector2 LoadScore()
	{
		var score = new ConfigFile();
		Error err = score.Load(PLAYER_SCORE_SAVE_PATH);

		var highScore = (int)score.GetValue("Player", "Score");
		var highestCombo = (int)score.GetValue("Player", "HighestCombo");

		return new Vector2(highScore, highestCombo);
	}
}

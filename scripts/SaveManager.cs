using Godot;


public partial class SaveManager : Node
{
	const string CONFIG_SAVE_PATH = "user://settings.cfg";
	public bool GameScene;
	Vector2I[] Resolutions = new Vector2I[]{
			new Vector2I(1280, 720),
			new Vector2I(1920, 1080),
			new Vector2I(2560, 1440),
			new Vector2I(3840, 2160)};
	OptionButton ResOptions;
	public void LoadConfig()
	{

		var config = new ConfigFile();

		// Load data from a file.
		Error err;
		if (OS.HasFeature("mobile"))
		{
			err = config.Load(OS.GetUserDataDir() + "settings.cfg");
		}
		else
		{
			err = config.Load(CONFIG_SAVE_PATH);
		}

		// If the file didn't load, ignore it.
		if (err != Error.Ok)
		{
			return;
		}
		// Fetch the data for each section.
		var Resolution = (Vector2I)config.GetValue("Settings", "Resolution");
		var WindowMode = (string)config.GetValue("Settings", "WindowMode");
		var MasterVolume = (float)config.GetValue("Volume", "Master");
		var MusicVolume = (float)config.GetValue("Volume", "Music");
		var SFXVolume = (float)config.GetValue("Volume", "SFX");


		GetTree().Root.ContentScaleSize = Resolution;
		if (GameScene)
		{
			ResOptions = GetNode<OptionButton>("../Node3D/UI/Menu/Menu/SettingsMenu/MarginContainer/SettingsMenu/Resolution");


			ResOptions.Selected = GetResolutionIndex(Resolutions, Resolution);

			if (Window.ModeEnum.Fullscreen.ToString().Equals(WindowMode))
			{
				GetWindow().Mode = Window.ModeEnum.Fullscreen;
			}
			if (Window.ModeEnum.Windowed.ToString().Equals(WindowMode))
			{
				GetWindow().Mode = Window.ModeEnum.Windowed;
			}


			var busIndex = AudioServer.GetBusIndex("Master");
			AudioServer.SetBusVolumeDb(busIndex, MasterVolume);

			busIndex = AudioServer.GetBusIndex("Music");
			AudioServer.SetBusVolumeDb(busIndex, MusicVolume);

			busIndex = AudioServer.GetBusIndex("SFX");
			AudioServer.SetBusVolumeDb(busIndex, SFXVolume);
		}

	}

	public void SaveConfig()
	{
		GD.Print(OS.HasFeature("mobile"));
		var config = new ConfigFile();

		config.SetValue("Settings", "Resolution", GetTree().Root.ContentScaleSize);
		config.SetValue("Settings", "WindowMode", GetWindow().Mode.ToString());

		var busIndex = AudioServer.GetBusIndex("Master");
		config.SetValue("Volume", "Master", AudioServer.GetBusVolumeDb(busIndex));

		busIndex = AudioServer.GetBusIndex("Music");
		config.SetValue("Volume", "Music", AudioServer.GetBusVolumeDb(busIndex));

		busIndex = AudioServer.GetBusIndex("SFX");
		config.SetValue("Volume", "SFX", AudioServer.GetBusVolumeDb(busIndex));

		GD.Print("\n_________\nCongigSaving Data\n_________");
		if (OS.HasFeature("mobile"))
		{
			GD.Print(config.Save(OS.GetUserDataDir() + "settings.cfg"));
		}
		else
		{
			GD.Print(config.Save(CONFIG_SAVE_PATH));
		}
		GD.Print(OS.GetUserDataDir());
	}

	public Error SaveScore(string PlayerName, int highScore, int highestCombo)
	{
		string PLAYER_SCORE_SAVE_PATH;

		PLAYER_SCORE_SAVE_PATH = "user://" + PlayerName + "playerscore.cfg";
		GD.Print(PlayerName);
		var score = new ConfigFile();
		score.SetValue(PlayerName, "Score", highScore);
		score.SetValue(PlayerName, "HighestCombo", highestCombo);

		Error err = score.Save(PLAYER_SCORE_SAVE_PATH);

		return err;
	}
	public Vector2 LoadScore(string PlayerName)
	{
		string PLAYER_SCORE_SAVE_PATH = "user://" + PlayerName + "playerscore.cfg";
		var score = new ConfigFile();
		Error err = score.Load(PLAYER_SCORE_SAVE_PATH);

		// GD.Print("\n" + err + "\n" + PlayerName + "\n");
		var highScore = (int)score.GetValue(PlayerName, "Score");
		var highestCombo = (int)score.GetValue(PlayerName, "HighestCombo");
		// GD.Print(highScore, highestCombo);
		return new Vector2(highScore, highestCombo);
	}

	public int GetResolutionIndex(Vector2I[] resolutions, Vector2I targetRes)
	{
		for (int i = 0; i < resolutions.Length; i++)
		{
			if (resolutions[i] == targetRes)
			{
				return i; // Return the index if the current element matches the target
			}
		}
		return -1;
	}
}

using Godot;
using System;

public partial class UnlockableContentManager : Node
{
	// Called when the node enters the scene tree for the first time.
	private UnlockableContent[] UnlockedContent;
	public UnlockableContentManager()
	{
		UnlockedContent = UnlockableContentList();
	}

	private UnlockableContent[] UnlockableContentList()
	{
		UnlockableContent[] contentList =
		[
			new UnlockableContent(0, "Cowboy Hat", "It's a regular ol' cowboy hat!", false, ResourceLoader.Load<PackedScene>("res://assets/hats/cowboy_hat_1/cowboy_hat.tscn")),
			new UnlockableContent(1, "Astronaut Hat", "Houston, we have a problem.", false,ResourceLoader.Load<PackedScene>("res://assets/hats/astronaut_hat_1/astronaut_hat.tscn")),
			new UnlockableContent(2, "Monocle", "How dapper!", false,ResourceLoader.Load<PackedScene>("res://assets/hats/monocle_hat_1/monocle_hat.tscn")),
			new UnlockableContent(3, "Cat Ears", "Please no.", false,ResourceLoader.Load<PackedScene>("res://assets/hats/cat_ears_hat_1/cat_ears.tscn"))
		];
		return contentList;
	}
	public UnlockableContent[] GetUnlockableContent()
	{
		return UnlockedContent;
	}

}

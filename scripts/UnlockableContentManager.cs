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
			new UnlockableContent(0, "Cowboy Hat", "It's a regular ol' cowboy hat!", false),
			new UnlockableContent(1, "Astronaut Hat", "Houston, we have a problem.", false),
			new UnlockableContent(2, "Monocle", "How dapper!", false),
			new UnlockableContent(3, "Cat Ears", "Please no.", false),
		];
		return contentList;
	}
	public UnlockableContent[] GetUnlockableContent()
	{
		return UnlockedContent;
	}
}

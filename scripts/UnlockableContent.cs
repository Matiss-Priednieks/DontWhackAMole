using Godot;
using System;

public partial class UnlockableContent : Node
{
	// Called when the node enters the scene tree for the first time.

	public int ContentID { get; set; }
	public string ContentName { get; set; }
	public string Description { get; set; }
	public bool IsUnlocked { get; set; }
	public PackedScene ContentScene { get; set; }


	public UnlockableContent(int contentId, string name, string description, bool isUnlocked, PackedScene contentScene)
	{
		ContentID = contentId;
		ContentName = name;
		Description = description;
		IsUnlocked = isUnlocked;
		ContentScene = contentScene;
	}
}

class_name UnlockableContentManager
extends RefCounted

var UnlockedContent: Array[UnlockableContent] = [
	UnlockableContent.new(0, "Cowboy Hat", "It's a regular ol' cowboy hat!", false, ResourceLoader.load("res://assets/hats/cowboy_hat_1/cowboy.png"), ResourceLoader.load("res://assets/hats/cowboy_hat_1/cowboy_hat.tscn"), 100),
	UnlockableContent.new(1, "Cat Ears", "Please no.", false, ResourceLoader.load("res://assets/hats/cat_ears_hat_1/catears.png"), ResourceLoader.load("res://assets/hats/cat_ears_hat_1/cat_ears.tscn"), 250),
	UnlockableContent.new(2, "Monocle", "How dapper!", false, ResourceLoader.load("res://assets/hats/monocle_hat_1/monocle.png"), ResourceLoader.load("res://assets/hats/monocle_hat_1/monocle_hat.tscn"), 625),
	UnlockableContent.new(3, "Astronaut Hat", "Houston, we have a problem.", false, ResourceLoader.load("res://assets/hats/astronaut_hat_1/astro.png"), ResourceLoader.load("res://assets/hats/astronaut_hat_1/astronaut_hat.tscn"), 1500),
]

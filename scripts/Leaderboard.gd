extends MarginContainer
class_name Leaderboard

var HTTPRequestNode: HTTPRequest
var LeaderboardItem: PackedScene
var ScoreList: VBoxContainer


func _ready() -> void:
	HTTPRequestNode = get_node("%LeaderboardRequest")

	LeaderboardItem = ResourceLoader.load("res://scenes/LeaderboardScoreObject.tscn")

	ScoreList = get_node("%ScoreList")

	PopulateLeaderboard()


func PopulateLeaderboard() -> int:
	var newRegHeaders: PackedStringArray = ["Content-Type: application/json"]
	var error := HTTPRequestNode.request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-leaderboard", newRegHeaders, HTTPClient.METHOD_GET, "{}")
	return error


func _on_leaderboard_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	if responseCode == 200 or responseCode == 201:
		if ScoreList.get_child_count() < 10:
			var json := JSON.new()
			json.parse(body.get_string_from_utf8())
			var dict: Dictionary = json.data if json.data is Dictionary else {}

			var PlayerScores: Array = []

			for child in ScoreList.get_children():
				child.queue_free()

			for key in dict:
				var scores: Variant = dict[key]
				if scores is Array:
					for score in scores:
						PlayerScores.append([key, score])
				elif scores is float or scores is int:
					PlayerScores.append([key, scores])

			PlayerScores.sort_custom(func(a, b): return a[1] > b[1])

			for pair in PlayerScores.slice(0, 10):
				var lbItemInst := LeaderboardItem.instantiate() as PanelContainer

				(lbItemInst.get_node("Panel/LBScoreLabel") as Label).text = "%s : %s" % [pair[0], pair[1]]

				ScoreList.add_child(lbItemInst)
				var leadboardItemSeperator := HSeparator.new()
				ScoreList.add_child(leadboardItemSeperator)
	else:
		var lbItemInst := LeaderboardItem.instantiate() as PanelContainer
		(lbItemInst.get_node("Panel/LBScoreLabel") as Label).text = "Error loading leaderboard"
		ScoreList.add_child(lbItemInst)


func _on_update_leaderboard_timeout() -> void:
	if visible:
		PopulateLeaderboard()

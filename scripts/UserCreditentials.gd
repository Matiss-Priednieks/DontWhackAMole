class_name UserCreditentials
extends RefCounted
## JSON payload builder for the DWAM backend. C# had five constructor
## overloads; GDScript uses one factory with defaults plus `to_dict()` for
## serialization (System.Text.Json emitted every public property, so we do too).

var email: String
var username: String
var highscore: float
var unlocks  # Dictionary keyed by int, or null
var collected_coins: int


static func create(_username: String, _email: String, _highscore: float = 0.0, _unlocks = null, _collected_coins: int = 0) -> UserCreditentials:
	var u := UserCreditentials.new()
	u.username = _username
	u.email = _email
	u.highscore = _highscore
	u.unlocks = _unlocks
	u.collected_coins = _collected_coins
	return u


func to_dict() -> Dictionary:
	var d := {
		"email": email,
		"username": username,
		"highscore": highscore,
		"unlocks": null,
		"collected_coins": collected_coins,
	}
	if unlocks != null:
		var conv := {}
		for k in unlocks.keys():
			conv[str(k)] = unlocks[k]
		d["unlocks"] = conv
	return d


func to_json() -> String:
	return JSON.stringify(to_dict())

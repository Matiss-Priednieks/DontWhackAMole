extends Node
## Autoload singleton (was res://scripts/LoggedInUser.cs). Reachable globally as
## `LoggedInUser` and at `/root/LoggedInUser`.

const BASE_URL := "https://forwardvector.uksouth.cloudapp.azure.com/dwam"
const JSON_HEADERS: PackedStringArray = ["Content-Type: application/json"]

## DEBUG: when true, every hat is treated as already owned so you can equip and
## test them from the shop without buying. On automatically in the editor and
## debug builds, off in release exports. Hard-set it to true/false to override.
var DEBUG_UNLOCK_ALL_HATS := OS.is_debug_build()

var HTTPRequestNode: HTTPRequest
var UnlocksHTTPRequest: HTTPRequest
var CurrencyHTTPRequest: HTTPRequest
var BuyContentHTTPRequest: HTTPRequest
var GetUnlocksHTTPRequest: HTTPRequest
var GetHighscoreHTTPRequest: HTTPRequest

var Unlockables := UnlockableContentManager.new()
var UsernameLabel: Label

var LoggedIn: bool
var Email: String
var Username: String

## Firebase Auth tokens from the last successful login. TokenExpiresAt is a unix
## timestamp (0 = none). Cleared on logout / SetDefaultUser.
var IdToken: String
var RefreshToken: String
var LocalId: String
var TokenExpiresAt: float = 0.0

var UserHighScore: int

var UnlockablesArray: Array[UnlockableContent]
var Unlockables_dict: Dictionary
var EquippedHatIndex: int = -1
var CurrentHat: Node3D

var AccountCurrency: int

var BuyRequestID: int = -1
var Player: Mole


func _ready() -> void:
	UnlockablesArray = Unlockables.GetUnlockableContent()
	Unlockables_dict = {}
	PopulateUnlockablesDict()

	SetDefaultUser()


func PopulateUnlockablesDict() -> void:
	for unlockable in UnlockablesArray:
		if DEBUG_UNLOCK_ALL_HATS:
			unlockable.IsUnlocked = true
		Unlockables_dict[unlockable.ContentID] = unlockable.IsUnlocked


func SetDefaultUser() -> void:
	Username = "Guest"
	if UsernameLabel != null:
		UsernameLabel.text = "Guest"
	LoggedIn = false
	IdToken = ""
	RefreshToken = ""
	LocalId = ""
	TokenExpiresAt = 0.0


func SetTokens(id_token: String, refresh_token: String, expires_in: Variant, local_id: String) -> void:
	IdToken = id_token
	RefreshToken = refresh_token
	LocalId = local_id
	var secs := float(str(expires_in)) if str(expires_in) != "" else 0.0
	TokenExpiresAt = Time.get_unix_time_from_system() + secs if secs > 0.0 else 0.0


func TokenValid() -> bool:
	return IdToken != "" and Time.get_unix_time_from_system() < TokenExpiresAt


func SetUnlocksDict(unlockables: Variant) -> void:
	UpdateUnlockablesDict(unlockables)
	if DEBUG_UNLOCK_ALL_HATS:
		for c in UnlockablesArray:
			Unlockables_dict[c.ContentID] = true
	UpdateShopUI()


func PurchasedItemUpdate(_unlockables: Variant) -> void:
	UpdateUnlockablesDict(_unlockables)
	UpdateUnlockedContentRequest(Unlockables_dict)


## `unlockables` comes from a server response - it may not be a Dictionary, and
## its keys may be out of range. Only accept int keys that map to a real hat.
func UpdateUnlockablesDict(unlockables: Variant) -> void:
	if not unlockables is Dictionary:
		return
	for key in (unlockables as Dictionary).keys():
		if str(key).is_valid_int():
			var idx := int(key)
			if idx >= 0 and idx < UnlockablesArray.size():
				Unlockables_dict[idx] = unlockables[key]


func GetUnlocksDict() -> Dictionary:
	return Unlockables_dict


func LoginInitialisation() -> void:
	HTTPRequestNode = get_node("../Node3D/UI/HighscoreRequest")
	UnlocksHTTPRequest = get_node("../Node3D/UnlocksRequest")
	CurrencyHTTPRequest = get_node("../Node3D/CurrencyUpdateRequest")
	BuyContentHTTPRequest = get_node("../Node3D/BuyAttempt")
	GetUnlocksHTTPRequest = get_node("../Node3D/UnlocksInfo")
	GetHighscoreHTTPRequest = get_node("../Node3D/UI/GetHighscore")
	Player = get_node("../Node3D/Mole")

	UsernameLabel = get_node("../Node3D/UI/Menu/Menu/AccountMenu/MarginContainer/LoggedInScreen/VBoxContainer/UserLabel")

	SetDefaultUser()
	GetUnlockedContentRequest()
	# GetHighscoreRequest()


func SetUsername(username: String) -> void:
	Username = username
	if UsernameLabel != null:
		UsernameLabel.text = username


func SetEmail(email: String) -> void:
	Email = email


func GetUsername() -> String:
	if UsernameLabel != null:
		return UsernameLabel.text
	return Username


func Logout() -> void:
	SetDefaultUser()


func Login(username: String) -> void:
	SetUsername(username)
	LoggedIn = true
	print(Email)


func SetHighscore(value: float) -> void:
	UserHighScore = int(value)


func GetHighscore() -> float:
	return float(UserHighScore)


func CheckUnlockedContent(contentBuyID: int) -> void:
	BuyRequestID = contentBuyID
	GetUnlockedContentRequest()


func FinalBuyCheck() -> void:
	print("Buy request 3")
	# C#: Unlockables_dict.TryGetValue(BuyRequestID, out isUnlocked) && !isUnlocked && ...
	# i.e. the id must actually be in the dict AND currently locked. `.get(id, false)
	# == false` would also be true for a missing id (e.g. the sentinel -1), which
	# then indexed UnlockablesArray[-1] and could spend the player's coins on load.
	if Unlockables_dict.has(BuyRequestID) and Unlockables_dict[BuyRequestID] == false and AccountCurrency >= UnlockablesArray[BuyRequestID].ContentPrice:
		Unlockables_dict[BuyRequestID] = true
		BuyContentRequest()
		Update()
		SteamManager.Unlock("ACH_FIRST_HAT")
		if not DEBUG_UNLOCK_ALL_HATS:
			var all_owned := true
			for c in UnlockablesArray:
				if not Unlockables_dict.get(c.ContentID, false):
					all_owned = false
					break
			if all_owned:
				SteamManager.Unlock("ACH_ALL_HATS")
	else:
		print("Item is already unlocked or insufficient funds.")
	BuyRequestID = -1


func UpdateShopUI() -> void:
	for key in Unlockables_dict.keys():
		if key >= 0 and key < UnlockablesArray.size():
			UnlockablesArray[key].IsUnlocked = DEBUG_UNLOCK_ALL_HATS or Unlockables_dict[key]


func EquipHat(hatIndex: int) -> void:
	print("Equip called")
	if CurrentHat == null:
		print("Equipping new hat")
		CurrentHat = UnlockablesArray[hatIndex].ContentScene.instantiate()
		(Player.get_node("HatMount") as Node3D).add_child(CurrentHat)
		EquippedHatIndex = hatIndex
	else:
		print("Equipping new hat")
		CurrentHat.queue_free()
		CurrentHat = UnlockablesArray[hatIndex].ContentScene.instantiate()
		(Player.get_node("HatMount") as Node3D).add_child(CurrentHat)
		EquippedHatIndex = hatIndex


func UnequipHat() -> void:
	CurrentHat.queue_free()
	CurrentHat = null
	EquippedHatIndex = -1


func GetUnlockedContentRequest() -> int:
	var userData := UserCreditentials.create(Username, Email, 0.0, Unlockables_dict)
	return GetUnlocksHTTPRequest.request(BASE_URL + "/get-unlocks", JSON_HEADERS, HTTPClient.METHOD_GET, userData.to_json())


func BuyContentRequest() -> int:
	var userData := UserCreditentials.create(Username, Email, 0.0, Unlockables_dict)
	return BuyContentHTTPRequest.request(BASE_URL + "/buy-attempt", JSON_HEADERS, HTTPClient.METHOD_GET, userData.to_json())


func HighscoreUpdateRequest() -> int:
	var userData := UserCreditentials.create(Username, Email, GetHighscore())
	return HTTPRequestNode.request(BASE_URL + "/update-highscore", JSON_HEADERS, HTTPClient.METHOD_POST, userData.to_json())


func GetHighscoreRequest() -> int:
	var userData := UserCreditentials.create(Username, Email)
	return GetHighscoreHTTPRequest.request(BASE_URL + "/get-highscore", JSON_HEADERS, HTTPClient.METHOD_GET, userData.to_json())


func UpdateUnlockedContentRequest(_unlockables_dict: Dictionary) -> int:
	print(_unlockables_dict)
	var userData := UserCreditentials.create(Username, Email, 0.0, _unlockables_dict)
	return UnlocksHTTPRequest.request(BASE_URL + "/update-unlocks", JSON_HEADERS, HTTPClient.METHOD_POST, userData.to_json())


func UpdateUserCurrency(CollectedCoins: int) -> int:
	print("CoinsInPOSTCall: " + str(CollectedCoins))
	var userData := UserCreditentials.create(Username, Email, GetHighscore(), null, CollectedCoins)
	print("CoinsInUserData:" + str(userData.collected_coins))
	var userDataJson := userData.to_json()
	var error := CurrencyHTTPRequest.request(BASE_URL + "/update-user-currency", JSON_HEADERS, HTTPClient.METHOD_POST, userDataJson)
	print(userDataJson)
	return error


func Update() -> void:
	UpdateUserCurrency(-UnlockablesArray[BuyRequestID].ContentPrice)
	UpdateUnlockedContentRequest(Unlockables_dict)

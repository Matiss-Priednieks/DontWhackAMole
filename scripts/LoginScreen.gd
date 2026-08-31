extends Panel
class_name LoginScreen

var LoginEmail: String
var LoginPassword: String
var EmailInput: LineEdit
var PasswordInput: LineEdit
var HTTPRequestNode: HTTPRequest

var User: Node
var ErrorPanel: Panel
var LoggedInPage: Panel
var ErrorMessage: Label
var UserLabel: Label
var Logout: Button
var Login: Button
var RegButton: Button
var GuestButton: Button
var MainMenu: PanelContainer
var AccountMenu: PanelContainer
var SaveManagerRef: Node


func _is_null_or_whitespace(s) -> bool:
	return s == null or str(s).strip_edges() == ""


func _ready() -> void:
	SaveManagerRef = get_node("/root/SaveManager")

	MainMenu = get_node("%MenuButtons")
	AccountMenu = get_node("%AccountMenu")
	EmailInput = get_node("%Email")
	PasswordInput = get_node("%Password")
	HTTPRequestNode = get_node("%LoginRequest")

	Logout = get_node("%Logout")
	Login = get_node("%LoginConfirm")

	RegButton = get_node("%GoToRegPage")
	GuestButton = get_node("%AsGuest")

	UserLabel = get_node("%UserLabel")

	LoggedInPage = get_node("%LoggedInScreen")

	ErrorPanel = get_node("%ErrorPanel")
	ErrorMessage = get_node("%ErrorMessage")
	User = get_node("/root/LoggedInUser")

	ErrorPanel.hide()


func _on_login_email_field_text_changed(newText: String) -> void:
	LoginEmail = newText
	ErrorPanel.hide()


func _on_login_email_field_text_submitted(newText: String) -> void:
	LoginEmail = newText
	PasswordInput.grab_focus()


func _on_login_password_field_text_changed(newText: String) -> void:
	LoginPassword = newText
	ErrorPanel.hide()


func _on_login_password_field_text_submitted(newText: String) -> void:
	LoginPassword = newText
	LoginRequest()


func _on_login_button_pressed() -> void:
	LoginRequest()


func _on_login_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	var body_text := body.get_string_from_utf8()
	print("[LoginScreen] login response ", responseCode, ": ", body_text)
	var response: Variant = JSON.parse_string(body_text)
	var dict: Dictionary = response if response is Dictionary else {}
	await get_tree().create_timer(1.0).timeout
	if responseCode == 200:
		Login.disabled = false
		ErrorPanel.hide()

		var username := str(dict.get("username", ""))
		if not _is_null_or_whitespace(username):
			User.Login(username)
		User.SetHighscore(float(dict.get("highscore", 0)))
		if not _is_null_or_whitespace(LoginEmail):
			User.SetEmail(LoginEmail)
		UserLabel.text = username
		UserLabel.show()
		LoggedInPage.show()
		hide()

		EmailInput.editable = true
		PasswordInput.editable = true
		User.GetHighscoreRequest()
	else:
		Login.disabled = false

		EmailInput.show()
		PasswordInput.show()
		Login.show()

		Logout.hide()
		UserLabel.hide()
		if str(dict.get("response_text", "")).contains("TOO_MANY_ATTEMPTS_TRY_LATER"):
			ErrorMessage.text = "Too Many Attempts Try Again Later"
		else:
			ErrorMessage.text = "Invalid Login"
		ErrorPanel.show()
		EmailInput.editable = true
		PasswordInput.editable = true


func _on_logout_pressed() -> int:
	show()
	LoggedInPage.hide()

	User.SetUsername("Guest")
	User.Email = ""
	User.LoggedIn = false
	return OK


func LoginRequest() -> int:
	Login.disabled = true

	EmailInput.editable = false
	PasswordInput.editable = false

	if User.GetUsername() == "Guest":
		var newRegHeaders: PackedStringArray = ["Content-Type: application/json"]
		var LoginCredentials := UserRegCreditentials.create(LoginEmail, LoginPassword, true)
		var JsonString := LoginCredentials.to_json()
		var error := HTTPRequestNode.request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-user/login", newRegHeaders, HTTPClient.METHOD_POST, JsonString)
		return error
	else:
		return OK


func _on_back_to_menu_pressed() -> void:
	AccountMenu.hide()
	MainMenu.show()

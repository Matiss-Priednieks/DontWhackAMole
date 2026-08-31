extends Panel
class_name Registration

var Username: String
var RegistrationEmail: String
var RegistrationPassword: String
var RegistrationPasswordConfirmation: String
var NameInput: LineEdit
var EmailInput: LineEdit
var PasswordInput: LineEdit
var ConfirmPasswordInput: LineEdit
var LoginEmailInput: LineEdit
var LoginPass: LineEdit
var HTTPRequestNode: HTTPRequest
var HTTPLoginRequest: HTTPRequest

var ErrorPanel: Panel
var LoginScreenPanel: Panel
var LoggedInPage: Panel
var ErrorMessage: Label
var Logout: Button
var Login: Button
var RegisterConfirm: Button
var UserLabel: Label
var User: Node

var UIRef: Game

const EMAIL_PATTERN := "^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+$"
const STRONG_PW_PATTERN := "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{6,}$"
const USERNAME_PATTERN := "^[\\p{L}a-zA-Z0-9_-]{1,256}$"


func _is_null_or_whitespace(s) -> bool:
	return s == null or str(s).strip_edges() == ""


func _regex_matches(pattern: String, subject: String) -> bool:
	var re := RegEx.new()
	re.compile(pattern)
	return re.search(subject) != null


func _ready() -> void:
	LoggedInPage = get_node("%LoggedInScreen")
	UserLabel = get_node("%UserLabel")
	Logout = get_node("%Logout")
	NameInput = get_node("%UsernameReg")
	EmailInput = get_node("%EmailReg")
	PasswordInput = get_node("%PasswordReg")
	ConfirmPasswordInput = get_node("%PasswordRegConfirm")
	HTTPRequestNode = get_node("%RegRequest")
	HTTPLoginRequest = get_node("%LoginRequest")
	LoginScreenPanel = get_node("%LoginScreen")
	ErrorPanel = get_node("%ErrorPanel")
	ErrorMessage = get_node("%ErrorMessage")

	User = get_node("/root/LoggedInUser")

	LoginEmailInput = get_node("%Email")
	LoginPass = get_node("%Password")

	Login = get_node("%LoginConfirm")
	RegisterConfirm = get_node("%RegisterConfirm")
	UIRef = get_parent().get_parent().get_parent().get_parent().get_parent().get_parent() as Game


func _process(delta: float) -> void:
	if NameInput.text == null or EmailInput.text == null or PasswordInput.text == null or ConfirmPasswordInput == null:
		RegisterConfirm.disabled = true


func _on_username_reg_text_changed(newText: String) -> void:
	if IsValidUsername(newText):
		Username = newText
		ErrorPanel.hide()
	else:
		ErrorMessage.text = "Username cannot containt special characters or be empty"
		ErrorPanel.show()


func _on_username_reg_text_submitted(newText: String) -> void:
	if IsValidUsername(newText):
		Username = newText
		ErrorPanel.hide()
	else:
		ErrorMessage.text = "Username cannot containt special characters or be empty"
		ErrorPanel.show()


func _on_register_pressed() -> void:
	# confirm registration button
	if IsValidRegistration():
		RegisterConfirm.disabled = true
		CreateRegistration()


func _on_password_reg_text_submitted(newText: String) -> void:
	RegistrationPassword = newText
	if IsValidRegistration():
		CreateRegistration()
	else:
		ConfirmPasswordInput.grab_focus()


func _on_password_reg_confirm_text_submitted(newText: String) -> void:
	RegistrationPasswordConfirmation = newText
	if IsValidRegistration():
		CreateRegistration()
	ConfirmPasswordInput.release_focus()


func _on_email_reg_text_changed(newText: String) -> void:
	RegistrationEmail = newText
	ErrorPanel.hide()


func _on_email_reg_text_submitted(newText: String) -> void:
	RegistrationEmail = newText
	if IsValidRegistration():
		CreateRegistration()
	ConfirmPasswordInput.grab_focus()


func _on_password_reg_text_changed(newText: String) -> void:
	RegistrationPassword = newText
	ErrorPanel.hide()


func _on_password_reg_confirm_text_changed(newText: String) -> void:
	RegistrationPasswordConfirmation = newText
	ErrorPanel.hide()


func IsStrongPassword(password: String) -> bool:
	# Minimum 6 characters, at least one uppercase, one lowercase, and one digit
	return _regex_matches(STRONG_PW_PATTERN, password)


func IsValidRegistration() -> bool:
	if IsValidEmail() and IsValidPassword() and IsValidUsername(Username):
		return true
	else:
		return false


func IsValidPassword() -> bool:
	if RegistrationPasswordConfirmation == RegistrationPassword:
		if IsStrongPassword(RegistrationPasswordConfirmation):
			return true
		else:
			ErrorMessage.text = "Password must be at least 6 characters with at least one uppercase letter and a digit."
			ErrorPanel.show()
			return false
	else:
		ErrorMessage.text = "Passwords Must Match"
		ErrorPanel.show()
		return false


func IsValidEmail() -> bool:
	if _regex_matches(EMAIL_PATTERN, EmailInput.text):
		return true
	else:
		ErrorMessage.text = "Invalid Email"
		ErrorPanel.show()
		return false


func IsValidUsername(username: String) -> bool:
	# Alphanumeric, underscores, hyphens, and letters from any language, max length 256
	return _regex_matches(USERNAME_PATTERN, username)


func CreateRegistration() -> void:
	call_deferred("NewRegRequest")


#region REQUEST FUNCTIONS
func UserDataRequest() -> void:
	# grabs user data from server
	var userData := UserCreditentials.create(Username, RegistrationEmail, User.GetHighscore(), User.GetUnlocksDict())
	var userDataJson := userData.to_json()
	var newRegHeaders: PackedStringArray = ["Content-Type: application/json"]
	var error := HTTPRequestNode.request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/save-user", newRegHeaders, HTTPClient.METHOD_POST, userDataJson)


func NewRegRequest() -> void:
	# sends new registration request
	var newReg := UserRegCreditentials.create(RegistrationEmail, RegistrationPasswordConfirmation, true)
	var newRegBody := newReg.to_json()
	var newRegHeaders: PackedStringArray = ["Content-Type: application/json"]
	var error := HTTPRequestNode.request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/create-user", newRegHeaders, HTTPClient.METHOD_POST, newRegBody)


func LoginRequest() -> int:
	# sends login request
	Login.disabled = true
	LoginEmailInput.editable = false
	LoginPass.editable = false
	if User.GetUsername() == "Guest":
		var newRegHeaders: PackedStringArray = ["Content-Type: application/json"]
		var LoginCredentials := UserRegCreditentials.create(RegistrationEmail, RegistrationPasswordConfirmation, true)
		var JsonString := LoginCredentials.to_json()
		var error := HTTPLoginRequest.request("https://forwardvector.uksouth.cloudapp.azure.com/dwam/get-user/login", newRegHeaders, HTTPClient.METHOD_POST, JsonString)
		return error
	else:
		UserLabel.text = "Guest"
		User.Logout()
		return OK
#endregion


#region COMPLETED REQUESTS
func _on_reg_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	var body_text := body.get_string_from_utf8()
	print("[Registration] reg response ", responseCode, ": ", body_text)
	var response: Variant = JSON.parse_string(body_text)
	var dict: Dictionary = response if response is Dictionary else {}

	if responseCode == 200 and int(dict.get("status_code", 0)) != 400:
		ErrorPanel.hide()
		RegisterConfirm.disabled = false
		UIRef.Register = false
		UIRef.Login = true
		await get_tree().create_timer(1.0).timeout
		call_deferred("UserDataRequest")
		await get_tree().create_timer(1.0).timeout
		call_deferred("LoginRequest")
		hide()
	else:
		if dict.size() != 0:
			if int(dict.get("status_code", 0)) == 400:
				ErrorPanel.show()
				ErrorMessage.text = "User already exists!"
		RegisterConfirm.disabled = false
		UIRef.Login = false
		UIRef.Register = true


func _on_login_request_request_completed(result: int, responseCode: int, headers: PackedStringArray, body: PackedByteArray) -> void:
	var body_text := body.get_string_from_utf8()
	print("[Registration] login response ", responseCode, ": ", body_text)
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
		if not _is_null_or_whitespace(RegistrationEmail):
			User.SetEmail(RegistrationEmail)

		UserLabel.text = username
		LoggedInPage.show()
		hide()

		EmailInput.editable = true
		PasswordInput.editable = true
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
#endregion

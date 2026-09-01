class_name UserRegCreditentials
extends RefCounted

var email: String
var password: String
var returnSecureToken: bool  # Firebase's spelling - was "returnSecuredToken" (typo, ignored)
## Only sent on /create-user so the server can slur-check the name before the
## Firebase account is made. Left empty (and omitted) for the login request.
var username: String


static func create(_email: String, _password: String, _secureToken: bool, _username: String = "") -> UserRegCreditentials:
	var u := UserRegCreditentials.new()
	u.email = _email
	u.password = _password
	u.returnSecureToken = _secureToken
	u.username = _username
	return u


func to_dict() -> Dictionary:
	var d := {
		"email": email,
		"password": password,
		"returnSecureToken": returnSecureToken,
	}
	if username != "":
		d["username"] = username
	return d


func to_json() -> String:
	return JSON.stringify(to_dict())

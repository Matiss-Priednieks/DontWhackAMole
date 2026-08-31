class_name UserRegCreditentials
extends RefCounted

var email: String
var password: String
var returnSecuredToken: bool


static func create(_email: String, _password: String, _securedToken: bool) -> UserRegCreditentials:
	var u := UserRegCreditentials.new()
	u.email = _email
	u.password = _password
	u.returnSecuredToken = _securedToken
	return u


func to_dict() -> Dictionary:
	return {
		"email": email,
		"password": password,
		"returnSecuredToken": returnSecuredToken,
	}


func to_json() -> String:
	return JSON.stringify(to_dict())

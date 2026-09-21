extends Node
## Autoload singleton. Single entry point for everything Steam.
##
## The game must run BOTH standalone and through Steam, so nothing here may hard
## depend on the GodotSteam extension being present. When the extension isn't
## loaded / Steam isn't running, `Available` stays false and every helper is a
## safe no-op - the rest of the game is unchanged.
##
## All Steam calls go through the `_steam` singleton object fetched at runtime
## (never the bare `Steam` identifier) so this script still parses if the
## `addons/godotsteam/` folder is removed for a non-Steam build.
##
## Signatures below are for GodotSteam GDExtension v4.22 (Godot 4.4+):
##   steamInitEx(app_id, embed_callbacks) -> {status, verbal}   (status 0 == OK)
##   requestUserStats(steam_id)      -> fires `user_stats_received(game, result, user)`
##   getAuthTicketForWebApi(identity) -> fires
##       `get_ticket_for_web_api(handle, result, size, buffer: PackedByteArray)`
## This build's embedded callback loop doesn't fire, so we pump run_callbacks()
## ourselves every frame.

## Steam App ID. 480 = "Spacewar", Steam's public test app - use it until the
## real DontWhackAMole App ID exists (needs the $100 Steam Direct fee). Also put
## the same number in `steam_appid.txt` in the project root for editor runs.
const APP_ID := 480

## Emitted after init resolves (whether or not Steam came up). Menus can wait on
## this to decide between the "Continue as <name>" button and the email form.
signal SteamReady(available: bool)
## Web-API auth ticket for /steam-login. hex == "" means it failed / no Steam.
signal SteamTicketReady(ticket_hex: String)
## Steam overlay opened/closed - the game should pause while it's open.
signal OverlayToggled(active: bool)

var Available := false
var SteamId: int = 0
var PersonaName := ""
var StatsReady := false

var _steam: Object = null
var _webTicketHandle: int = 0


func _ready() -> void:
	if not Engine.has_singleton("Steam"):
		push_warning("[Steam] GodotSteam extension not present - running standalone.")
		SteamReady.emit(false)
		return

	_steam = Engine.get_singleton("Steam")

	var res: Dictionary = _steam.steamInitEx(APP_ID, false)
	if int(res.get("status", 1)) != 0:
		push_warning("[Steam] init failed (%s) - running standalone." % res.get("verbal", res))
		_steam = null
		SteamReady.emit(false)
		return

	Available = true
	process_mode = Node.PROCESS_MODE_ALWAYS  # keep pumping callbacks even when the tree is paused
	SteamId = int(_steam.getSteamID())
	PersonaName = str(_steam.getPersonaName())
	print("[Steam] ready as %s (%d)" % [PersonaName, SteamId])

	_steam.connect("user_stats_received", _on_user_stats_received)
	_steam.connect("overlay_toggled", _on_overlay_toggled)
	_steam.connect("get_ticket_for_web_api", _on_web_ticket)
	_steam.requestUserStats(SteamId)  # -> user_stats_received; needed before setAchievement

	SteamReady.emit(true)


func _process(_delta: float) -> void:
	if Available:
		_steam.run_callbacks()


func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST or what == NOTIFICATION_PREDELETE:
		if Available and _steam != null and _steam.has_method("steamShutdown"):
			_steam.steamShutdown()


# --- Stats / achievements -----------------------------------------------------
# Define the API names on the Steamworks partner site first. With App ID 480 the
# calls run but only Spacewar's built-in achievements actually exist.

func Unlock(api_name: String) -> void:
	if not Available:
		return
	var cur: Variant = _steam.getAchievement(api_name)
	if cur is Dictionary and cur.get("achieved", false):
		return
	if _steam.setAchievement(api_name):
		_steam.storeStats()


func ClearAchievement(api_name: String) -> void:
	if not Available:
		return
	_steam.clearAchievement(api_name)
	_steam.storeStats()


## Add `delta` to an INT stat (e.g. lifetime coins, clutch dodges).
func AddStat(api_name: String, delta: int) -> void:
	if not Available:
		return
	_steam.setStatInt(api_name, int(_steam.getStatInt(api_name)) + delta)
	_steam.storeStats()


## Raise an INT stat to `value` only if it's a new high (e.g. best score).
func SetStatMax(api_name: String, value: int) -> void:
	if not Available:
		return
	if value > int(_steam.getStatInt(api_name)):
		_steam.setStatInt(api_name, value)
		_steam.storeStats()


## Dev helper - wipe all stats + achievements for the local user.
func ResetAllStats() -> void:
	if not Available:
		return
	_steam.resetAllStats(true)
	_steam.storeStats()


# --- Overlay ----------------------------------------------------------------

func OpenOverlay(to: String = "") -> void:
	if Available:
		_steam.activateGameOverlay(to)


func OverlayEnabled() -> bool:
	return Available and bool(_steam.isOverlayEnabled())


# --- Web-API auth ticket (for /steam-login) --------------------------------

func RequestLoginTicket() -> void:
	if not Available:
		SteamTicketReady.emit("")
		return
	# Identity string is optional; if set, the server must pass the same value to
	# AuthenticateUserTicket. "" is fine to start.
	_webTicketHandle = _steam.getAuthTicketForWebApi("")


func _on_web_ticket(handle: int, result: int, _size: int, buffer: PackedByteArray) -> void:
	if result != 1 or buffer.is_empty():  # 1 == k_EResultOK
		push_warning("[Steam] web ticket failed (result=%d)" % result)
		SteamTicketReady.emit("")
		return
	_webTicketHandle = handle
	SteamTicketReady.emit(buffer.hex_encode())


func CancelLoginTicket() -> void:
	if Available and _webTicketHandle != 0:
		_steam.cancelAuthTicket(_webTicketHandle)
		_webTicketHandle = 0


# --- Callbacks ------------------------------------------------------------

func _on_user_stats_received(_game: int, result: int, _user: int) -> void:
	if result == 1:
		StatsReady = true


func _on_overlay_toggled(active: bool, _user_initiated: bool = false, _app_id: int = 0) -> void:
	OverlayToggled.emit(active)


# --- Dev test harness (debug builds only) ----------------------------------
# F9 : print Steam status   |   Shift+F9 : wipe all stats/achievements
# F10: unlock a real Spacewar (480) achievement   |   F11: request a login ticket
# Remove this block once the real App ID and the login UI are in.

func _unhandled_key_input(event: InputEvent) -> void:
	if not OS.is_debug_build() or not (event is InputEventKey) or not event.pressed or event.echo:
		return
	match (event as InputEventKey).keycode:
		KEY_F9:
			if (event as InputEventKey).shift_pressed:
				ResetAllStats()
				print("[Steam] reset all stats/achievements")
			else:
				print("[Steam] available=%s id=%d name=%s overlay_enabled=%s stats_ready=%s"
					% [Available, SteamId, PersonaName, OverlayEnabled(), StatsReady])
		KEY_F10:
			print("[Steam] test-unlocking ACH_WIN_ONE_GAME (Spacewar)")
			Unlock("ACH_WIN_ONE_GAME")
		KEY_F11:
			if not SteamTicketReady.is_connected(_dbg_ticket):
				SteamTicketReady.connect(_dbg_ticket, CONNECT_ONE_SHOT)
			RequestLoginTicket()


func _dbg_ticket(hex: String) -> void:
	if hex == "":
		print("[Steam] ticket request failed / Steam not available")
	else:
		print("[Steam] got login ticket (%d hex chars): %s..." % [hex.length(), hex.substr(0, 32)])

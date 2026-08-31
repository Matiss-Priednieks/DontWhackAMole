extends CanvasLayer
class_name DebugInfoUI

var Player: Mole
var FPS: Label
var Timer_lbl: Label
var ChosenHole: Label
var DownOrOut: Label
var OutTimer: Label
var IFrames: Label


func _ready() -> void:
	Player = get_node("%Mole")
	FPS = get_node("%FPS")
	Timer_lbl = get_node("%PopTimer")
	OutTimer = get_node("%DangerTimer")
	ChosenHole = get_node("%ChosenHole")
	DownOrOut = get_node("%DownOrOut")
	IFrames = get_node("%IFrames")


func _process(delta: float) -> void:
	FPS.text = "FPS: " + str(Engine.get_frames_per_second())
	Timer_lbl.text = "Time until pop: " + str(round((get_node("%PopOutTimer") as Timer).time_left))
	OutTimer.text = "Time until Danger: " + str(1 - Player.GetDangerTimer())
	ChosenHole.text = "Chosen Hole: " + str(Player.GetChosenHole())
	DownOrOut.text = "Down: " + str(Player.GetDownStatus())
	IFrames.text = "IFrames: " + str(Player.GetIFrames())

	if Input.is_action_just_released("debug_info"):
		visible = not visible

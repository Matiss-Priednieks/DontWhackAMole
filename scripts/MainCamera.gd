extends Camera3D
class_name MainCamera

var Length: int = 100
var StartPos: Vector2
var CurPos: Vector2
var Swiping: bool

signal LeftSwipe(Hole: int)
signal RightSwipe(Hole: int)
signal UpSwipe(Hole: int)
signal DownSwipe(Hole: int)


func _process(delta: float) -> void:
	if Input.is_action_just_pressed("press"):
		if not Swiping:
			Swiping = true
			StartPos = get_viewport().get_mouse_position()
	if Input.is_action_pressed("press"):
		if Swiping:
			CurPos = get_viewport().get_mouse_position()
			if StartPos.distance_to(CurPos) >= Length:
				var Direction := Vector2(CurPos.x - StartPos.x, CurPos.y - StartPos.y)
				var NormalisedDirection := Direction.normalized()

				if NormalisedDirection.x >= -1 and NormalisedDirection.x <= 0 and NormalisedDirection.y <= 0.8 and NormalisedDirection.y >= -0.8:
					print("Left Swipe")
					emit_signal("LeftSwipe", 1)
				elif NormalisedDirection.x >= 0 and NormalisedDirection.x <= 1 and NormalisedDirection.y <= 0.8 and NormalisedDirection.y >= -0.8:
					print("Right Swipe")
					emit_signal("RightSwipe", 3)
				elif NormalisedDirection.y >= -1 and NormalisedDirection.y <= 0 and NormalisedDirection.x <= 0.8 and NormalisedDirection.x >= -0.8:
					print("Up Swipe")
					emit_signal("UpSwipe", 0)
				elif NormalisedDirection.y >= 0 and NormalisedDirection.y <= 1 and NormalisedDirection.x <= 0.8 and NormalisedDirection.x >= -0.8:
					print("Down Swipe")
					emit_signal("DownSwipe", 2)

				print("Swipe detected")
				Swiping = false
				print(NormalisedDirection)
	else:
		Swiping = false

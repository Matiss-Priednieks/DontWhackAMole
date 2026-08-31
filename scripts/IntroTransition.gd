extends CanvasLayer
class_name IntroTransition

var ScreenAlpha: float = 1.0

var colour: Color
var colorRect: ColorRect


func _ready() -> void:
	ScreenAlpha = 1.0
	colorRect = get_node("BlackBox")
	colorRect.show()
	FadeFirstScreen()


func _process(delta: float) -> void:
	colour = Color(0.04, 0.04, 0.04, ScreenAlpha)
	colorRect.color = colour


func FadeFirstScreen() -> void:
	await get_tree().create_timer(1.0).timeout
	FadeIn()
	await get_tree().create_timer(2.0).timeout
	colorRect.hide()


func FadeIn() -> void:
	var tempTween: Tween = create_tween()
	tempTween.tween_property(self, "ScreenAlpha", 0.0, 2.0).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)

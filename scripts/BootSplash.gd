extends CanvasLayer
class_name BootSplash

var ScreenAlpha: float
var Fading: bool
var MainGameScene: PackedScene
var colour: Color
var colorRect: ColorRect
var SaveManagerRef: Node
var _SceneTree: SceneTree
var BannerDisplayed: bool


func _ready() -> void:
	_SceneTree = get_tree()
	SaveManagerRef = _SceneTree.root.get_node("SaveManager")
	SaveManagerRef.LoadConfig()
	ScreenAlpha = 1
	(get_node("%FirstScreen") as Control).show()
	MainGameScene = ResourceLoader.load("res://scenes/MainGame.tscn")
	colorRect = get_node("BlackBox")
	var BusLayout: AudioBusLayout = ResourceLoader.load("res://resources/default_bus_layout.tres")
	AudioServer.set_bus_layout(BusLayout)
	FadeFirstScreen()


func _process(delta: float) -> void:
	colour = Color(0.04, 0.04, 0.04, ScreenAlpha)
	colorRect.color = colour

	if Input.is_action_just_pressed("press") and BannerDisplayed:
		_SceneTree.change_scene_to_packed(MainGameScene)


func FadeFirstScreen() -> void:
	await _SceneTree.create_timer(1.0).timeout
	FadeIn()

	await _SceneTree.create_timer(2.0).timeout
	FadeOut()
	BannerDisplayed = true
	await _SceneTree.create_timer(1.5).timeout
	(get_node("%FirstScreen") as Control).hide()
	await _SceneTree.create_timer(2.0).timeout

	_SceneTree.change_scene_to_packed(MainGameScene)


func FadeIn() -> void:
	var tempTween := create_tween()
	tempTween.tween_property(self, "ScreenAlpha", 0, 2).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)


func FadeOut() -> void:
	var tempTween := create_tween()
	tempTween.tween_property(self, "ScreenAlpha", 1, 2).set_trans(Tween.TRANS_EXPO).set_ease(Tween.EASE_OUT)

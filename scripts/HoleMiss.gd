extends Area3D
class_name HoleMiss

var SmokeScene: PackedScene


func _ready() -> void:
	SmokeScene = ResourceLoader.load("res://scenes/smoke_particles.tscn")


func _on_area_entered(area: Area3D) -> void:
	if area is Mallet:
		var smokeInstance: GPUParticles3D = SmokeScene.instantiate()
		add_child(smokeInstance)
		smokeInstance.emitting = true

extends MeshInstance3D
class_name Hole

var _goldMat: StandardMaterial3D
var _goldTween: Tween


func Flash(value: bool) -> void:
	var mat: ShaderMaterial = mesh.surface_get_material(0).next_pass
	mat.set_shader_parameter("Flashing", value)


## Marks this hole as the "shining" bonus hole - a pulsing gold material_override
## that sits on top of (and hides) the normal flash material while it's active.
func SetGolden(on: bool) -> void:
	if on:
		if _goldMat == null:
			_goldMat = StandardMaterial3D.new()
			_goldMat.albedo_color = Color(1.0, 0.78, 0.2)
			_goldMat.emission_enabled = true
			_goldMat.emission = Color(1.0, 0.7, 0.15)
			_goldMat.emission_energy_multiplier = 3.0
		material_override = _goldMat
		_goldTween = create_tween().set_loops()
		_goldTween.tween_property(_goldMat, "emission_energy_multiplier", 6.5, 0.5).set_trans(Tween.TRANS_SINE)
		_goldTween.tween_property(_goldMat, "emission_energy_multiplier", 2.5, 0.5).set_trans(Tween.TRANS_SINE)
	else:
		if _goldTween and _goldTween.is_valid():
			_goldTween.kill()
		material_override = null

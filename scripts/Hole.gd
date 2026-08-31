extends MeshInstance3D
class_name Hole


func Flash(value: bool) -> void:
	var mat: ShaderMaterial = mesh.surface_get_material(0).next_pass
	mat.set_shader_parameter("Flashing", value)

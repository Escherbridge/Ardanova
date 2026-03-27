@tool
extends EditorPlugin

func _enter_tree() -> void:
	add_autoload_singleton("ArdaNova", "res://addons/ardanova/ardanova_client.gd")

func _exit_tree() -> void:
	remove_autoload_singleton("ArdaNova")

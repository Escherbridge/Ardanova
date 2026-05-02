extends Node

## Basic test script for the ArdaNova Godot SDK.
## Run this scene to verify SDK functionality.

func _ready() -> void:
	print("[ArdaNova Test] Starting SDK tests...")

	# Test JWT Provider
	var jwt = ArdaNovaJwtProvider.new()
	assert(not jwt.has_token(), "Should start with no token")
	jwt.store_token("test-token-123")
	assert(jwt.has_token(), "Should have token after storing")
	assert(jwt.get_stored_token() == "test-token-123", "Token should match")
	jwt.clear_token()
	assert(not jwt.has_token(), "Should have no token after clearing")

	print("[ArdaNova Test] All tests passed!")

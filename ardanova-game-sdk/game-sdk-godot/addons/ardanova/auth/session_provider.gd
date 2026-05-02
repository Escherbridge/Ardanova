class_name ArdaNovaSessionProvider
extends RefCounted

## Session token management for ArdaNova authentication.
## Tokens are obtained by exchanging an auth code via the SDK API.

const TOKEN_SAVE_PATH = "user://ardanova_session.cfg"

var _config: ConfigFile


func _init() -> void:
	_config = ConfigFile.new()
	_config.load(TOKEN_SAVE_PATH)


## Store the session token persistently.
func store_token(token: String) -> void:
	_config.set_value("auth", "session_token", token)
	_config.save(TOKEN_SAVE_PATH)


## Retrieve the stored session token.
func get_stored_token() -> String:
	return _config.get_value("auth", "session_token", "")


## Clear the stored session token (logout).
func clear_token() -> void:
	_config.set_value("auth", "session_token", "")
	_config.save(TOKEN_SAVE_PATH)


## Check if a session token is stored.
func has_token() -> bool:
	return get_stored_token() != ""

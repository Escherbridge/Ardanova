extends Node

## ArdaNova Platform API Client for Godot.
## Routes through the Next.js SDK API layer (/api/sdk/*) which handles
## session authorization. Does NOT hit the .NET backend directly.
##
## Primary use cases:
## - Authenticate game players via ArdaNova web login
## - Gate content behind credential/tier requirements
## - Report game actions to earn equity/token rewards

signal authenticated(profile: Dictionary)
signal authentication_failed(error: String)
signal request_completed(result: Dictionary)
signal request_failed(error: String)

## Base URL of the Next.js application (e.g., https://app.ardanova.io).
## SDK requests are sent to /api/sdk/* routes on this host.
@export var api_base_url: String = "http://localhost:3000"

var _session_token: String = ""
var _http_request: HTTPRequest

## Content gating helper — use for credential/tier/token checks.
var gate: ArdaNovaCredentialGate


func _ready() -> void:
	_http_request = HTTPRequest.new()
	add_child(_http_request)
	gate = ArdaNovaCredentialGate.new(self)


## Set the session token for authenticated requests.
func set_session_token(token: String) -> void:
	_session_token = token


## Authenticate by exchanging an auth code for a session token.
## The auth code is obtained through the ArdaNova web login flow.
func authenticate(auth_code: String) -> void:
	var result = await _post("/api/sdk/auth/session", {"authCode": auth_code})
	if result.has("error"):
		authentication_failed.emit(result["error"])
	else:
		_session_token = result.get("sessionToken", "")
		authenticated.emit(result.get("profile", {}))


## Get the current authenticated user's profile.
func get_profile() -> Dictionary:
	return await _get("/api/sdk/me")


## Get all membership credentials for the current user.
func get_credentials() -> Array:
	var result = await _get("/api/sdk/me/credentials")
	if result is Array:
		return result
	return []


## Check if the current user holds a credential matching the given criteria.
func check_credential(project_id: String = "", guild_id: String = "", min_tier: String = "") -> Dictionary:
	var params: Array[String] = []
	if project_id != "":
		params.append("projectId=%s" % project_id)
	if guild_id != "":
		params.append("guildId=%s" % guild_id)
	if min_tier != "":
		params.append("minTier=%s" % min_tier)

	var query = "?" + "&".join(params) if params.size() > 0 else ""
	return await _get("/api/sdk/me/credentials/check%s" % query)


## Get all token balances for the current user.
func get_token_balances() -> Array:
	var result = await _get("/api/sdk/me/token-balances")
	if result is Array:
		return result
	return []


## Get token balance for a specific project.
func get_token_balance(project_id: String) -> Dictionary:
	return await _get("/api/sdk/me/token-balances/%s" % project_id)


## Report an in-game action to earn equity/XP.
## The platform determines the reward based on task configuration.
func report_action(action_type: String, task_id: String, metadata: Dictionary = {}) -> Dictionary:
	return await _post("/api/sdk/actions", {
		"actionType": action_type,
		"taskId": task_id,
		"metadata": metadata
	})


func _get(path: String) -> Variant:
	var url = api_base_url.rstrip("/") + path
	var headers = ["Content-Type: application/json"]
	if _session_token != "":
		headers.append("Authorization: Bearer %s" % _session_token)

	var error = _http_request.request(url, headers, HTTPClient.METHOD_GET)
	if error != OK:
		return {"error": "Request failed with code: %d" % error}

	var response = await _http_request.request_completed
	var response_code: int = response[1]
	var body: PackedByteArray = response[3]

	if response_code < 200 or response_code >= 300:
		var error_text = body.get_string_from_utf8()
		return {"error": "HTTP %d: %s" % [response_code, error_text]}

	var json = JSON.new()
	var parse_error = json.parse(body.get_string_from_utf8())
	if parse_error != OK:
		return {"error": "Failed to parse JSON response"}

	return json.data


func _post(path: String, data: Dictionary) -> Variant:
	var url = api_base_url.rstrip("/") + path
	var headers = ["Content-Type: application/json"]
	if _session_token != "":
		headers.append("Authorization: Bearer %s" % _session_token)

	var json_body = JSON.stringify(data)
	var error = _http_request.request(url, headers, HTTPClient.METHOD_POST, json_body)
	if error != OK:
		return {"error": "Request failed with code: %d" % error}

	var response = await _http_request.request_completed
	var response_code: int = response[1]
	var body: PackedByteArray = response[3]

	if response_code < 200 or response_code >= 300:
		var error_text = body.get_string_from_utf8()
		return {"error": "HTTP %d: %s" % [response_code, error_text]}

	var json = JSON.new()
	var parse_error = json.parse(body.get_string_from_utf8())
	if parse_error != OK:
		return {"error": "Failed to parse JSON response"}

	return json.data

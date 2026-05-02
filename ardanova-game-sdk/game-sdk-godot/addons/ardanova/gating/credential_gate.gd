class_name ArdaNovaCredentialGate
extends RefCounted

## High-level content gating helpers. Use these to check whether the
## current player meets credential or token requirements before
## unlocking game content.
##
## Usage:
##   if await ArdaNova.gate.has_credential("project-id"):
##       enable_premium_content()
##
##   if await ArdaNova.gate.has_min_tier("project-id", "GOLD"):
##       enable_gold_content()

var _client


func _init(client) -> void:
	_client = client


## Check if the current user holds any active credential for the given scope.
func has_credential(project_id: String = "", guild_id: String = "") -> bool:
	var result = await _client.check_credential(project_id, guild_id)
	return result.get("hasCredential", false)


## Check if the current user holds a credential at or above the given tier.
## Tier order: BRONZE < SILVER < GOLD < PLATINUM < DIAMOND
func has_min_tier(project_id: String, min_tier: String, guild_id: String = "") -> bool:
	var result = await _client.check_credential(project_id, guild_id, min_tier)
	return result.get("hasCredential", false) and result.get("meetsMinTier", false)


## Check if the current user has at least the given token balance for a project.
func has_token_balance(project_id: String, min_balance: int) -> bool:
	var result = await _client.get_token_balance(project_id)
	if result.has("error"):
		return false
	return result.get("balance", 0) >= min_balance

class_name ArdaNovaCredential
extends RefCounted

## ArdaNova membership credential data model.

var id: String
var project_id: String
var guild_id: String
var user_id: String
var asset_id: String
var status: String  # ACTIVE, REVOKED, SUSPENDED
var is_transferable: bool
var tier: String  # BRONZE, SILVER, GOLD, PLATINUM, DIAMOND
var granted_via: String
var mint_tx_hash: String
var created_at: String


static func from_dict(data: Dictionary) -> ArdaNovaCredential:
	var cred = ArdaNovaCredential.new()
	cred.id = data.get("id", "")
	cred.project_id = data.get("projectId", "")
	cred.guild_id = data.get("guildId", "")
	cred.user_id = data.get("userId", "")
	cred.asset_id = data.get("assetId", "")
	cred.status = data.get("status", "")
	cred.is_transferable = data.get("isTransferable", false)
	cred.tier = data.get("tier", "")
	cred.granted_via = data.get("grantedVia", "")
	cred.mint_tx_hash = data.get("mintTxHash", "")
	cred.created_at = data.get("createdAt", "")
	return cred

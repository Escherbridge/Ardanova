class_name ArdaNovaTokenBalance
extends RefCounted

## ArdaNova token balance data model.

var id: String
var user_id: String
var project_token_config_id: String
var is_platform_token: bool
var balance: int
var locked_balance: int


static func from_dict(data: Dictionary) -> ArdaNovaTokenBalance:
	var tb = ArdaNovaTokenBalance.new()
	tb.id = data.get("id", "")
	tb.user_id = data.get("userId", "")
	tb.project_token_config_id = data.get("projectTokenConfigId", "")
	tb.is_platform_token = data.get("isPlatformToken", false)
	tb.balance = data.get("balance", 0)
	tb.locked_balance = data.get("lockedBalance", 0)
	return tb

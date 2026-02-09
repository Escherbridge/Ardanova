class_name ArdaNovaUserProfile
extends RefCounted

## ArdaNova user profile data model.

var id: String
var name: String
var email: String
var image: String
var role: String
var user_type: String
var verification_level: String


static func from_dict(data: Dictionary) -> ArdaNovaUserProfile:
	var profile = ArdaNovaUserProfile.new()
	profile.id = data.get("id", "")
	profile.name = data.get("name", "")
	profile.email = data.get("email", "")
	profile.image = data.get("image", "")
	profile.role = data.get("role", "")
	profile.user_type = data.get("userType", "")
	profile.verification_level = data.get("verificationLevel", "")
	return profile

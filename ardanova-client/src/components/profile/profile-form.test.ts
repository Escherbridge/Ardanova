import { describe, expect, it } from "vitest";

import { profileFormFrom, profileUpdateFrom } from "./profile-form";
import { profileIdentityLabels } from "./profile-labels";

const saved = profileFormFrom({
  name: "Ada",
  bio: "Builds tools.",
  website: "https://ada.example",
  phone: null,
});

describe("profile edit payload", () => {
  it.each([
    ["nothing changed", { ...saved }, {}],
    ["only whitespace changed", { ...saved, bio: "  Builds tools.  " }, {}],
    ["a bio edit", { ...saved, bio: "Ships tools." }, { bio: "Ships tools." }],
    ["a cleared website", { ...saved, website: "  " }, { website: "" }],
    ["a bare-domain website", { ...saved, website: "ada.org" }, { website: "https://ada.org" }],
    ["a cleared name", { ...saved, name: "" }, {}],
    ["a new phone", { ...saved, phone: " +1 555 0100 " }, { phone: "+1 555 0100" }],
  ])("sends only real changes for %s", (_case, values, expected) => {
    expect(profileUpdateFrom(values, saved)).toEqual(expected);
  });
});

describe("profile identity badges", () => {
  it.each([
    [{ userType: "VOLUNTEER", verificationLevel: "ANONYMOUS" }, { userType: "Volunteer", verification: null }],
    [{ userType: "PROJECT_FOUNDER", verificationLevel: "VERIFIED" }, { userType: "Project founder", verification: "Verified" }],
    [{ userType: undefined, verificationLevel: undefined }, { userType: "Member", verification: null }],
  ])("labels %o as %o", (identity, expected) => {
    expect(profileIdentityLabels(identity)).toEqual(expected);
  });
});

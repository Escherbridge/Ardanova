import { describe, expect, it } from "vitest";

import { canGrantRole, memberExitAction } from "./member-authority";

describe("memberExitAction", () => {
  it.each([
    // viewerRole, viewer is the member, memberRole, expected
    ["FOUNDER", false, "LEADER", "remove"],
    ["LEADER", false, "CORE_CONTRIBUTOR", "remove"],
    ["CORE_CONTRIBUTOR", false, "CONTRIBUTOR", "remove"],
    ["CORE_CONTRIBUTOR", false, "CORE_CONTRIBUTOR", null],
    ["CORE_CONTRIBUTOR", false, "LEADER", null],
    ["CONTRIBUTOR", false, "OBSERVER", null],
    [undefined, false, "OBSERVER", null],
    ["CONTRIBUTOR", true, "CONTRIBUTOR", "leave"],
    ["LEADER", true, "LEADER", "leave"],
    ["FOUNDER", true, "FOUNDER", null],
    ["LEADER", false, "FOUNDER", null],
  ] as const)(
    "%s viewer (self=%s) on a %s row -> %s",
    (viewerRole, isSelf, memberRole, expected) => {
      expect(
        memberExitAction({
          viewerId: "viewer",
          viewerRole,
          memberUserId: isSelf ? "viewer" : "someone-else",
          memberRole,
        }),
      ).toBe(expected);
    },
  );

  it("offers nothing to a signed-out viewer", () => {
    expect(
      memberExitAction({
        viewerId: undefined,
        viewerRole: undefined,
        memberUserId: "member",
        memberRole: "CONTRIBUTOR",
      }),
    ).toBeNull();
  });
});

describe("canGrantRole", () => {
  it.each([
    ["FOUNDER", "FOUNDER", false],
    ["FOUNDER", "LEADER", true],
    ["LEADER", "LEADER", false],
    ["LEADER", "CORE_CONTRIBUTOR", true],
    ["CORE_CONTRIBUTOR", "CONTRIBUTOR", true],
    ["CORE_CONTRIBUTOR", "LEADER", false],
    [undefined, "OBSERVER", false],
  ] as const)("%s may grant %s: %s", (viewerRole, role, expected) => {
    expect(canGrantRole(viewerRole, role)).toBe(expected);
  });
});

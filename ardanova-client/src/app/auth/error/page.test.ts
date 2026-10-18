import { describe, expect, it } from "vitest";

import { cn } from "~/lib/utils";

import { ACCOUNT_NOT_LINKED_REDIRECT } from "~/server/auth/error-codes";

import {
  authErrorMessages,
  resolveAuthErrorReference,
  retrySignInHref,
} from "./copy";
import { authErrorSecondaryButtonClass } from "./styles";

describe("authentication error copy", () => {
  it("explains ArdaNova's own email-collision refusal instead of blaming the provider", () => {
    const code = new URL(ACCOUNT_NOT_LINKED_REDIRECT, "http://local").searchParams.get("error");
    const reference = resolveAuthErrorReference(code ?? undefined);

    expect(reference).toBe("AccountNotLinked");
    expect(authErrorMessages[reference].description).toMatch(/already uses this email/);
  });

  it("does not attribute a generic access denial to the provider alone", () => {
    expect(authErrorMessages.AccessDenied.description).toMatch(/provider or by ArdaNova/);
  });

  it.each([undefined, "Nonsense", "__proto__", "constructor"])(
    "falls back to the default copy for %s",
    (code) => expect(resolveAuthErrorReference(code)).toBe("Default"),
  );

  it.each([
    ["/projects/alpha", "%2Fprojects%2Falpha"],
    ["//evil.example", "%2Fdashboard"],
    [undefined, "%2Fdashboard"],
  ])("retrying from %s returns to %s", (callbackUrl, encoded) => {
    expect(retrySignInHref(callbackUrl)).toBe(`/auth/signin?callbackUrl=${encoded}`);
  });
});

describe("authentication error page", () => {
  it("keeps the secondary public-site action legible on the dark panel", () => {
    const classes = cn("bg-background", authErrorSecondaryButtonClass);

    expect(classes).toContain("bg-transparent");
    expect(classes).toContain("text-[#f6f0eb]");
    expect(classes).not.toContain("bg-background");
  });
});

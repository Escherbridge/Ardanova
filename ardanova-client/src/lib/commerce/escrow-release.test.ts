import { describe, expect, it } from "vitest";

import { validateTransactionReference } from "./escrow-release";

describe("validateTransactionReference", () => {
  it("accepts an omitted optional reference", () => {
    expect(validateTransactionReference("   ")).toEqual({
      isValid: true,
      value: undefined,
      error: null,
    });
  });

  it.each([
    "0x0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
    "A234567890BCDEFGHIJKLMNOPQRSTUVWXYZ234567890BCDEFGHIJ",
    "d9428888-122b-11e1-b85c-61cd3cbb3210",
  ])("accepts a bounded opaque reference: %s", (reference) => {
    expect(validateTransactionReference(reference)).toMatchObject({
      isValid: true,
      value: reference,
    });
  });

  it.each(["short", "reference with spaces", "<script>alert(1)</script>"])(
    "rejects a malformed reference: %s",
    (reference) => {
      expect(validateTransactionReference(reference)).toMatchObject({
        isValid: false,
      });
    },
  );
});

import { describe, expect, it } from "vitest";
import {
  getCredentialProofPresentation,
  getCredentialProofReference,
  getTransferabilityLabel,
  type CredentialProofReport,
} from "./credential-proof";

describe("reading a credential's proof", () => {
  it("lets a withdrawn credential outrank however good its proof is", () => {
    expect(
      getCredentialProofPresentation({
        status: "REVOKED",
        chainVerified: true,
        assetId: "12345",
      }).state,
    ).toBe("withdrawn");
  });

  it("lets a paused credential outrank its proof too", () => {
    expect(
      getCredentialProofPresentation({
        status: "SUSPENDED",
        isOnChain: true,
      }).state,
    ).toBe("paused");
  });

  it("prefers a live verification over a merely recorded one", () => {
    expect(
      getCredentialProofPresentation({
        status: "ACTIVE",
        isIndependentlyVerifiable: true,
        isOnChain: true,
        chainVerified: true,
      }).state,
    ).toBe("verified");
    expect(
      getCredentialProofPresentation({
        status: "ACTIVE",
        isIndependentlyVerifiable: true,
        isOnChain: true,
        chainVerified: false,
      }).state,
    ).toBe("recorded");
  });

  it("treats a reference on its own as recorded, once the node says it issued it", () => {
    expect(
      getCredentialProofPresentation({
        status: "ACTIVE",
        isIndependentlyVerifiable: true,
        mintTxHash: "0xabc",
      }).state,
    ).toBe("recorded");
  });

  it("refuses every proof reading for a credential the node did not issue", () => {
    // The whole point of the flag. A row can carry a fabricated identifier — the shipped
    // simulated provider made one up — and identifier PRESENCE must decide nothing. This is
    // the same question both screens ask, so neither can read "Verified" while the other
    // reads "Verification pending" about the same credential.
    const unissued: CredentialProofReport[] = [
      { status: "ACTIVE", assetId: "12345" },
      { status: "ACTIVE", mintTxHash: "0xabc" },
      { status: "ACTIVE", isOnChain: true },
      { status: "ACTIVE", chainVerified: true },
      { status: "ACTIVE", isIndependentlyVerifiable: false, chainVerified: true },
      { status: "ACTIVE", isIndependentlyVerifiable: null, assetId: "12345" },
    ];

    for (const report of unissued) {
      expect(getCredentialProofPresentation(report).state).toBe("preparing");
      expect(getCredentialProofReference(report)).toBeNull();
    }
  });

  it("treats an absent proof as still being prepared, never as a failure", () => {
    expect(getCredentialProofPresentation({ status: "ACTIVE" }).state).toBe(
      "preparing",
    );
    expect(getCredentialProofPresentation(null).state).toBe("preparing");
    expect(
      getCredentialProofPresentation({ status: "ACTIVE" }).detail,
    ).toContain("still being prepared");
  });

  it("reads a status whatever case or padding the node sends", () => {
    expect(getCredentialProofPresentation({ status: "  revoked " }).state).toBe(
      "withdrawn",
    );
  });

  it("offers one reference, preferring the asset id, and never an empty one", () => {
    const issued = { isIndependentlyVerifiable: true } as const;
    expect(
      getCredentialProofReference({
        ...issued,
        assetId: "12345",
        mintTxHash: "0xabc",
      }),
    ).toBe("12345");
    expect(
      getCredentialProofReference({ ...issued, mintTxHash: "0xabc" }),
    ).toBe("0xabc");
    expect(
      getCredentialProofReference({
        ...issued,
        assetId: "   ",
        mintTxHash: "0xabc",
      }),
    ).toBe("0xabc");
    expect(getCredentialProofReference({ ...issued, assetId: "   " })).toBeNull();
    expect(getCredentialProofReference({ ...issued })).toBeNull();
    expect(getCredentialProofReference({})).toBeNull();
    expect(getCredentialProofReference(null)).toBeNull();
  });

  it("says a credential stays with you without saying soulbound", () => {
    expect(getTransferabilityLabel(true)).toBe("Yes");
    expect(getTransferabilityLabel(false).toLowerCase()).not.toContain(
      "soulbound",
    );
    expect(getTransferabilityLabel(undefined).toLowerCase()).not.toContain(
      "soulbound",
    );
  });
});

describe("the words a non-crypto member is shown about a credential", () => {
  // The exact clause list from the product test, plus the terms the page used to render.
  const forbidden = [
    "wallet",
    "chain",
    "blockchain",
    "algorand",
    "token",
    "mint",
    "burn",
    "asa",
    "asset",
    "gas",
    "mnemonic",
    "soulbound",
    "transaction hash",
    "address",
    "crypto",
  ];

  const everyReport: (CredentialProofReport | null | undefined)[] = [
    { status: "REVOKED" },
    { status: "SUSPENDED" },
    { status: "ACTIVE", isIndependentlyVerifiable: true, chainVerified: true },
    { status: "ACTIVE", isIndependentlyVerifiable: true, isOnChain: true },
    { status: "ACTIVE", isIndependentlyVerifiable: true, assetId: "12345" },
    { status: "ACTIVE", isIndependentlyVerifiable: true, mintTxHash: "0xabc" },
    { status: "ACTIVE", chainVerified: true },
    { status: "ACTIVE", isOnChain: true },
    { status: "ACTIVE", assetId: "12345" },
    { status: "ACTIVE", mintTxHash: "0xabc" },
    { status: "ACTIVE" },
    { status: "who_knows" },
    null,
    undefined,
  ];

  it("never names a piece of blockchain machinery in any reachable copy", () => {
    for (const report of everyReport) {
      const { label, detail } = getCredentialProofPresentation(report);
      const copy = `${label} ${detail}`.toLowerCase();

      for (const word of forbidden) {
        expect(copy, `"${word}" reached the member via: ${copy}`).not.toContain(
          word,
        );
      }
    }
  });

  it("never names one in the transferability label either", () => {
    for (const value of [true, false, null, undefined]) {
      const copy = getTransferabilityLabel(value).toLowerCase();
      for (const word of forbidden) {
        expect(copy, `"${word}" reached the member via: ${copy}`).not.toContain(
          word,
        );
      }
    }
  });
});

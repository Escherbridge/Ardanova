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
        isOnChain: true,
        chainVerified: true,
      }).state,
    ).toBe("verified");
    expect(
      getCredentialProofPresentation({
        status: "ACTIVE",
        isOnChain: true,
        chainVerified: false,
      }).state,
    ).toBe("recorded");
  });

  it("treats a reference on its own as recorded, which is what the card had", () => {
    expect(
      getCredentialProofPresentation({
        status: "ACTIVE",
        mintTxHash: "0xabc",
      }).state,
    ).toBe("recorded");
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
    expect(
      getCredentialProofReference({ assetId: "12345", mintTxHash: "0xabc" }),
    ).toBe("12345");
    expect(getCredentialProofReference({ mintTxHash: "0xabc" })).toBe("0xabc");
    expect(
      getCredentialProofReference({ assetId: "   ", mintTxHash: "0xabc" }),
    ).toBe("0xabc");
    expect(getCredentialProofReference({ assetId: "   " })).toBeNull();
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

// @vitest-environment jsdom
import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AzoaMoneyProvider } from "azoa-sdk/react";
import { createKeyStore } from "azoa-sdk/money";
import { createArdaMoneyTransport, type MoneyHost } from "./transport";
import { ArdaConsentStatus } from "./consent-status";

afterEach(cleanup);

function account(ownership: "custodial" | "claimed", consent = "granted") {
  return {
    ownership,
    consent,
    consentScopes: consent === "granted" ? ["kyc:read"] : [],
    consentExpiresAt: null,
    ready: consent === "granted",
    identityReady: true,
    kycReady: true,
    walletReady: true,
    kycStatus: "Approved",
  };
}

function renderPermission(accountStatus: MoneyHost["accountStatus"]) {
  const unavailable = async () => {
    throw new Error("Unexpected operation.");
  };
  const revokeConsent = vi.fn(async () => ({ revoked: true, revokedAt: null }));
  const transport = createArdaMoneyTransport({
    accountStatus,
    revokeConsent,
    voidCashOut: unavailable,
    voidSale: unavailable,
    accountCapabilities: unavailable,
    ensureAccount: unavailable,
    beginKyc: unavailable,
    checkoutCapabilities: unavailable,
    beginCheckout: unavailable,
    getCheckout: unavailable,
    voidCheckout: unavailable,
    cashOutAvailability: unavailable,
    cashOutDestination: unavailable,
    startCashOut: unavailable,
    cashOutHistory: unavailable,
    sellable: unavailable,
    quoteSale: unavailable,
    sell: unavailable,
  });
  render(
    <AzoaMoneyProvider
      subject="ada"
      keyStore={createKeyStore({ subject: "ada", storage: localStorage })}
      transport={transport}
    >
      <ArdaConsentStatus />
    </AzoaMoneyProvider>,
  );
  return revokeConsent;
}

async function withdrawPermission() {
  fireEvent.click(
    await screen.findByRole("button", { name: "Withdraw permission" }),
  );
  await waitFor(() =>
    expect(
      screen
        .getByRole("button", { name: "Yes, withdraw permission" })
        .hasAttribute("disabled"),
    ).toBe(false),
  );
  fireEvent.click(
    screen.getByRole("button", { name: "Yes, withdraw permission" }),
  );
}

describe("revocation state without a manufactured event receipt", () => {
  it.each(["custodial", "claimed"] as const)(
    "refreshes a null receipt once and confirms current inactive %s permission",
    async (ownership) => {
      const status = vi
        .fn()
        .mockResolvedValueOnce(account(ownership))
        .mockResolvedValue(account(ownership, "none"));
      const revoke = renderPermission(status);
      await withdrawPermission();
      expect(
        await screen.findByText(/Payment permission is inactive/),
      ).toBeTruthy();
      expect(revoke).toHaveBeenCalledTimes(1);
      expect(status).toHaveBeenCalledTimes(2);
      expect(
        screen.queryByRole("button", { name: "Withdraw permission" }),
      ).toBeNull();
    },
  );

  it("does not loop a failed confirmation read and permits an explicit recheck", async () => {
    const status = vi
      .fn()
      .mockResolvedValueOnce(account("claimed"))
      .mockRejectedValueOnce(new Error("Connection lost."))
      .mockResolvedValue(account("claimed", "none"));
    const revoke = renderPermission(status);
    await withdrawPermission();
    await waitFor(() => expect(status).toHaveBeenCalledTimes(2));
    await waitFor(() =>
      expect(
        screen
          .getByRole("button", { name: "Check permission" })
          .hasAttribute("disabled"),
      ).toBe(false),
    );
    expect(screen.queryByText(/Payment permission is inactive/)).toBeNull();
    expect(revoke).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "Check permission" }));
    expect(
      await screen.findByText(/Payment permission is inactive/),
    ).toBeTruthy();
    expect(status).toHaveBeenCalledTimes(3);
    expect(revoke).toHaveBeenCalledTimes(1);
  });
});

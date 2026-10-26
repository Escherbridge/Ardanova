// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import {
  ACCOUNT_SETUP_NOT_OPEN,
  ACCOUNT_STATUS_UNREADABLE,
} from "./account-setup-copy";
import { CustodialAccountCard } from "./custodial-account-card";

const startSetup = { name: /start account setup/i };

describe("CustodialAccountCard", () => {
  it("hides Start account setup when the status read was refused with PRECONDITION_FAILED", () => {
    render(
      <CustodialAccountCard
        statusError="Setup refused."
        statusErrorCode="PRECONDITION_FAILED"
        onEnsure={vi.fn()}
      />,
    );

    expect(screen.queryByRole("button", startSetup)).toBeNull();
    expect(screen.getByRole("status").textContent).toBe(ACCOUNT_SETUP_NOT_OPEN);
  });

  it("keeps Start account setup on offer after a transient status failure", () => {
    render(
      <CustodialAccountCard
        statusError="Service temporarily unreachable."
        statusErrorCode="INTERNAL_SERVER_ERROR"
        onEnsure={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", startSetup).hasAttribute("disabled")).toBe(
      false,
    );
    expect(screen.getByRole("status").textContent).toBe(
      ACCOUNT_STATUS_UNREADABLE,
    );
  });

  it("never prints the server's raw ensure error", () => {
    const { container } = render(
      <CustodialAccountCard
        ensureError="AZOA tenant-bound custodial account onboarding is not configured."
        onEnsure={vi.fn()}
      />,
    );

    expect(screen.getByRole("alert").textContent).not.toMatch(/azoa|tenant/i);
    expect(container.textContent).not.toContain("tenant-bound");
  });

  it("names every step state for assistive tech", () => {
    render(
      <CustodialAccountCard
        status={{ identityReady: true, walletReady: false } as never}
        onEnsure={vi.fn()}
      />,
    );

    expect(screen.getAllByRole("img", { name: "Complete" })).toHaveLength(1);
    expect(screen.getAllByRole("img", { name: "Not complete" })).toHaveLength(
      2,
    );
  });
});

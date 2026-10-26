// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { AppTopbar } from "./app-topbar";

vi.mock("next/navigation", () => ({ usePathname: () => "/dashboard" }));
vi.mock("next-auth/react", () => ({ signOut: vi.fn() }));
vi.mock("~/components/ai/nova-assistant", () => ({ NovaAssistant: () => null }));

describe("AppTopbar mobile drawer", () => {
  it("opens with focus on the first primary navigation link", async () => {
    render(<AppTopbar user={{ name: "Sam Sponsor" }} />);

    fireEvent.click(
      screen.getByRole("button", { name: "Open workspace navigation" }),
    );

    const navigation = await screen.findByRole("navigation", {
      name: "Primary workspace navigation",
    });
    const firstLink = within(navigation).getAllByRole("link")[0];
    await waitFor(() => expect(document.activeElement).toBe(firstLink));
  });
});

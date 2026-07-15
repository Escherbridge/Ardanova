import { describe, expect, it, vi } from "vitest";

import { navigateToAcceptedBidCommerce } from "./bid-accept-navigation";

describe("bid acceptance commerce navigation", () => {
  it("navigates to the exact internal commerceUrl returned by the server", () => {
    const navigate = vi.fn();

    navigateToAcceptedBidCommerce("/tasks/task-123/commerce", navigate);

    expect(navigate).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith("/tasks/task-123/commerce");
  });

  it.each([
    "https://malicious.example/tasks/task-123/commerce",
    "//malicious.example/tasks/task-123/commerce",
    "/tasks/task-123/commerce?settled=true",
    "/tasks/task-123",
  ])("rejects a non-canonical server response route: %s", (commerceUrl) => {
    const navigate = vi.fn();

    expect(() => navigateToAcceptedBidCommerce(commerceUrl, navigate)).toThrow(
      "Bid acceptance returned an invalid commerce route.",
    );
    expect(navigate).not.toHaveBeenCalled();
  });
});

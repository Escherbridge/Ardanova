import { describe, expect, it } from "vitest";

import { POST } from "./route";

describe("SDK session exchange", () => {
  it("fails closed until a single-use authorization-code contract exists", async () => {
    const response = await POST();
    const body = (await response.json()) as { error: string };

    expect(response.status).toBe(501);
    expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(body.error).toContain("single-use authorization codes");
    expect(body).not.toHaveProperty("sessionToken");
    expect(body).not.toHaveProperty("profile");
  });
});

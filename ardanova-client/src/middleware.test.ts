import { NextRequest } from "next/server";
import { describe, expect, it } from "vitest";

import { PROJECTS_EXPLAINER_HEADER } from "~/lib/auth-navigation";

import { ardanovaRequestHeaders, config, middleware } from "./middleware";

describe("edge middleware", () => {
  it.each(["/", "/projects/demo", "/api/trpc/project.getMembers"])(
    "hardens the response for %s",
    (path) => {
      const response = middleware(
        new NextRequest(`http://localhost:3000${path}`),
      );

      expect(response.headers.get("X-Content-Type-Options")).toBe("nosniff");
      expect(response.headers.get("X-Frame-Options")).toBe("DENY");
      expect(response.headers.get("Content-Security-Policy")).toBe(
        "frame-ancestors 'none'",
      );
      expect(response.headers.get("Referrer-Policy")).toBe(
        "strict-origin-when-cross-origin",
      );
      expect(response.headers.get("Strict-Transport-Security")).toBeNull();
    },
  );

  it("marks only a signed-out-eligible GET /projects for the explainer", () => {
    const header = (path: string, method = "GET") =>
      ardanovaRequestHeaders(
        new NextRequest(`http://localhost:3000${path}`, { method }),
      ).get(PROJECTS_EXPLAINER_HEADER);

    expect(header("/projects")).toBe("1");
    expect(header("/projects", "POST")).toBeNull();
    expect(header("/projects/demo")).toBeNull();
    expect(header("/opportunities")).toBeNull();
    expect(header("/governance")).toBeNull();
  });

  it("runs on public pages and the API, not on build assets", () => {
    const [pattern] = config.matcher;
    const matches = (path: string) => new RegExp(`^${pattern}$`).test(path);

    expect(matches("/")).toBe(true);
    expect(matches("/events/launch")).toBe(true);
    expect(matches("/api/trpc/event.list")).toBe(true);
    expect(matches("/_next/static/chunks/main.js")).toBe(false);
  });
});

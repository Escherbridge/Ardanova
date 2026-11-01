import {
  getAccessFallbackHTTPStatus,
  isHTTPAccessFallbackError,
} from "next/dist/client/components/http-access-fallback/http-access-fallback";
import { getURLFromRedirectError } from "next/dist/client/components/redirect";
import { isRedirectError } from "next/dist/client/components/redirect-error";
import { isValidElement } from "react";
import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
// Signed out: no session. The request path is what the middleware stamps on every page request.
vi.mock("~/server/auth", () => ({
  auth: async () => null,
  authForPage: async () => null,
  isDevelopmentAuthPreviewEnabled: false,
}));
vi.mock("next/headers", () => ({
  headers: async () =>
    new Headers({ "x-ardanova-request-path": "/tag/carpentry" }),
}));
vi.mock("~/components/ai/nova-assistant", () => ({
  NovaAssistant: () => null,
}));

const { default: TagLayout } = await import("./layout");
const { default: TagPage } = await import("./page");

/** Renders nested server components the way the RSC renderer walks them, down to the first host or client element. */
async function renderServerComponents(node: unknown): Promise<unknown> {
  let current = await node;
  while (isValidElement(current) && typeof current.type === "function") {
    const component = current.type as (props: unknown) => unknown;
    current = await component(current.props);
  }
  return current;
}

/** What a server render ended in: a redirect, a 404, or the tag the stream renders for. */
async function outcomeOf(render: Promise<unknown>) {
  try {
    const element = (await render) as { props: { tag?: string } };
    return { streamFor: element.props.tag };
  } catch (error) {
    if (isRedirectError(error)) {
      return { redirectedTo: getURLFromRedirectError(error) };
    }
    if (isHTTPAccessFallbackError(error)) {
      return { status: getAccessFallbackHTTPStatus(error) };
    }
    throw error;
  }
}

describe("/tag/[tag] (plan §1: signed-in only)", () => {
  it("sends a signed-out reader to sign in, then back to the same tag", async () => {
    await expect(
      outcomeOf(renderServerComponents(TagLayout({ children: null }))),
    ).resolves.toEqual({
      redirectedTo: "/auth/signin?callbackUrl=%2Ftag%2Fcarpentry",
    });
  });

  it.each([
    // A sector chip's link (feedRoutes.tag(sectorTag("FOOD_BEVERAGE"))).
    ["food-beverage", { streamFor: "food-beverage" }],
    // feedRoutes.tag percent-encodes; the page reads the tag as typed.
    ["caf%C3%A9", { streamFor: "café" }],
    ["Carpentry", { redirectedTo: "/tag/carpentry" }],
    ["roofing-", { redirectedTo: "/tag/roofing" }],
    ["x", { status: 404 }],
  ])("one URL per tag: /tag/%s", async (segment, expected) => {
    await expect(
      outcomeOf(TagPage({ params: Promise.resolve({ tag: segment }) })),
    ).resolves.toEqual(expected);
  });
});

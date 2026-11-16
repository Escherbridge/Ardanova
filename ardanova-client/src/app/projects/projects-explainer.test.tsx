// @vitest-environment jsdom
import { isValidElement, type ReactNode } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { getURLFromRedirectError } from "next/dist/client/components/redirect";
import { isRedirectError } from "next/dist/client/components/redirect-error";
import { describe, expect, it, vi } from "vitest";

import { PROJECTS_EXPLAINER_HEADER } from "~/lib/auth-navigation";

vi.mock("server-only", () => ({}));

const state = vi.hoisted(() => ({
  session: null as { user: { id: string } } | null,
  headerValues: {} as Record<string, string>,
}));

vi.mock("~/server/auth", () => ({
  auth: async () => state.session,
  authForPage: async () => state.session,
  providerAuth: async () => state.session,
  isDevelopmentAuthPreviewEnabled: false,
  signIn: async () => undefined,
}));
vi.mock("next/headers", () => ({
  headers: async () => new Headers(state.headerValues),
}));
vi.mock("~/trpc/server", () => ({
  HydrateClient: (props: { children?: ReactNode }) => props.children ?? null,
}));

const { default: ProjectsLayout } = await import("./layout");
const { default: ProjectsPage, generateMetadata } = await import("./page");
const { default: ProjectsBrowser } = await import("./projects-browser");
const { default: AuthenticatedLayout } = await import(
  "~/components/layouts/authenticated-layout"
);
const { default: OpportunitiesLayout } = await import(
  "../opportunities/layout"
);
const { default: GovernanceLayout } = await import("../governance/layout");
const { default: SignInPage } = await import("../auth/signin/page");
const { default: Home } = await import("../page");
const { PROJECTS_DESCRIPTION, PROJECTS_RECORD, SIMULATED_ECONOMY } =
  await import("./projects-explainer");

async function renderServerComponents(node: unknown): Promise<unknown> {
  let current = await node;
  while (isValidElement(current) && typeof current.type === "function") {
    const component = current.type as (props: unknown) => unknown;
    current = await component(current.props);
  }
  return current;
}

async function outcomeOf(render: Promise<unknown>) {
  try {
    await render;
    return { redirectedTo: null };
  } catch (error) {
    if (isRedirectError(error)) {
      return { redirectedTo: getURLFromRedirectError(error) };
    }
    throw error;
  }
}

describe("signed-out /projects explainer", () => {
  it("returns the explainer in the server HTML, with both account buttons", async () => {
    state.session = null;
    state.headerValues = { [PROJECTS_EXPLAINER_HEADER]: "1" };

    const html = renderToStaticMarkup(await ProjectsPage());

    expect(html).toContain("<h1");
    expect(html).toContain(">Projects</h1>");
    expect(html).toContain(PROJECTS_DESCRIPTION);
    expect(html).toContain(PROJECTS_RECORD.replaceAll("'", "&#x27;"));
    expect(html).toContain(SIMULATED_ECONOMY);
    expect(html).toContain(
      'href="/auth/signin?mode=signup&amp;callbackUrl=%2Fprojects"',
    );
    expect(html).toContain(">Create an account with Google</");
    expect(html).toContain('href="/auth/signin?callbackUrl=%2Fprojects"');
    expect(html).toContain(">Sign in</");
    expect(html).toContain('href="/patterns"');
    expect(html).toContain("Read the patterns first");
    await expect(generateMetadata()).resolves.toEqual({
      title: "Projects",
      description: PROJECTS_DESCRIPTION,
    });
  });

  it("leaves the signed-in index on the existing projects browser", async () => {
    state.session = { user: { id: "user-1" } };
    state.headerValues = { [PROJECTS_EXPLAINER_HEADER]: "1" };

    const element = await ProjectsPage();

    expect(isValidElement(element) && element.type).toBe(ProjectsBrowser);
    await expect(generateMetadata()).resolves.toEqual({});
  });

  it("does not wrap the explainer in the workspace shell", async () => {
    state.session = null;
    state.headerValues = {
      [PROJECTS_EXPLAINER_HEADER]: "1",
      "x-ardanova-request-path": "/projects",
    };

    const element = await ProjectsLayout({ children: <p>explainer</p> });

    expect(isValidElement(element) && element.type).toBe("p");
  });

  it("keeps a signed-in /projects inside the workspace shell", async () => {
    state.session = { user: { id: "user-1" } };
    state.headerValues = { [PROJECTS_EXPLAINER_HEADER]: "1" };

    const element = await ProjectsLayout({ children: <p>list</p> });

    expect(isValidElement(element) && element.type).toBe(AuthenticatedLayout);
  });

  it("still sends signed-out visitors on other project routes to sign in", async () => {
    state.session = null;
    state.headerValues = { "x-ardanova-request-path": "/projects/create" };

    await expect(
      outcomeOf(
        renderServerComponents(ProjectsLayout({ children: <p>create</p> })),
      ),
    ).resolves.toEqual({
      redirectedTo: "/auth/signin?callbackUrl=%2Fprojects%2Fcreate",
    });
  });

  it.each([
    ["../opportunities/layout", OpportunitiesLayout, "/opportunities"],
    ["../governance/layout", GovernanceLayout, "/governance"],
  ])("keeps %s gated", async (_name, Layout, path) => {
    state.session = null;
    state.headerValues = { "x-ardanova-request-path": path };

    await expect(
      outcomeOf(renderServerComponents(Layout({ children: null }))),
    ).resolves.toEqual({
      redirectedTo: `/auth/signin?callbackUrl=${encodeURIComponent(path)}`,
    });
  });
});

describe("public entry points", () => {
  it("sends a signed-out reader to /projects from the hero, nav, and footer", async () => {
    state.session = null;
    state.headerValues = {};
    const html = renderToStaticMarkup(await Home());

    expect(html).toContain('href="/projects"');
    expect(html).toContain("Discover projects");
    expect(html).not.toContain("callbackUrl=%2Fprojects");
    expect(html).not.toContain('href="/opportunities"');
    expect(html).not.toContain('href="/governance"');
  });

  it("keeps signed-in catalog links on their own routes", async () => {
    state.session = { user: { id: "user-1" } };
    state.headerValues = {};
    const html = renderToStaticMarkup(await Home());

    expect(html).toContain('href="/projects"');
    expect(html).toContain('href="/opportunities"');
    expect(html).toContain('href="/governance"');
  });

  it("says Google is the only sign-in, with a mailto, in both modes", async () => {
    state.session = null;
    const sentence =
      "ArdaNova uses Google sign-in for now. If you don&#x27;t have a Google account, write to";

    for (const searchParams of [
      Promise.resolve({}),
      Promise.resolve({ mode: "signup" }),
    ]) {
      const html = renderToStaticMarkup(await SignInPage({ searchParams }));
      expect(html).toContain(sentence);
      expect(html).toContain('href="mailto:contact@escherbridge.com"');
      expect(html).toContain(">contact@escherbridge.com</a>");
    }
  });
});

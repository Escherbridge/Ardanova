// @vitest-environment jsdom
import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { httpLink } from "@trpc/client";
import { fetchRequestHandler } from "@trpc/server/adapters/fetch";
import superjson from "superjson";
import {
  afterAll,
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";

import { feedCardFixture } from "~/lib/feed/card-fixture";
import { api } from "~/trpc/react";

import { FeedCard } from "./feed-card";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-a", role: "INDIVIDUAL" },
  }),
}));

// The real post router and API client run in-process and read `~/env` on import. jsdom defines `window`, so t3-env
// would refuse server variables as client reads: validation is skipped and the values are loopback fixtures.
const serverEnv: Record<string, string> = {
  SKIP_ENV_VALIDATION: "1",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "like-toggle-test-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY: "like-toggle-test-actor-key-is-at-least-32-bytes",
};
for (const [name, value] of Object.entries(serverEnv)) vi.stubEnv(name, value);

const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const { postRouter } = await import("~/server/api/routers/post");
const router = createTRPCRouter({ post: postRouter });

/** The .NET API: the only faked collaborator. Everything from the click to this request is real. */
const dotNetApi =
  vi.fn<(url: string, init: RequestInit) => Promise<Response>>();

/** Renders `ui` under a real tRPC client whose HTTP requests the real post router answers in-process. */
function renderSignedIn(ui: ReactNode) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const client = api.createClient({
    links: [
      httpLink({
        url: "http://localhost:3000/api/trpc",
        transformer: superjson,
        fetch: (input, init) =>
          fetchRequestHandler({
            endpoint: "/api/trpc",
            // jsdom's AbortSignal is not Node's; the in-process request needs no abort.
            req: new Request(input as RequestInfo, {
              ...(init as RequestInit),
              signal: undefined,
            }),
            router,
            createContext: ({ req }) =>
              createTRPCContext({ headers: req.headers }),
          }),
      }),
    ],
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <api.Provider client={client} queryClient={queryClient}>
        {ui}
      </api.Provider>
    </QueryClientProvider>,
  );
}

/** One .NET answer, released by the test after the request has arrived, so the optimistic state can be read first. */
function heldAnswer() {
  let release: (response: Response) => void = () => undefined;
  dotNetApi.mockImplementationOnce(
    () => new Promise<Response>((resolve) => (release = resolve)),
  );
  return (status: number, body: unknown) =>
    release(
      new Response(JSON.stringify(body), {
        status,
        headers: { "Content-Type": "application/json" },
      }),
    );
}

const postDto = (likesCount: number) => ({
  id: "post-1",
  authorId: "user-b",
  content: "Framing the east wall Saturday",
  likesCount,
  commentsCount: 0,
  sharesCount: 7,
});

const footerText = () => document.querySelector("footer")?.textContent ?? "";

beforeEach(() => vi.stubGlobal("fetch", dotNetApi));
afterEach(() => {
  vi.unstubAllGlobals();
  dotNetApi.mockReset();
});
afterAll(() => vi.unstubAllEnvs());

describe("FeedCard like toggle (SR-03-D3)", () => {
  it("A likes B's card: the count rises at once, the API's count wins, and a second tap unlikes", async () => {
    renderSignedIn(
      <FeedCard
        viewerId="user-a"
        card={feedCardFixture({
          counters: { likes: 3, replies: 0, shares: 7 },
        })}
      />,
    );
    const like = screen.getByRole("button", { name: /^Like/ });
    expect(like.textContent).toBe("Like");
    expect(like.getAttribute("aria-pressed")).toBe("false");
    expect(footerText()).toContain("3 likes");
    expect(footerText()).not.toMatch(/share|7/i);

    const answerLike = heldAnswer();
    fireEvent.click(like);

    expect(like.getAttribute("aria-pressed")).toBe("true");
    expect(footerText()).toContain("4 likes");
    await waitFor(() => expect(dotNetApi).toHaveBeenCalledTimes(1));
    const [likeUrl, likeInit] = dotNetApi.mock.calls[0];
    expect([likeInit.method, likeUrl]).toEqual([
      "POST",
      "http://127.0.0.1:5147/api/posts/post-1/like",
    ]);

    // Someone else liked it meanwhile: the API's count replaces the optimistic one.
    answerLike(200, postDto(5));
    await waitFor(() => expect(footerText()).toContain("5 likes"));
    expect(like.getAttribute("aria-pressed")).toBe("true");

    const answerUnlike = heldAnswer();
    fireEvent.click(like);
    expect(like.getAttribute("aria-pressed")).toBe("false");
    expect(footerText()).toContain("4 likes");
    await waitFor(() => expect(dotNetApi).toHaveBeenCalledTimes(2));
    const [unlikeUrl, unlikeInit] = dotNetApi.mock.calls[1];
    expect([unlikeInit.method, unlikeUrl]).toEqual([
      "DELETE",
      "http://127.0.0.1:5147/api/posts/post-1/like",
    ]);
    answerUnlike(200, postDto(4));
    await waitFor(() => expect(footerText()).toContain("4 likes"));
    expect(footerText()).not.toMatch(/share|7/i);
  });

  it("rolls the toggle and the count back when the API refuses the like", async () => {
    renderSignedIn(
      <FeedCard
        viewerId="user-a"
        card={feedCardFixture({
          counters: { likes: 3, replies: 0, shares: 0 },
        })}
      />,
    );
    const like = screen.getByRole("button", { name: /^Like/ });

    const answerLike = heldAnswer();
    fireEvent.click(like);
    expect(footerText()).toContain("4 likes");
    await waitFor(() => expect(dotNetApi).toHaveBeenCalledTimes(1));

    // The post was retracted while the card sat on screen.
    answerLike(404, { error: "Post post-1 not found" });

    await waitFor(() =>
      expect(like.getAttribute("aria-pressed")).toBe("false"),
    );
    expect(footerText()).toContain("3 likes");
  });

  it("offers no toggle on the reader's own card or to a signed-out reader; both still see the count", () => {
    const card = feedCardFixture({
      counters: { likes: 2, replies: 0, shares: 0 },
    });
    const own = renderSignedIn(<FeedCard viewerId="user-b" card={card} />);
    expect(screen.queryByRole("button", { name: /^Like/ })).toBeNull();
    expect(footerText()).toContain("2 likes");
    own.unmount();

    render(<FeedCard viewerId={null} card={card} />);
    expect(screen.queryByRole("button", { name: /^Like/ })).toBeNull();
    expect(footerText()).toContain("2 likes");
  });
});

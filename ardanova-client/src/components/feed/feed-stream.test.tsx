// @vitest-environment jsdom
import {
  act,
  fireEvent,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import type { FeedPage } from "~/lib/contracts/feed-contract";
import { feedCardFixture } from "~/lib/feed/card-fixture";
import type { FeedKitCard } from "~/lib/feed/types";

import { FeedStream } from "./feed-stream";
import { renderWithTrpcEdge, type EdgeCall } from "./trpc-test-edge";

/** The sentinel's observer; `reveal()` scrolls it into view, where it stays (as it does above a short page). */
class FakeIntersectionObserver {
  static latest: FakeIntersectionObserver | null = null;
  readonly root = null;
  readonly rootMargin = "";
  readonly thresholds = [];
  private target: Element | null = null;

  constructor(private readonly callback: IntersectionObserverCallback) {
    FakeIntersectionObserver.latest = this;
  }
  observe(target: Element) {
    this.target = target;
  }
  unobserve() {
    this.target = null;
  }
  disconnect() {
    this.target = null;
  }
  takeRecords() {
    return [];
  }
  reveal() {
    act(() =>
      this.callback(
        [
          {
            isIntersecting: true,
            target: this.target,
          } as IntersectionObserverEntry,
        ],
        this as unknown as IntersectionObserver,
      ),
    );
  }
}

const card = (id: string, sectors: string[] = ["CONSTRUCTION"]): FeedKitCard =>
  feedCardFixture({
    id,
    body: `Card ${id}`,
    project: {
      id: `project-${id}`,
      title: `Project ${id}`,
      slug: `project-${id}`,
      summary: null,
      sectors,
    },
  });

/** Serves pages by cursor ("" is the first); an Error entry fails that request once. */
function feedEdge(pages: Record<string, FeedPage | Error>) {
  return {
    "feed.getPage": (input: unknown) => {
      const cursor = (input as { cursor?: string | null }).cursor ?? "";
      const page = pages[cursor];
      if (page instanceof Error) {
        pages[cursor] = { items: [], nextCursor: null };
        throw page;
      }
      if (!page) throw new Error(`no page for cursor "${cursor}"`);
      return page;
    },
  };
}

const cursorsRequested = (calls: EdgeCall[]) =>
  calls
    .filter((call) => call.path === "feed.getPage")
    .map((call) => (call.input as { cursor?: string | null }).cursor ?? "");

/** Each card's body, read the way assistive tech does: through the article's aria-describedby. */
const renderedBodies = () =>
  screen
    .getAllByRole("article")
    .map(
      (article) =>
        document.getElementById(article.getAttribute("aria-describedby") ?? "")
          ?.textContent,
    );

beforeEach(() => {
  FakeIntersectionObserver.latest = null;
  vi.stubGlobal("IntersectionObserver", FakeIntersectionObserver);
});
afterEach(() => vi.unstubAllGlobals());

describe("FeedStream", () => {
  it("keeps paging through short and empty pages while nextCursor is set, then ends in the cold-start row", async () => {
    const { calls } = renderWithTrpcEdge(
      <FeedStream scope="home" />,
      feedEdge({
        "": { items: [card("a"), card("b", ["HOUSING"])], nextCursor: "c1" },
        // Short page that repeats b: the caps held; it is not the end.
        c1: { items: [card("b", ["HOUSING"]), card("c")], nextCursor: "c2" },
        c2: { items: [], nextCursor: "c3" },
        c3: { items: [card("d")], nextCursor: null },
      }),
    );

    await screen.findByText("Card a");
    FakeIntersectionObserver.latest!.reveal();

    await screen.findByText("Card d");
    expect(renderedBodies()).toEqual(["Card a", "Card b", "Card c", "Card d"]);
    expect(cursorsRequested(calls)).toEqual(["", "c1", "c2", "c3"]);

    const sectors = screen.getByRole("navigation", { name: "Sectors" });
    expect(
      within(sectors)
        .getAllByRole("link")
        .map((link) => [link.textContent, link.getAttribute("href")]),
    ).toEqual([
      ["Construction", "/tag/construction"],
      ["Housing", "/tag/housing"],
    ]);
    expect(
      screen
        .getByRole("link", { name: "Start a project" })
        .getAttribute("href"),
    ).toBe("/projects/create");
    expect(screen.queryByRole("button", { name: "Load more" })).toBeNull();
  });

  it("pages by the visible Load more button where IntersectionObserver is missing", async () => {
    vi.stubGlobal("IntersectionObserver", undefined);
    const { calls } = renderWithTrpcEdge(
      <FeedStream scope="home" />,
      feedEdge({
        "": { items: [card("a")], nextCursor: "c1" },
        c1: { items: [card("b")], nextCursor: null },
      }),
    );

    fireEvent.click(await screen.findByRole("button", { name: "Load more" }));

    await screen.findByText("Card b");
    expect(cursorsRequested(calls)).toEqual(["", "c1"]);
  });

  it("stops at a failed page instead of re-requesting it, and Retry resumes", async () => {
    const { calls } = renderWithTrpcEdge(
      <FeedStream scope="home" />,
      feedEdge({
        "": { items: [card("a")], nextCursor: "c1" },
        c1: new Error("upstream unavailable"),
      }),
      // Real latency, so the in-flight render commits and a missing guard would re-request on failure.
      { latencyMs: 5 },
    );

    await screen.findByText("Card a");
    FakeIntersectionObserver.latest!.reveal();

    await screen.findByText("Feed didn't load.");
    // The sentinel is still in view; a hot loop would have asked for c1 again by now.
    await new Promise((resolve) => setTimeout(resolve, 60));
    expect(cursorsRequested(calls)).toEqual(["", "c1"]);

    fireEvent.click(screen.getByRole("button", { name: "Retry" }));
    await waitFor(() =>
      expect(screen.queryByText("Feed didn't load.")).toBeNull(),
    );
    expect(cursorsRequested(calls)).toEqual(["", "c1", "c1"]);
    screen.getByRole("navigation", { name: "Sectors" });
  });

  it("keeps a just-published ask first, once, but shows the stream's copy: picked up since it was posted", async () => {
    const ask = (task: FeedKitCard["task"]) =>
      feedCardFixture({
        id: "post-ask",
        postType: "HELP_WANTED",
        body: "Need two hands Saturday",
        task,
      });
    renderWithTrpcEdge(
      <FeedStream
        scope="project:project-p"
        leadingCards={[ask({ id: "task-1", state: "OPEN" })]}
      />,
      feedEdge({
        "": {
          items: [
            card("a"),
            ask({ id: "task-1", state: "PICKED_UP", assigneeName: "Cy" }),
          ],
          nextCursor: null,
        },
      }),
    );

    await screen.findByText("Card a");
    expect(renderedBodies()).toEqual(["Need two hands Saturday", "Card a"]);
    const [first] = screen.getAllByRole("article");
    within(first).getByText("Picked up by Cy");
  });

  it("on an empty platform shows one line and Start a project", async () => {
    renderWithTrpcEdge(
      <FeedStream scope="home" />,
      feedEdge({ "": { items: [], nextCursor: null } }),
    );

    await screen.findByText("Nothing posted yet.");
    expect(
      screen
        .getByRole("link", { name: "Start a project" })
        .getAttribute("href"),
    ).toBe("/projects/create");
    expect(screen.queryByRole("navigation", { name: "Sectors" })).toBeNull();
    expect(screen.queryByRole("feed")).toBeNull();
  });
});

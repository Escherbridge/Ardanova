// @vitest-environment jsdom
import { fireEvent, render, screen, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { feedCardFixture } from "~/lib/feed/card-fixture";
import { KNOWN_FEED_POST_TYPES } from "~/lib/feed/registry";
import type { FeedKitCard } from "~/lib/feed/types";

import { FeedCard } from "./feed-card";
import { renderWithTrpcEdge } from "./trpc-test-edge";

/** Every fact any card kind can carry, so each type renders its full anatomy. */
const fullFacts: Partial<FeedKitCard> = {
  opportunity: { id: "opp-1", title: "Lead carpenter", slug: "lead-carpenter" },
  event: {
    id: "evt-1",
    title: "Raising day",
    startsAt: "2026-11-07T15:00:00Z",
  },
  funding: { percent: 50 },
  thread: {
    source: "PROJECT_COMMENT",
    commentId: "c-1",
    targetType: "PROJECT",
    targetId: "project-p",
    opportunityId: null,
  },
  task: { id: "task-t", state: "OPEN", assigneeName: null, paid: false },
  viewer: { isMember: true, isFollowing: false, canClaim: true },
};

/** The one primary action, after proving there is exactly one and its accessible name holds its visible label (WCAG 2.5.3). */
function onlyPrimaryAction(container: HTMLElement): HTMLElement {
  const actions = container.querySelectorAll<HTMLElement>(
    "[data-primary-action]",
  );
  expect(actions).toHaveLength(1);
  const action = actions[0];
  const visible = action.textContent ?? "";
  expect(visible.trim()).not.toBe("");
  screen.getByRole(action.tagName === "A" ? "link" : "button", {
    name: (accessibleName, element) =>
      element === action && accessibleName.includes(visible),
  });
  return action;
}

const askBody = "Framing the east wall Saturday, need two hands #carpentry";
const ask = (facts: Partial<FeedKitCard>) =>
  feedCardFixture({
    postType: "HELP_WANTED",
    body: askBody,
    hashtags: ["carpentry", "framing"],
    task: { id: "task-t", state: "OPEN", assigneeName: null, paid: false },
    ...facts,
  });

describe("FeedCard", () => {
  it.each([...KNOWN_FEED_POST_TYPES, "BARN_DANCE_ANNOUNCED"])(
    "%s renders exactly one primary action, named by its visible label",
    (postType) => {
      // Signed in, so the quiet like toggle renders too: it must not count as a second primary action.
      const { container, unmount } = renderWithTrpcEdge(
        <FeedCard
          card={feedCardFixture({ postType, ...fullFacts })}
          viewerId="user-a"
        />,
        {},
      );
      onlyPrimaryAction(container);
      unmount();
    },
  );

  it("renders a PostType it does not know as a plain post", () => {
    const { container } = render(
      <FeedCard
        card={feedCardFixture({
          postType: "BARN_DANCE_ANNOUNCED",
          ...fullFacts,
        })}
      />,
    );
    const action = onlyPrimaryAction(container);
    expect(action.tagName).toBe("BUTTON");
    expect(action.textContent).toBe("Reply");
    expect(container.querySelector("article")?.textContent).toContain(
      "Framing the east wall Saturday",
    );
    // Funding facts are present, but an unknown type gets the post anatomy: no funding mark.
    expect(screen.queryByRole("progressbar")).toBeNull();
  });

  it("B's help ask: A, on the team, can pick it up; C, a follower, replies or messages", () => {
    const asMember = renderWithTrpcEdge(
      <FeedCard
        viewerId="user-a"
        card={ask({
          viewer: { isMember: true, isFollowing: false, canClaim: true },
          reasons: [
            {
              kind: "ProjectTeam",
              text: "",
              about: "PROJECT",
              aboutId: "project-p",
            },
          ],
        })}
      />,
      {},
    );
    const pickUp = screen.getByRole("link", { name: "Pick up task" });
    expect(pickUp.getAttribute("href")).toBe("/tasks/task-t");
    screen.getByText("Open");
    screen.getByText("You're on Barn raising");
    expect(
      screen
        .getByRole("link", { name: "Message Bea Builder" })
        .getAttribute("href"),
    ).toBe("/chats?with=user-b");
    // #carpentry is linked in B's words, so only #framing gets a chip: one link per tag.
    expect(
      screen
        .getAllByRole("link")
        .map((link) => link.getAttribute("href"))
        .filter((href) => href?.startsWith("/tag/")),
    ).toEqual(["/tag/carpentry", "/tag/framing"]);
    asMember.unmount();

    renderWithTrpcEdge(
      <FeedCard
        viewerId="user-c"
        card={ask({
          viewer: { isMember: false, isFollowing: true, canClaim: false },
          reasons: [
            {
              kind: "FollowedProject",
              text: "",
              about: "PROJECT",
              aboutId: "project-p",
            },
          ],
        })}
      />,
      {},
    );
    expect(screen.queryByRole("link", { name: "Pick up task" })).toBeNull();
    expect(
      screen
        .getByRole("button", { name: "Reply" })
        .getAttribute("aria-expanded"),
    ).toBe("false");
    screen.getByText("Following Barn raising");
    expect(
      screen
        .getByRole("link", { name: "Message Bea Builder" })
        .getAttribute("href"),
    ).toBe("/chats?with=user-b");
  });

  it("offers Follow on the subject chip to a non-member, and not to the team", () => {
    const onToggleFollow = vi.fn();
    const outsider = render(
      <FeedCard card={feedCardFixture()} onToggleFollow={onToggleFollow} />,
    );
    fireEvent.click(
      screen.getByRole("button", { name: "Follow Barn raising" }),
    );
    expect(onToggleFollow).toHaveBeenCalledWith(
      expect.objectContaining({
        key: "project:project-p",
        name: "Barn raising",
      }),
      true,
    );
    outsider.unmount();

    render(
      <FeedCard
        card={feedCardFixture({
          viewer: { isMember: true, isFollowing: false, canClaim: false },
        })}
        onToggleFollow={onToggleFollow}
      />,
    );
    expect(screen.queryByRole("button", { name: /Follow/ })).toBeNull();
    expect(
      screen.getByRole("link", { name: "Barn raising" }).getAttribute("href"),
    ).toBe("/projects/barn-raising");
  });

  it("C replies inline: the reply posts, shows as the one preview, and the count rises", async () => {
    let replies: object[] = [];
    const { calls, container } = renderWithTrpcEdge(
      <FeedCard
        viewerId="user-c"
        card={ask({
          viewer: { isMember: false, isFollowing: true, canClaim: false },
        })}
      />,
      {
        "post.getComments": () => replies,
        "post.addComment": (input) => {
          const { postId, content } = input as {
            postId: string;
            content: string;
          };
          const reply = {
            id: "reply-1",
            postId,
            authorId: "user-c",
            content,
            likesCount: 0,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          };
          replies = [reply];
          return reply;
        },
      },
    );

    fireEvent.click(screen.getByRole("button", { name: "Reply" }));
    const field = await screen.findByRole("textbox", { name: "Reply" });
    expect(field.getAttribute("placeholder")).toBe("How can you help?");
    fireEvent.change(field, {
      target: { value: "I can bring a level and two hands" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Send reply" }));

    // The textarea mirrors its value into defaultValue, so look for the reply where the preview renders it.
    const preview = await screen.findByText(
      "I can bring a level and two hands",
      {
        selector: "blockquote",
      },
    );
    within(preview.closest("figure")!).getByText(/^You ·/);
    expect(
      calls.find((call) => call.path === "post.addComment")?.input,
    ).toEqual({
      postId: "post-1",
      content: "I can bring a level and two hands",
    });
    expect(container.querySelector("footer")?.textContent).toContain("1 reply");
    expect((field as HTMLTextAreaElement).value).toBe("");
  });
});

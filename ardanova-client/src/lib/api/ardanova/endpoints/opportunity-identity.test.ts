import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

import type { BaseApiClient } from "../../base-client";
import { OpportunitiesEndpoint } from "./opportunities";

function fakeClient() {
  const post = vi.fn().mockResolvedValue({ status: 200 });

  return {
    client: { post } as unknown as BaseApiClient,
    post,
  };
}

describe("opportunity endpoint identity boundaries", () => {
  it("keeps poster, applicant, update author, and comment author out of request bodies", async () => {
    const { client, post } = fakeClient();
    const opportunities = new OpportunitiesEndpoint(client);

    await opportunities.create({
      title: "Draft researcher role",
      description: "Help organize the project research backlog.",
      type: "TASK_BOUNTY",
      projectId: "project-1",
    });
    await opportunities.apply("opportunity-1", {
      coverLetter: "I can help organize the project research backlog.",
    });
    await opportunities.createUpdate("opportunity-1", {
      title: "Research plan",
      content: "The first set of interviews is scheduled for next week.",
    });
    await opportunities.addComment("opportunity-1", {
      content: "I can review the interview guide before then.",
    });

    expect(post.mock.calls.map(([path]) => String(path))).toEqual([
      "/api/opportunities",
      "/api/opportunities/opportunity-1/apply",
      "/api/opportunities/opportunity-1/updates",
      "/api/opportunities/opportunity-1/comments",
    ]);

    for (const [, body] of post.mock.calls) {
      expect(JSON.stringify(body)).not.toMatch(
        /posterId|applicantId|userId|opportunityId/,
      );
    }
  });
});

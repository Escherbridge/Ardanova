import { describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
import { BaseApiClient, type ApiResponse } from "../../base-client";
import { OpportunityBidsEndpoint } from "./opportunity-bids";
import { PayoutsEndpoint } from "./payouts";
import { TaskEscrowsEndpoint } from "./task-escrows";
import { TasksEndpoint } from "./tasks";
import { TokenBalancesEndpoint } from "./token-balances";
import { WalletsEndpoint } from "./wallets";
import { FundingIntentsEndpoint } from "./funding-intents";
import { ProjectTokensEndpoint } from "./project-tokens";

class CapturingClient extends BaseApiClient {
  readonly calls: Array<{
    method: string;
    endpoint: string;
    body?: unknown;
    headers?: Record<string, string>;
  }> = [];

  constructor() {
    super({ baseUrl: "https://api.example.test" });
  }

  override get<T>(endpoint: string): Promise<ApiResponse<T>> {
    this.calls.push({ method: "GET", endpoint });
    return Promise.resolve({ status: 200 });
  }

  override post<T>(
    endpoint: string,
    body?: unknown,
    headers?: Record<string, string>,
  ): Promise<ApiResponse<T>> {
    this.calls.push({
      method: "POST",
      endpoint,
      body,
      ...(headers ? { headers } : {}),
    });
    return Promise.resolve({ status: 200 });
  }
}

describe("commerce self-service API contracts", () => {
  it("uses actor-derived /me routes and identity-free request bodies", async () => {
    const client = new CapturingClient();

    await new WalletsEndpoint(client).getMine();
    await new WalletsEndpoint(client).create({ address: "ALGO1" });
    await new PayoutsEndpoint(client).requestPayout({
      sourceProjectTokenConfigId: "config-1",
      sourceTokenAmount: 10,
      holderClass: "CONTRIBUTOR",
    });
    await new TaskEscrowsEndpoint(client).getMine();
    await new TaskEscrowsEndpoint(client).create({
      taskId: "task-1",
      shareId: "share-1",
      amount: 10,
    });
    await new TaskEscrowsEndpoint(client).dispute("escrow-1", {
      reason: "QUALITY_ISSUE",
      description:
        "The delivered work does not meet the documented acceptance criteria.",
    });
    await new TasksEndpoint(client).getMine();
    await new TasksEndpoint(client).getCommerce("task-1");
    await new OpportunityBidsEndpoint(client).getMine();
    await new OpportunityBidsEndpoint(client).getByOpportunityId(
      "opportunity-1",
    );
    await new OpportunityBidsEndpoint(client).accept("bid-1");
    await new TokenBalancesEndpoint(client).getBalance(
      "config-1",
      "CONTRIBUTOR",
    );
    await new TokenBalancesEndpoint(client).getArdaBalance();
    await new TokenBalancesEndpoint(client).getPortfolio();
    await new TokenBalancesEndpoint(client).checkLiquidity(
      "config-1",
      "CONTRIBUTOR",
    );
    await new FundingIntentsEndpoint(client).createCheckout(
      {
        projectTokenConfigId: "config-1",
        amount: "12.34",
        disclosureVersion: "funding-disclosure-v1",
      },
      "9e68f25a-589c-472a-ab2d-0b4161c5ab89",
    );
    await new FundingIntentsEndpoint(client).getStatus("funding-1");
    await new ProjectTokensEndpoint(client).getMetadata([
      "config-1",
      "config-2",
    ]);

    expect(client.calls).toEqual([
      { method: "GET", endpoint: "/api/wallets/me" },
      { method: "POST", endpoint: "/api/wallets", body: { address: "ALGO1" } },
      {
        method: "POST",
        endpoint: "/api/Payouts",
        body: {
          sourceProjectTokenConfigId: "config-1",
          sourceTokenAmount: 10,
          holderClass: "CONTRIBUTOR",
        },
      },
      { method: "GET", endpoint: "/api/taskescrows/me" },
      {
        method: "POST",
        endpoint: "/api/taskescrows",
        body: { taskId: "task-1", shareId: "share-1", amount: 10 },
      },
      {
        method: "POST",
        endpoint: "/api/taskescrows/escrow-1/dispute",
        body: {
          reason: "QUALITY_ISSUE",
          description:
            "The delivered work does not meet the documented acceptance criteria.",
        },
      },
      { method: "GET", endpoint: "/api/tasks/me" },
      { method: "GET", endpoint: "/api/task-commerce/task-1" },
      { method: "GET", endpoint: "/api/opportunity-bids/me" },
      { method: "GET", endpoint: "/api/opportunities/opportunity-1/bids" },
      {
        method: "POST",
        endpoint: "/api/opportunity-bids/bid-1/accept",
        body: {},
      },
      {
        method: "GET",
        endpoint:
          "/api/TokenBalance/me/balance?projectTokenConfigId=config-1&holderClass=CONTRIBUTOR",
      },
      { method: "GET", endpoint: "/api/TokenBalance/me/arda" },
      { method: "GET", endpoint: "/api/TokenBalance/me/portfolio" },
      {
        method: "GET",
        endpoint:
          "/api/TokenBalance/me/liquidity?projectTokenConfigId=config-1&holderClass=CONTRIBUTOR",
      },
      {
        method: "POST",
        endpoint: "/api/funding-intents/checkout",
        body: {
          projectTokenConfigId: "config-1",
          amount: "12.34",
          disclosureVersion: "funding-disclosure-v1",
        },
        headers: {
          "X-Idempotency-Key": "9e68f25a-589c-472a-ab2d-0b4161c5ab89",
        },
      },
      { method: "GET", endpoint: "/api/funding-intents/funding-1" },
      {
        method: "POST",
        endpoint: "/api/ProjectTokens/config/metadata/batch",
        body: { ids: ["config-1", "config-2"] },
      },
    ]);
    expect(
      client.calls.some((call) => call.endpoint.includes("/tasks/user/")),
    ).toBe(false);
  });
});

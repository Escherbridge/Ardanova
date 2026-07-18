import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));

const signalRMocks = vi.hoisted(() => {
  const connection = {
    state: "Disconnected",
    on: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
  };
  const builder = {
    withUrl: vi.fn(),
    withAutomaticReconnect: vi.fn(),
    configureLogging: vi.fn(),
    build: vi.fn(() => connection),
  };
  builder.withUrl.mockReturnValue(builder);
  builder.withAutomaticReconnect.mockReturnValue(builder);
  builder.configureLogging.mockReturnValue(builder);
  return { builder, connection };
});

const { createActorAssertion } = vi.hoisted(() => ({
  createActorAssertion: vi
    .fn()
    .mockReturnValueOnce("signed-assertion-1")
    .mockReturnValueOnce("signed-assertion-2"),
}));

vi.mock("@microsoft/signalr", () => ({
  HubConnectionBuilder: function HubConnectionBuilder() {
    return signalRMocks.builder;
  },
  HubConnectionState: { Connected: "Connected" },
  LogLevel: { Warning: "Warning" },
}));

vi.mock("~/env", () => ({
  env: {
    API_URL: "https://api.example.test",
    API_KEY: "service-key-012345678901234567890123456789",
  },
}));

vi.mock("~/server/actor-assertion", () => ({ createActorAssertion }));

import { SignalRBackendClient } from "./signalr-backend-client";

describe("SignalR backend actor authentication", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("uses a fresh signed actor assertion factory and never sends a raw actor id", async () => {
    const client = new SignalRBackendClient("user-123");

    await client.connect();

    const options = signalRMocks.builder.withUrl.mock.calls.at(0)?.[1] as {
      headers: Record<string, string>;
      accessTokenFactory: () => string;
    };
    expect(options.headers).toEqual({
      "X-Api-Key": "service-key-012345678901234567890123456789",
    });
    expect(options.headers).not.toHaveProperty("X-Ardanova-Actor-Id");
    expect(options.accessTokenFactory()).toBe("signed-assertion-1");
    expect(options.accessTokenFactory()).toBe("signed-assertion-2");
    expect(createActorAssertion).toHaveBeenNthCalledWith(
      1,
      { subject: "user-123" },
      {
        method: "GET",
        url: "https://api.example.test/hubs/ardanova",
      },
    );
  });
});

// @vitest-environment jsdom
import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { afterAll, afterEach, describe, expect, it, vi } from "vitest";

import {
  IN_PROCESS_SERVER_ENV,
  fakeDotNetApi,
  renderOverRouter,
} from "~/components/notifications/dotnet-test-edge";

import { AppTopbar } from "./app-topbar";

vi.mock("server-only", () => ({}));
vi.mock("~/server/auth", () => ({
  auth: async () => ({
    expires: "2099-01-01T00:00:00Z",
    user: { id: "user-bea", role: "INDIVIDUAL" },
  }),
}));
vi.mock("next/navigation", () => ({ usePathname: () => "/dashboard" }));
vi.mock("next-auth/react", () => ({ signOut: vi.fn() }));
vi.mock("~/components/ai/nova-assistant", () => ({
  NovaAssistant: () => null,
}));

for (const [name, value] of Object.entries(IN_PROCESS_SERVER_ENV)) {
  vi.stubEnv(name, value);
}
const { createTRPCContext, createTRPCRouter } = await import(
  "~/server/api/trpc"
);
const { notificationRouter } = await import(
  "~/server/api/routers/notification"
);
const router = createTRPCRouter({ notification: notificationRouter });

/** A .NET NotificationDto as the API serialises it (camelCase, string enum, UTC 'Z'). */
const notificationDto = (
  fields: Pick<
    Record<string, string>,
    "id" | "type" | "title" | "message" | "actionUrl"
  >,
) => ({
  userId: "user-bea",
  data: null,
  isRead: false,
  readAt: null,
  createdAt: "2026-11-01T09:00:00Z",
  ...fields,
});

const reply = notificationDto({
  id: "n-reply",
  type: "COMMENT_REPLY",
  title: "New reply",
  message: "Cy Carpenter: “Count me in for Saturday.”",
  actionUrl: "/projects/barn-raising",
});
const pickedUp = notificationDto({
  id: "n-picked-up",
  type: "TASK_ASSIGNED",
  title: "Picked up",
  message: "Cy Carpenter took Frame the east wall",
  actionUrl: "/tasks/task-t",
});
// A type this bell does not own (plan §4: exactly two return loops).
const projectUpdate = notificationDto({
  id: "n-update",
  type: "PROJECT_UPDATE",
  title: "Project updated",
  message: "Barn raising posted an update",
  actionUrl: "/projects/barn-raising",
});

afterEach(() => vi.unstubAllGlobals());
afterAll(() => vi.unstubAllEnvs());

describe("AppTopbar mobile drawer", () => {
  it("opens with focus on Home, the first primary navigation link", async () => {
    const dotNet = fakeDotNetApi({
      "GET /api/notifications/me/unread": () => ({ body: [] }),
    });
    vi.stubGlobal("fetch", dotNet.fetch);
    renderOverRouter(
      <AppTopbar user={{ id: "user-bea", name: "Bea Builder" }} />,
      router,
      createTRPCContext,
    );

    fireEvent.click(
      screen.getByRole("button", { name: "Open workspace navigation" }),
    );

    const navigation = await screen.findByRole("navigation", {
      name: "Primary workspace navigation",
    });
    const firstLink = within(navigation).getAllByRole("link")[0];
    expect(firstLink?.textContent).toBe("Home");
    await waitFor(() => expect(document.activeElement).toBe(firstLink));
  });
});

describe("AppTopbar notification bell (plan §4)", () => {
  it("counts a reply and a picked-up ask, deep-links the reply and marks it read on the API", async () => {
    let unread = [reply, pickedUp, projectUpdate];
    const dotNet = fakeDotNetApi({
      "GET /api/notifications/me/unread": () => ({ body: unread }),
      "POST /api/notifications/n-reply/read": () => {
        unread = unread.filter((notification) => notification.id !== reply.id);
        return {
          body: { ...reply, isRead: true, readAt: "2026-11-01T09:05:00Z" },
        };
      },
    });
    vi.stubGlobal("fetch", dotNet.fetch);
    const { navigations } = renderOverRouter(
      <AppTopbar user={{ id: "user-bea", name: "Bea Builder" }} />,
      router,
      createTRPCContext,
    );

    fireEvent.click(
      await screen.findByRole("button", { name: "Notifications, 2 unread" }),
    );
    const panel = screen.getByRole("region", { name: "Notifications" });
    const entries = within(panel).getAllByRole("link");
    expect(entries.map((entry) => entry.getAttribute("href"))).toEqual([
      "/projects/barn-raising",
      "/tasks/task-t",
    ]);
    expect(entries[0]?.textContent).toContain("New reply");
    expect(entries[0]?.textContent).toContain("Count me in for Saturday.");
    expect(entries[1]?.textContent).toContain(
      "Cy Carpenter took Frame the east wall",
    );
    expect(panel.textContent).not.toContain("Project updated");

    fireEvent.click(entries[0]);

    expect(navigations).toEqual(["/projects/barn-raising"]);
    await waitFor(() =>
      expect(
        dotNet.calls
          .filter((call) => call.method === "POST")
          .map((call) => call.path),
      ).toEqual(["/api/notifications/n-reply/read"]),
    );
    // After the write the bell re-reads the API; the count it shows is the server's, not its own guess.
    await waitFor(() =>
      expect(
        dotNet.calls.filter(
          (call) => call.path === "/api/notifications/me/unread",
        ),
      ).toHaveLength(2),
    );
    await screen.findByRole("button", { name: "Notifications, 1 unread" });
  });

  it("Mark all read marks the two shown rows read and leaves a notice no screen lists unread", async () => {
    // An escrow notice: unread, and shown nowhere in the bell.
    const escrow = notificationDto({
      id: "n-escrow",
      type: "ESCROW_FUNDED",
      title: "Escrow funded",
      message: "Escrow for Frame the east wall is funded",
      actionUrl: "/tasks/task-t",
    });
    const stored = [reply, pickedUp, escrow];
    let unread = stored;
    // NotificationsController.MarkAsRead answers the row it marked.
    const markRead = (id: string) => () => {
      unread = unread.filter((notification) => notification.id !== id);
      const row = stored.find((notification) => notification.id === id)!;
      return {
        body: { ...row, isRead: true, readAt: "2026-11-01T09:05:00Z" },
      };
    };
    const dotNet = fakeDotNetApi({
      "GET /api/notifications/me/unread": () => ({ body: unread }),
      "POST /api/notifications/n-reply/read": markRead("n-reply"),
      "POST /api/notifications/n-picked-up/read": markRead("n-picked-up"),
    });
    vi.stubGlobal("fetch", dotNet.fetch);
    renderOverRouter(
      <AppTopbar user={{ id: "user-bea", name: "Bea Builder" }} />,
      router,
      createTRPCContext,
    );

    fireEvent.click(
      await screen.findByRole("button", { name: "Notifications, 2 unread" }),
    );
    fireEvent.click(screen.getByRole("button", { name: "Mark all read" }));

    await screen.findByText("Nothing new.");
    await waitFor(() =>
      expect(
        dotNet.calls
          .filter((call) => call.method === "POST")
          .map((call) => call.path)
          .sort(),
      ).toEqual([
        "/api/notifications/n-picked-up/read",
        "/api/notifications/n-reply/read",
      ]),
    );
    // Never the read-all route, so the escrow notice is still unread on the API.
    expect(unread.map((notification) => notification.id)).toEqual(["n-escrow"]);
  });
});

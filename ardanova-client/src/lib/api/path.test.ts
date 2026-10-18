import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("server-only", () => ({}));
vi.mock("~/env", () => ({ env: {} }));

import { ArdaNovaApiClient } from "./ardanova";
import { InvalidApiPathError } from "./path";

const BASE = "http://api.test";
let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  fetchMock = vi.fn().mockResolvedValue(new Response("{}", { status: 200 }));
  vi.stubGlobal("fetch", fetchMock);
});
afterEach(() => vi.unstubAllGlobals());

const api = () => new ArdaNovaApiClient({ baseUrl: BASE });
const requestedUrl = () => String(fetchMock.mock.calls[0]?.[0]);

type Call = (client: ArdaNovaApiClient, id: string) => Promise<unknown>;
const endpoints: Array<[string, Call, (encoded: string) => string]> = [
  ["users.getById", (c, id) => c.users.getById(id), (e) => `/api/users/${e}`],
  ["users.getSkills", (c, id) => c.users.getSkills(id), (e) => `/api/users/${e}/skills`],
  ["guilds.getById", (c, id) => c.guilds.getById(id), (e) => `/api/guilds/${e}`],
  ["projects.getById", (c, id) => c.projects.getById(id), (e) => `/api/projects/${e}`],
  ["opportunities.getById", (c, id) => c.opportunities.getById(id), (e) => `/api/opportunities/${e}`],
  ["xpEvents.getHistory", (c, id) => c.xpEvents.getHistory(id, undefined, 5), (e) => `/api/xpevents/user/${e}/history?limit=5`],
];

const hostileIds = [
  "paged?page=1&pageSize=3",
  "../users/paged?x=",
  "a/b",
  "x#frag",
  "%2e%2e",
];

describe("API client path safety", () => {
  describe.each(endpoints)("%s", (_name, call, expected) => {
    it.each(hostileIds)("keeps %j inside one path segment", async (id) => {
      await call(api(), id);
      const url = new URL(requestedUrl());
      expect(url.pathname + url.search).toBe(expected(encodeURIComponent(id)));
    });

    it.each(["", ".", ".."])("refuses %j before any request is sent", async (id) => {
      await expect((async () => call(api(), id))()).rejects.toBeInstanceOf(InvalidApiPathError);
      expect(fetchMock).not.toHaveBeenCalled();
    });
  });

  it("encodes query values so they cannot add parameters", async () => {
    await api().users.search("a&role=ADMIN", 1, 10);
    const url = new URL(requestedUrl());
    expect([...url.searchParams.keys()]).toEqual(["query", "page", "pageSize"]);
    expect(url.searchParams.get("query")).toBe("a&role=ADMIN");
  });

  it("builds optional query strings with URLSearchParams and drops absent values", async () => {
    await api().projects.search({ searchTerm: "x&status=DRAFT", page: 2 });
    const url = new URL(requestedUrl());
    expect(url.pathname).toBe("/api/projects/search");
    expect(Object.fromEntries(url.searchParams)).toEqual({
      searchTerm: "x&status=DRAFT",
      page: "2",
      pageSize: "10",
    });
  });
});

describe("endpoint files interpolate only through apiPath", () => {
  const dir = join(__dirname, "ardanova", "endpoints");
  const files = readdirSync(dir).filter((f) => f.endsWith(".ts") && !f.endsWith(".test.ts"));

  it.each(files)("%s has no raw ${} inside an /api/ template", (file) => {
    const source = readFileSync(join(dir, file), "utf8");
    const raw = [...source.matchAll(/(\w*)`(\/api\/[^`]*)`/g)]
      .filter(([, tag, body]) => tag !== "apiPath" && body!.includes("${"))
      .map(([match]) => match);
    expect(raw).toEqual([]);
    expect(source).not.toMatch(/encodeURIComponent\(/);
  });
});

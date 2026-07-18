import { describe, expect, it } from "vitest";

import { assertLocalDemoSeedAllowed } from "./local-demo-seed-guard";

const safeEnvironment = {
  ALLOW_LOCAL_DEMO_SEED: "true",
  DATABASE_URL:
    "postgresql://postgres:secret@localhost:5432/ardanova_local_demo",
  NODE_ENV: "development",
};

describe("local demo seed guard", () => {
  it("requires a per-run explicit opt-in", () => {
    expect(() =>
      assertLocalDemoSeedAllowed({
        ...safeEnvironment,
        ALLOW_LOCAL_DEMO_SEED: undefined,
      }),
    ).toThrow("ALLOW_LOCAL_DEMO_SEED=true");
  });

  it("refuses production even when explicitly enabled", () => {
    expect(() =>
      assertLocalDemoSeedAllowed({
        ...safeEnvironment,
        NODE_ENV: "production",
      }),
    ).toThrow("NODE_ENV=production");
  });

  it("refuses a hosted database", () => {
    expect(() =>
      assertLocalDemoSeedAllowed({
        ...safeEnvironment,
        DATABASE_URL:
          "postgresql://postgres:secret@database.example.test:5432/ardanova_local_demo",
      }),
    ).toThrow("non-loopback database host");
  });

  it("refuses a loopback database without the dedicated demo name", () => {
    expect(() =>
      assertLocalDemoSeedAllowed({
        ...safeEnvironment,
        DATABASE_URL:
          "postgresql://postgres:secret@localhost:5432/ardanova_production",
      }),
    ).toThrow("dedicated database");
  });

  it.each([
    "postgresql://postgres:secret@localhost:5432/ardanova_local_demo",
    "postgres://postgres:secret@127.0.0.1:5432/ardanova_local_demo_alex",
    "postgresql://postgres:secret@[::1]:5432/ardanova_local_demo_qa-1",
  ])("accepts an opted-in dedicated loopback target: %s", (databaseUrl) => {
    const target = assertLocalDemoSeedAllowed({
      ...safeEnvironment,
      DATABASE_URL: databaseUrl,
    });
    expect(target.databaseName).toMatch(/^ardanova_local_demo/);
  });
});

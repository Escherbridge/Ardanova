const LOOPBACK_DATABASE_HOSTS = new Set([
  "localhost",
  "127.0.0.1",
  "::1",
  "[::1]",
]);
const POSTGRES_PROTOCOLS = new Set(["postgres:", "postgresql:"]);
const DEDICATED_DATABASE_NAME =
  /^ardanova_local_demo(?:_[a-z0-9][a-z0-9_-]*)?$/i;

export const LOCAL_DEMO_SEED_OPT_IN = "ALLOW_LOCAL_DEMO_SEED";

export type LocalDemoSeedEnvironment = {
  DATABASE_URL?: string;
  ALLOW_LOCAL_DEMO_SEED?: string;
  NODE_ENV?: string;
};

export type LocalDemoSeedTarget = {
  databaseHost: string;
  databaseName: string;
};

export function assertLocalDemoSeedAllowed(
  environment: LocalDemoSeedEnvironment,
): LocalDemoSeedTarget {
  if (environment.ALLOW_LOCAL_DEMO_SEED?.trim().toLowerCase() !== "true") {
    throw new Error(
      `${LOCAL_DEMO_SEED_OPT_IN}=true is required in the calling shell for this seed run.`,
    );
  }

  if (environment.NODE_ENV?.trim().toLowerCase() === "production") {
    throw new Error(
      "Local demo seeding is unavailable when NODE_ENV=production.",
    );
  }

  if (!environment.DATABASE_URL) {
    throw new Error("DATABASE_URL is required for local demo seeding.");
  }

  let databaseUrl: URL;
  try {
    databaseUrl = new URL(environment.DATABASE_URL);
  } catch {
    throw new Error("DATABASE_URL must be a valid PostgreSQL URL.");
  }

  if (!POSTGRES_PROTOCOLS.has(databaseUrl.protocol)) {
    throw new Error("Local demo seeding requires a PostgreSQL DATABASE_URL.");
  }

  const databaseHost = databaseUrl.hostname.toLowerCase();
  if (!LOOPBACK_DATABASE_HOSTS.has(databaseHost)) {
    throw new Error(
      `Local demo seeding refused for non-loopback database host: ${databaseHost}`,
    );
  }

  const pathSegments = databaseUrl.pathname.split("/").filter(Boolean);
  let databaseName = "";
  try {
    databaseName =
      pathSegments.length === 1
        ? decodeURIComponent(pathSegments[0] ?? "")
        : "";
  } catch {
    throw new Error("DATABASE_URL contains an invalid encoded database name.");
  }

  if (!DEDICATED_DATABASE_NAME.test(databaseName)) {
    throw new Error(
      'Local demo seeding requires a dedicated database named "ardanova_local_demo" or "ardanova_local_demo_<suffix>".',
    );
  }

  return { databaseHost, databaseName };
}

import { spawnSync } from "node:child_process";

const npm = process.platform === "win32" ? "npm.cmd" : "npm";
const npmCli = process.env.npm_execpath;
const releaseCheckEnv = {
  AUTH_SECRET: "release-check-auth-secret-is-not-for-runtime-use",
  AUTH_URL: "http://127.0.0.1:3000",
  GOOGLE_CLIENT_ID: "release-check-google-client",
  GOOGLE_CLIENT_SECRET: "release-check-google-secret",
  DATABASE_URL:
    "postgresql://release_check:release_check@127.0.0.1:5433/release_check",
  API_URL: "http://127.0.0.1:5147",
  API_KEY: "release-check-api-key-is-at-least-32-bytes",
  ADMIN_API_KEY: "release-check-admin-api-key-is-at-least-32-bytes",
  ACTOR_ASSERTION_HMAC_KEY:
    "release-check-actor-assertion-key-is-at-least-32-bytes",
  DEV_AUTH_BYPASS: "false",
  NODE_TLS_REJECT_UNAUTHORIZED: "1",
};
const steps = [
  "diff:check",
  "format:changed",
  "audit:prod",
  "lint",
  "typecheck",
  "test",
  "build",
];

for (const step of steps) {
  console.log(`\n[release:check] npm run ${step}`);
  const command = npmCli ? process.execPath : npm;
  const args = npmCli ? [npmCli, "run", step] : ["run", step];
  const result = spawnSync(command, args, {
    env: {
      ...process.env,
      ...releaseCheckEnv,
      CI: process.env.CI ?? "1",
      NEXT_TELEMETRY_DISABLED: process.env.NEXT_TELEMETRY_DISABLED ?? "1",
    },
    shell: !npmCli && process.platform === "win32",
    stdio: "inherit",
  });

  if (result.error) {
    console.error(
      `[release:check] Could not start ${step}: ${result.error.message}`,
    );
    process.exit(1);
  }

  if (result.status !== 0) {
    console.error(
      `[release:check] ${step} failed with exit code ${result.status ?? "unknown"}.`,
    );
    process.exit(result.status ?? 1);
  }
}

console.log("\n[release:check] Frontend release gate passed.");

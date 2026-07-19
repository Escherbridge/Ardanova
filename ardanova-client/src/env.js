import nextEnv from "@next/env";
import path from "path";
import { fileURLToPath } from "url";
import { createEnv } from "@t3-oss/env-nextjs";
import { z } from "zod";

const { loadEnvConfig } = nextEnv;

const __dirname = path.dirname(fileURLToPath(import.meta.url));
// `src/env.js` -> repo root is two levels up (parent of `ardanova-client/`).
// Next loads `.env` only from `ardanova-client/`; a single root `.env` is common here.
const repoRoot = path.resolve(__dirname, "../..");
const clientRoot = path.resolve(__dirname, "..");
loadEnvConfig(repoRoot);
loadEnvConfig(clientRoot);

const unsafeSecretMarkers = [
  "replace-with",
  "your-api-key",
  "your-admin-api-key",
  "your-actor-assertion",
  "your-auth-secret",
  "placeholder",
  "change-me",
  "changeme",
  "not-a-secret",
];

function isStrongRuntimeSecret(value) {
  if (!value.trim()) return false;
  const normalized = value.toLowerCase();
  return (
    new TextEncoder().encode(value).byteLength >= 32 &&
    !unsafeSecretMarkers.some((marker) => normalized.includes(marker))
  );
}

function runtimeSecretSchema(name) {
  return z
    .string()
    .refine(
      isStrongRuntimeSecret,
      `${name} must be a generated secret containing at least 32 bytes`,
    );
}

const authSecretSchema = runtimeSecretSchema("AUTH_SECRET");
const apiKeySchema = runtimeSecretSchema("API_KEY");
const adminApiKeySchema = runtimeSecretSchema("ADMIN_API_KEY");
const actorAssertionKeySchema = runtimeSecretSchema("ACTOR_ASSERTION_HMAC_KEY");

export const env = createEnv({
  /**
   * Specify your server-side environment variables schema here. This way you can ensure the app
   * isn't built with invalid env vars.
   */
  server: {
    AUTH_SECRET: authSecretSchema,
    AUTH_URL:
      process.env.NODE_ENV === "production"
        ? z.string().url()
        : z.string().url().optional(),
    GOOGLE_CLIENT_ID: z.string(),
    GOOGLE_CLIENT_SECRET: z.string(),
    DATABASE_URL: z
      .string()
      .url()
      .refine(
        (str) => !str.includes("YOUR_MYSQL_URL_HERE"),
        "You forgot to change the default URL",
      ),
    NODE_ENV: z
      .enum(["development", "test", "production"])
      .default("development"),
    DEV_AUTH_BYPASS: z
      .enum(["true", "false"])
      .default("false")
      .transform((value) => value === "true"),
    NODE_TLS_REJECT_UNAUTHORIZED: z.enum(["1"]).optional(),
    // ArdaNova Backend API - default matches `dotnet run` + launchSettings (port 5147).
    // Use 127.0.0.1 (not "localhost") so Node's fetch targets IPv4; Kestrel often binds IPv4-only and ::1 fails with "fetch failed".
    API_URL: z.preprocess((val) => {
      if (val === undefined || val === null || val === "") return undefined;
      if (typeof val !== "string") return val;
      try {
        const u = new URL(val);
        if (u.hostname === "localhost") u.hostname = "127.0.0.1";
        return u.href.replace(/\/$/, "");
      } catch {
        return val;
      }
    }, z.string().url().default("http://127.0.0.1:5147")),
    API_KEY: apiKeySchema,
    ADMIN_API_KEY:
      process.env.NODE_ENV === "production"
        ? adminApiKeySchema
        : adminApiKeySchema.optional(),
    ACTOR_ASSERTION_HMAC_KEY: actorAssertionKeySchema,
  },

  /**
   * Specify your client-side environment variables schema here. This way you can ensure the app
   * isn't built with invalid env vars. To expose them to the client, prefix them with
   * `NEXT_PUBLIC_`.
   */
  client: {
    // NEXT_PUBLIC_CLIENTVAR: z.string(),
  },

  /**
   * You can't destruct `process.env` as a regular object in the Next.js edge runtimes (e.g.
   * middlewares) or client-side so we need to destruct manually.
   */
  runtimeEnv: {
    AUTH_SECRET: process.env.AUTH_SECRET,
    AUTH_URL: process.env.AUTH_URL,
    GOOGLE_CLIENT_ID: process.env.GOOGLE_CLIENT_ID,
    GOOGLE_CLIENT_SECRET: process.env.GOOGLE_CLIENT_SECRET,
    DATABASE_URL: process.env.DATABASE_URL,
    NODE_ENV: process.env.NODE_ENV,
    DEV_AUTH_BYPASS: process.env.DEV_AUTH_BYPASS,
    NODE_TLS_REJECT_UNAUTHORIZED: process.env.NODE_TLS_REJECT_UNAUTHORIZED,
    API_URL: process.env.API_URL,
    API_KEY: process.env.API_KEY,
    ADMIN_API_KEY: process.env.ADMIN_API_KEY,
    ACTOR_ASSERTION_HMAC_KEY: process.env.ACTOR_ASSERTION_HMAC_KEY,
  },
  /**
   * Run `build` or `dev` with `SKIP_ENV_VALIDATION` to skip env validation. This is especially
   * useful for Docker builds.
   */
  skipValidation: !!process.env.SKIP_ENV_VALIDATION,
  /**
   * Makes it so that empty strings are treated as undefined. `SOME_VAR: z.string()` and
   * `SOME_VAR=''` will throw an error.
   */
  emptyStringAsUndefined: true,
});

export type ReadinessEnvironment = Record<string, string | undefined>;

export interface ConfigurationReadiness {
  ready: boolean;
  invalid: string[];
}

export interface BackendReadiness {
  ready: boolean;
  status: "ready" | "unreachable" | "timed_out" | "invalid_url";
  httpStatus?: number;
}

export type ReadinessFetch = (
  input: URL,
  init: RequestInit,
) => Promise<Response>;

const requiredNonEmptyValues = [
  "GOOGLE_CLIENT_ID",
  "GOOGLE_CLIENT_SECRET",
  "DATABASE_URL",
  "API_KEY",
  "ADMIN_API_KEY",
] as const;

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
] as const;

function byteLength(value: string | undefined): number {
  return value ? new TextEncoder().encode(value).byteLength : 0;
}

function isStrongRuntimeSecret(value: string | undefined): boolean {
  if (!value?.trim() || byteLength(value) < 32) return false;
  const normalized = value.toLowerCase();
  return !unsafeSecretMarkers.some((marker) => normalized.includes(marker));
}

function isHttpUrl(value: string | undefined): boolean {
  if (!value) return false;
  try {
    const url = new URL(value);
    return url.protocol === "http:" || url.protocol === "https:";
  } catch {
    return false;
  }
}

function isHttpsUrl(value: string | undefined): boolean {
  if (!value) return false;
  try {
    return new URL(value).protocol === "https:";
  } catch {
    return false;
  }
}

function isHostedRuntime(environment: ReadinessEnvironment): boolean {
  return Boolean(
    environment.RAILWAY_ENVIRONMENT?.trim() ||
      environment.RAILWAY_PROJECT_ID?.trim() ||
      environment.RAILWAY_PUBLIC_DOMAIN?.trim(),
  );
}

function isDatabaseUrl(value: string | undefined): boolean {
  if (!value) return false;
  try {
    const url = new URL(value);
    return url.protocol === "postgres:" || url.protocol === "postgresql:";
  } catch {
    return false;
  }
}

/** Reports required runtime variable names without returning their values. */
export function validateReadinessConfiguration(
  environment: ReadinessEnvironment,
): ConfigurationReadiness {
  const invalid = requiredNonEmptyValues.filter(
    (name) => !environment[name]?.trim(),
  ) as string[];

  if (!isStrongRuntimeSecret(environment.AUTH_SECRET)) {
    invalid.push("AUTH_SECRET");
  }
  if (!isStrongRuntimeSecret(environment.API_KEY)) invalid.push("API_KEY");
  if (!isStrongRuntimeSecret(environment.ADMIN_API_KEY)) {
    invalid.push("ADMIN_API_KEY");
  }
  if (!isStrongRuntimeSecret(environment.ACTOR_ASSERTION_HMAC_KEY)) {
    invalid.push("ACTOR_ASSERTION_HMAC_KEY");
  }
  if (!isHttpUrl(environment.AUTH_URL)) invalid.push("AUTH_URL");
  if (!isHttpUrl(environment.API_URL)) invalid.push("API_URL");
  if (!isDatabaseUrl(environment.DATABASE_URL)) invalid.push("DATABASE_URL");

  if (isHostedRuntime(environment)) {
    if (!isHttpsUrl(environment.AUTH_URL)) invalid.push("AUTH_URL");
    if (environment.DEV_AUTH_BYPASS?.trim()) invalid.push("DEV_AUTH_BYPASS");
    if (environment.NODE_TLS_REJECT_UNAUTHORIZED?.trim() === "0") {
      invalid.push("NODE_TLS_REJECT_UNAUTHORIZED");
    }
  }

  return { ready: invalid.length === 0, invalid: [...new Set(invalid)] };
}

/** Performs a bounded call to the backend's public readiness endpoint. */
export async function checkBackendReadiness(
  apiUrl: string | undefined,
  options: {
    timeoutMs?: number;
    fetchImpl?: ReadinessFetch;
  } = {},
): Promise<BackendReadiness> {
  if (!apiUrl || !isHttpUrl(apiUrl)) {
    return { ready: false, status: "invalid_url" };
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(
    () => controller.abort(),
    options.timeoutMs ?? 2_500,
  );

  try {
    const fetchReadiness = options.fetchImpl ?? fetch;
    const response = await fetchReadiness(new URL("/ready", apiUrl), {
      method: "GET",
      headers: { Accept: "application/json" },
      cache: "no-store",
      signal: controller.signal,
    });

    return response.ok
      ? { ready: true, status: "ready", httpStatus: response.status }
      : {
          ready: false,
          status: "unreachable",
          httpStatus: response.status,
        };
  } catch (error) {
    return {
      ready: false,
      status:
        error instanceof Error && error.name === "AbortError"
          ? "timed_out"
          : "unreachable",
    };
  } finally {
    clearTimeout(timeoutId);
  }
}

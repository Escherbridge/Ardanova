const DEFAULT_SIGNED_IN_DESTINATION = "/dashboard";
export const ARDANOVA_REQUEST_PATH_HEADER = "x-ardanova-request-path";

export function normalizeInternalCallbackUrl(
  value: string | string[] | undefined,
  fallback = DEFAULT_SIGNED_IN_DESTINATION,
): string {
  const candidate = Array.isArray(value) ? value[0] : value;
  if (
    !candidate ||
    !candidate.startsWith("/") ||
    candidate.startsWith("//") ||
    candidate.includes("\\") ||
    /[\u0000-\u001f\u007f]/u.test(candidate)
  ) {
    return fallback;
  }

  try {
    const parsed = new URL(candidate, "https://ardanova.local");
    if (parsed.origin !== "https://ardanova.local") return fallback;
    return `${parsed.pathname}${parsed.search}${parsed.hash}`;
  } catch {
    return fallback;
  }
}

export function buildSignInHref(
  callbackUrl: string,
  options: { mode?: "signup" } = {},
): string {
  const params = new URLSearchParams();
  if (options.mode) params.set("mode", options.mode);
  params.set("callbackUrl", normalizeInternalCallbackUrl(callbackUrl));
  return `/auth/signin?${params.toString()}`;
}

export function normalizeInternalReturnTo(
  value: string | string[] | undefined,
  currentPath = "/settings/verification",
): string | null {
  const normalized = normalizeInternalCallbackUrl(value, "");
  if (!normalized) return null;

  const parsed = new URL(normalized, "https://ardanova.local");
  return parsed.pathname === currentPath ? null : normalized;
}

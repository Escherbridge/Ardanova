import { type NextRequest, NextResponse } from "next/server";

import { ARDANOVA_REQUEST_PATH_HEADER } from "~/lib/auth-navigation";

/** Browser hardening sent on every page and API response; see src/AGENTS.md §Security headers. */
export const SECURITY_HEADERS: Readonly<Record<string, string>> = {
  "X-Content-Type-Options": "nosniff",
  "X-Frame-Options": "DENY",
  "Content-Security-Policy": "frame-ancestors 'none'",
  "Referrer-Policy": "strict-origin-when-cross-origin",
  "Permissions-Policy": "camera=(), microphone=(), geolocation=()",
};

/** HSTS only over production TLS, so local http development is never pinned to https. */
const STRICT_TRANSPORT_SECURITY = "max-age=31536000; includeSubDomains";

export function middleware(request: NextRequest) {
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(
    ARDANOVA_REQUEST_PATH_HEADER,
    `${request.nextUrl.pathname}${request.nextUrl.search}`,
  );

  const response = NextResponse.next({ request: { headers: requestHeaders } });
  for (const [name, value] of Object.entries(SECURITY_HEADERS)) {
    response.headers.set(name, value);
  }
  if (process.env.NODE_ENV === "production") {
    response.headers.set("Strict-Transport-Security", STRICT_TRANSPORT_SECURITY);
  }
  return response;
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};

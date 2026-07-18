import { type NextRequest, NextResponse } from "next/server";

import { ARDANOVA_REQUEST_PATH_HEADER } from "~/lib/auth-navigation";

export function middleware(request: NextRequest) {
  const requestHeaders = new Headers(request.headers);
  requestHeaders.set(
    ARDANOVA_REQUEST_PATH_HEADER,
    `${request.nextUrl.pathname}${request.nextUrl.search}`,
  );

  return NextResponse.next({ request: { headers: requestHeaders } });
}

export const config = {
  matcher: [
    "/admin/:path*",
    "/chats/:path*",
    "/credentials/:path*",
    "/dashboard/:path*",
    "/events/:path*",
    "/governance/:path*",
    "/guilds/:path*",
    "/opportunities/:path*",
    "/people/:path*",
    "/portfolio/:path*",
    "/projects/:path*",
    "/settings/:path*",
    "/studio/:path*",
    "/swap/:path*",
    "/tasks/:path*",
  ],
};

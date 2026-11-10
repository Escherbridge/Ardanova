/** Public origin. Hardcoded so canonicals do not follow a host environment variable. */
export const SITE_ORIGIN = "https://ardanova.com";

/** Existing share card. Child openGraph objects must repeat it or Next drops the image. */
export const shareImage = {
  url: `${SITE_ORIGIN}/og/ardanova-og-1200x630.png`,
  width: 1200,
  height: 630,
  alt: "ArdaNova",
} as const;

/** Absolute canonical URL. The homepage keeps the trailing slash. */
export function publicCanonical(path: "/" | `/${string}`): string {
  if (path === "/") return `${SITE_ORIGIN}/`;
  return `${SITE_ORIGIN}${path}`;
}

import { buildFeedXml } from "~/lib/feed-xml";

export function GET() {
  return new Response(buildFeedXml(), {
    headers: {
      "Content-Type": "application/rss+xml; charset=utf-8",
    },
  });
}

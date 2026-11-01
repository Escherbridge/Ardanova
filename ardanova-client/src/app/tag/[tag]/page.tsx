import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";

import { canonicalHashtag } from "~/components/hashtags";
import { feedRoutes } from "~/lib/feed/routes";

import { TagFeed } from "./tag-feed";

interface TagPageProps {
  params: Promise<{ tag: string }>;
}

/** The route segment as typed; a malformed escape stays as-is and then fails the tag check. */
function segmentText(raw: string): string {
  try {
    return decodeURIComponent(raw);
  } catch {
    return raw;
  }
}

export async function generateMetadata({
  params,
}: TagPageProps): Promise<Metadata> {
  const tag = canonicalHashtag(segmentText((await params).tag));
  return { title: tag ? `#${tag}` : "Tag" };
}

/** `/tag/[tag]`: the heading and the `tag:x` stream, nothing else. See ./AGENTS.md. */
export default async function TagPage({ params }: TagPageProps) {
  const typed = segmentText((await params).tag);
  const tag = canonicalHashtag(typed);
  if (!tag) notFound();
  // One URL per tag: "/tag/Carpentry" and "/tag/carpentry-" both land on "/tag/carpentry".
  if (tag !== typed) redirect(feedRoutes.tag(tag));
  return <TagFeed tag={tag} />;
}

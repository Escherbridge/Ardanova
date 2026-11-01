import Link from "next/link";

import { feedRoutes } from "~/lib/feed/routes";
import { cn } from "~/lib/utils";

/** One stored-form tag as a link to /tag/[tag]. */
export function HashtagChip({
  tag,
  className,
}: {
  tag: string;
  className?: string;
}) {
  return (
    <Link
      href={feedRoutes.tag(tag)}
      className={cn(
        "border-border text-muted-foreground hover:border-foreground hover:text-foreground inline-flex min-h-8 items-center border px-2 font-mono text-xs transition-colors",
        className,
      )}
    >
      #{tag}
    </Link>
  );
}

/** A row of tag chips; renders nothing for an empty list. */
export function HashtagList({
  tags,
  className,
}: {
  tags: readonly string[];
  className?: string;
}) {
  if (tags.length === 0) return null;
  return (
    <ul aria-label="Tags" className={cn("flex flex-wrap gap-1.5", className)}>
      {tags.map((tag) => (
        <li key={tag}>
          <HashtagChip tag={tag} />
        </li>
      ))}
    </ul>
  );
}

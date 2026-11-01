"use client";

import { useSession } from "next-auth/react";

import { FeedStream } from "~/components/feed";
import { useSubjectFollow } from "~/lib/feed/use-subject-follow";

/** One tag's stream under its `#tag` heading. */
export function TagFeed({ tag }: { tag: string }) {
  const { data: session } = useSession();
  const onToggleFollow = useSubjectFollow();

  return (
    <div className="mx-auto w-full max-w-[40rem] py-6 sm:py-10">
      <h1 className="border-foreground mb-6 border-b-2 pb-4 text-4xl leading-none break-words sm:text-5xl">
        <span className="text-primary">#</span>
        {tag}
      </h1>
      <FeedStream
        key={tag}
        scope={`tag:${tag}`}
        label={`#${tag}`}
        viewerId={session?.user?.id ?? null}
        onToggleFollow={onToggleFollow}
      />
    </div>
  );
}

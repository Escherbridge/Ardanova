"use client";

import { Paperclip } from "lucide-react";

import { parseProjectMediaItems } from "~/lib/contracts/project-create-contract";

/** Attachments on an update, shown as files rather than as the raw stored column. */
export function UpdateMediaStrip({ stored }: { stored: string | null | undefined }) {
  const media = parseProjectMediaItems(stored);
  if (media.length === 0) return null;

  return (
    <ul
      aria-label="Update attachments"
      className="flex list-none flex-wrap gap-2 pt-2"
    >
      {media.map((item) => {
        const caption = item.fileName || "Attachment";

        if (item.kind === "image" && item.url) {
          return (
            <li key={item.bucketPath}>
              {/* next/image is not used: bucket hosts are deployment-specific and are
                  not in an images.remotePatterns allow-list. */}
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={item.url}
                alt={caption}
                loading="lazy"
                className="border-border size-20 rounded-lg border-2 object-cover"
              />
            </li>
          );
        }

        const chip = (
          <span className="border-border bg-muted/40 flex items-center gap-2 rounded-lg border px-3 py-2 text-xs">
            <Paperclip className="size-3" aria-hidden="true" />
            <span className="max-w-40 truncate">{caption}</span>
          </span>
        );

        return (
          <li key={item.bucketPath}>
            {item.url ? (
              <a
                href={item.url}
                target="_blank"
                rel="noopener noreferrer"
                className="focus-visible:ring-ring inline-flex rounded-lg focus-visible:ring-2 focus-visible:outline-none"
              >
                {chip}
              </a>
            ) : (
              chip
            )}
          </li>
        );
      })}
    </ul>
  );
}

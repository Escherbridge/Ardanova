import { Fragment } from "react";
import Link from "next/link";

import { feedRoutes } from "~/lib/feed/routes";
import { cn } from "~/lib/utils";

import { tokenizeHashtags } from "./hashtag-tokens";

/** A person's words with each #tag linked to its tag page; everything else is left exactly as written. */
export function HashtagText({
  text,
  linkClassName,
}: {
  text: string;
  linkClassName?: string;
}) {
  return (
    <>
      {tokenizeHashtags(text).map((token, index) =>
        token.kind === "tag" ? (
          <Link
            key={index}
            href={feedRoutes.tag(token.tag)}
            className={cn(
              // A visible underline, not colour alone, marks the link (WCAG 1.4.1).
              "text-system decoration-system/40 font-medium underline underline-offset-2 transition-colors hover:decoration-current focus-visible:decoration-current",
              linkClassName,
            )}
          >
            {token.text}
          </Link>
        ) : (
          <Fragment key={index}>{token.text}</Fragment>
        ),
      )}
    </>
  );
}

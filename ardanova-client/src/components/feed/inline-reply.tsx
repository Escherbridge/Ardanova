"use client";

import { useEffect, useId, useRef, useState } from "react";
import { Loader2 } from "lucide-react";

import { formatNoteAge } from "~/components/feed/field-note-audience";
import { MutationErrorNotice } from "~/components/ui/query-error-state";
import { Button } from "~/components/ui/button";
import { Textarea } from "~/components/ui/textarea";
import { api } from "~/trpc/react";

interface ReplyLike {
  id: string;
  authorId: string;
  content: string;
  createdAt: string;
}

/** The newest reply only: the card previews at most one (plan §1 card anatomy, row 6). */
function newestReply(
  replies: readonly ReplyLike[] | undefined,
): ReplyLike | null {
  if (!replies?.length) return null;
  return replies.reduce((newest, reply) =>
    Date.parse(reply.createdAt) > Date.parse(newest.createdAt) ? reply : newest,
  );
}

/** Mounted only while open, so a closed card makes no reply reads (no N+1 down the stream). */
export function InlineReply({
  postId,
  panelId,
  placeholder,
  viewerId,
  askerId,
  askerName,
  onSent,
}: {
  postId: string;
  panelId: string;
  placeholder: string;
  viewerId?: string | null;
  askerId: string;
  askerName: string | null;
  onSent: () => void;
}) {
  const fieldId = useId();
  const fieldRef = useRef<HTMLTextAreaElement>(null);
  const [draft, setDraft] = useState("");

  const replies = api.post.getComments.useQuery({ id: postId });
  const send = api.post.addComment.useMutation({
    onSuccess: async () => {
      setDraft("");
      onSent();
      await replies.refetch();
    },
  });

  useEffect(() => {
    fieldRef.current?.focus();
  }, []);

  const latest = newestReply(replies.data);
  const latestBy =
    latest && viewerId && latest.authorId === viewerId
      ? "You"
      : latest && latest.authorId === askerId
        ? askerName
        : null;
  const content = draft.trim();

  return (
    <section
      id={panelId}
      aria-label="Replies"
      className="border-border mt-4 border-t border-dashed pt-3"
    >
      {latest ? (
        <figure className="border-border mb-3 border-l-2 pl-3">
          <blockquote className="line-clamp-3 text-sm leading-relaxed whitespace-pre-wrap">
            {latest.content}
          </blockquote>
          <figcaption className="text-muted-foreground mt-1 font-mono text-[11px]">
            {latestBy ? `${latestBy} · ` : ""}
            <time dateTime={latest.createdAt} suppressHydrationWarning>
              {formatNoteAge(latest.createdAt)}
            </time>
          </figcaption>
        </figure>
      ) : null}

      <form
        onSubmit={(event) => {
          event.preventDefault();
          if (!content || send.isPending) return;
          send.mutate({ postId, content });
        }}
      >
        <label htmlFor={fieldId} className="sr-only">
          Reply
        </label>
        <Textarea
          id={fieldId}
          ref={fieldRef}
          rows={2}
          value={draft}
          placeholder={placeholder}
          onChange={(event) => setDraft(event.target.value)}
        />
        <div className="mt-2 flex justify-end">
          <Button
            type="submit"
            size="sm"
            variant="outline"
            disabled={!content || send.isPending}
          >
            {send.isPending ? (
              <Loader2 aria-hidden className="animate-spin" />
            ) : null}
            Send reply
          </Button>
        </div>
        <MutationErrorNotice
          error={send.error}
          action="send your reply"
          className="mt-2"
        />
      </form>
    </section>
  );
}

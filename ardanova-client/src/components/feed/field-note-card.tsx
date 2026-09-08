"use client";

import { useState } from "react";
import { Heart, Loader2, MessageSquare, Pin, Trash2 } from "lucide-react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Textarea } from "~/components/ui/textarea";
import { api } from "~/trpc/react";
import { audienceLabel, formatNoteAge } from "./field-note-audience";

export interface FieldNote {
  id: string;
  authorId: string;
  title?: string | null;
  content: string;
  visibility: string;
  likesCount: number;
  commentsCount: number;
  isPinned: boolean;
  createdAt: string;
}

interface FieldNoteCardProps {
  note: FieldNote;
  /** The reader, used only to label their own notes and offer removal. */
  viewerId?: string;
  onChanged?: () => void;
}

export function FieldNoteCard({
  note,
  viewerId,
  onChanged,
}: FieldNoteCardProps) {
  const [showReplies, setShowReplies] = useState(false);
  const [reply, setReply] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const isOwnNote = viewerId !== undefined && viewerId === note.authorId;

  const replies = api.post.getComments.useQuery(
    { id: note.id },
    { enabled: showReplies },
  );

  const appreciate = api.post.like.useMutation({
    onSuccess: () => onChanged?.(),
    onError: (error) => setErrorMessage(error.message),
  });

  const addReply = api.post.addComment.useMutation({
    onSuccess: async () => {
      setReply("");
      setErrorMessage(null);
      await replies.refetch();
      onChanged?.();
    },
    onError: (error) => setErrorMessage(error.message),
  });

  const removeNote = api.post.delete.useMutation({
    onSuccess: () => onChanged?.(),
    onError: (error) => setErrorMessage(error.message),
  });

  return (
    <article className="border border-border bg-card p-4">
      <header className="flex flex-wrap items-center gap-2">
        <span className="text-sm font-semibold">
          {isOwnNote ? "You" : "Project team member"}
        </span>
        <span className="text-xs text-muted-foreground">
          {formatNoteAge(note.createdAt)}
        </span>
        {note.isPinned ? (
          <Badge variant="outline" size="sm">
            <Pin className="mr-1 size-3" />
            Pinned
          </Badge>
        ) : null}
        <Badge variant="secondary" size="sm">
          {audienceLabel(note.visibility)}
        </Badge>
      </header>

      {note.title ? (
        <h3 className="mt-2 text-base font-semibold">{note.title}</h3>
      ) : null}

      <p className="mt-2 whitespace-pre-wrap text-sm text-foreground">
        {note.content}
      </p>

      <footer className="mt-3 flex flex-wrap items-center gap-2">
        <Button
          type="button"
          variant="ghost"
          size="sm"
          disabled={appreciate.isPending}
          onClick={() => appreciate.mutate({ id: note.id })}
        >
          <Heart />
          {note.likesCount}
        </Button>

        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => setShowReplies((open) => !open)}
        >
          <MessageSquare />
          {note.commentsCount} {note.commentsCount === 1 ? "reply" : "replies"}
        </Button>

        {isOwnNote ? (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            disabled={removeNote.isPending}
            onClick={() => removeNote.mutate({ id: note.id })}
          >
            <Trash2 />
            Remove
          </Button>
        ) : null}
      </footer>

      {showReplies ? (
        <section className="mt-3 space-y-3 border-t border-border pt-3">
          {replies.isLoading ? (
            <p className="flex items-center gap-2 text-xs text-muted-foreground">
              <Loader2 className="size-3 animate-spin" />
              Loading replies
            </p>
          ) : null}

          {(replies.data ?? []).map((entry) => (
            <div key={entry.id} className="text-sm">
              <span className="text-xs text-muted-foreground">
                {viewerId !== undefined && viewerId === entry.authorId
                  ? "You"
                  : "Project team member"}{" "}
                · {formatNoteAge(entry.createdAt)}
              </span>
              <p className="whitespace-pre-wrap">{entry.content}</p>
            </div>
          ))}

          <form
            className="space-y-2"
            onSubmit={(event) => {
              event.preventDefault();
              if (reply.trim() === "" || addReply.isPending) return;
              addReply.mutate({ postId: note.id, content: reply.trim() });
            }}
          >
            <Textarea
              value={reply}
              onChange={(event) => setReply(event.target.value)}
              placeholder="Add a reply"
              rows={2}
            />
            <Button
              type="submit"
              size="sm"
              disabled={reply.trim() === "" || addReply.isPending}
            >
              {addReply.isPending ? <Loader2 className="animate-spin" /> : null}
              Reply
            </Button>
          </form>
        </section>
      ) : null}

      {errorMessage ? (
        <p className="mt-2 text-xs text-destructive">{errorMessage}</p>
      ) : null}
    </article>
  );
}

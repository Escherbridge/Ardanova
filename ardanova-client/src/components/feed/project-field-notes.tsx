"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "~/components/ui/button";
import { api } from "~/trpc/react";
import { FieldNoteCard, type FieldNote } from "./field-note-card";
import { FieldNoteComposer } from "./field-note-composer";

interface ProjectFieldNotesProps {
  projectId: string;
  /** Team members may write; everyone else reads what the server lets them see. */
  canPost?: boolean;
  /** The reader, used only to label their own notes. */
  viewerId?: string;
}

const pageSize = 20;

/**
 * The project field-notes stream: updates from the people doing the work, in one place.
 * There is no global feed — notes belong to the project they were written in.
 */
export function ProjectFieldNotes({
  projectId,
  canPost = false,
  viewerId,
}: ProjectFieldNotesProps) {
  const [page, setPage] = useState(1);
  const utilities = api.useUtils();

  const stream = api.post.getProjectFieldNotes.useQuery({
    projectId,
    page,
    pageSize,
  });

  const refresh = async () => {
    await utilities.post.getProjectFieldNotes.invalidate({ projectId });
  };

  const notes = (stream.data?.items ?? []) as FieldNote[];
  const totalPages = stream.data?.totalPages ?? 1;

  return (
    <section className="space-y-4">
      <header>
        <h2 className="text-lg font-semibold">Field notes</h2>
        <p className="text-sm text-muted-foreground">
          Progress, setbacks and decisions, written down as the work happens.
        </p>
      </header>

      {canPost ? (
        <FieldNoteComposer projectId={projectId} onPosted={refresh} />
      ) : null}

      {stream.isLoading ? (
        <p className="flex items-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="size-4 animate-spin" />
          Loading field notes
        </p>
      ) : null}

      {stream.error ? (
        <p className="text-sm text-destructive">{stream.error.message}</p>
      ) : null}

      {!stream.isLoading && !stream.error && notes.length === 0 ? (
        <p className="border border-dashed border-border p-6 text-center text-sm text-muted-foreground">
          No field notes yet. The first update tells supporters the work has started.
        </p>
      ) : null}

      <div className="space-y-3">
        {notes.map((note) => (
          <FieldNoteCard
            key={note.id}
            note={note}
            viewerId={viewerId}
            onChanged={refresh}
          />
        ))}
      </div>

      {totalPages > 1 ? (
        <nav className="flex items-center justify-between gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
          >
            Newer
          </Button>
          <span className="text-xs text-muted-foreground">
            Page {page} of {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((current) => current + 1)}
          >
            Older
          </Button>
        </nav>
      ) : null}
    </section>
  );
}

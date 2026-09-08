"use client";

import { useState } from "react";
import { Loader2, Send } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Textarea } from "~/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { api } from "~/trpc/react";
import {
  audienceDescription,
  audienceLabel,
  projectAudienceOptions,
  type FieldNoteAudience,
} from "./field-note-audience";

interface FieldNoteComposerProps {
  projectId: string;
  onPosted?: () => void;
}

/** Writes one field note into a project stream. The author is the signed-in person, server-side. */
export function FieldNoteComposer({
  projectId,
  onPosted,
}: FieldNoteComposerProps) {
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [audience, setAudience] = useState<FieldNoteAudience>("PUBLIC");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const createNote = api.post.create.useMutation({
    onSuccess: () => {
      setTitle("");
      setContent("");
      setErrorMessage(null);
      onPosted?.();
    },
    onError: (error) => setErrorMessage(error.message),
  });

  const canSubmit = content.trim().length > 0 && !createNote.isPending;

  return (
    <form
      className="border border-border bg-card p-4"
      onSubmit={(event) => {
        event.preventDefault();
        if (!canSubmit) return;
        createNote.mutate({
          projectId,
          content: content.trim(),
          title: title.trim() === "" ? undefined : title.trim(),
          visibility: audience,
          type: "PROJECT_UPDATE",
        });
      }}
    >
      <div className="space-y-3">
        <Input
          value={title}
          onChange={(event) => setTitle(event.target.value)}
          placeholder="Headline (optional)"
          maxLength={140}
        />
        <Textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          placeholder="What happened, what you learned, what you need next."
          rows={4}
          required
        />

        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground">Who can read</span>
            <Select
              value={audience}
              onValueChange={(value) =>
                setAudience(value as FieldNoteAudience)
              }
            >
              <SelectTrigger className="w-44">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {projectAudienceOptions.map((option) => (
                  <SelectItem key={option} value={option}>
                    {audienceLabel(option)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <Button type="submit" disabled={!canSubmit}>
            {createNote.isPending ? (
              <Loader2 className="animate-spin" />
            ) : (
              <Send />
            )}
            Post update
          </Button>
        </div>

        <p className="text-xs text-muted-foreground">
          {audienceDescription(audience)}
        </p>

        {errorMessage ? (
          <p className="text-xs text-destructive">{errorMessage}</p>
        ) : null}
      </div>
    </form>
  );
}

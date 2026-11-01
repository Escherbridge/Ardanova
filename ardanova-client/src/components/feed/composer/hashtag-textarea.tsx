"use client";

import { useLayoutEffect, useRef, useState, type KeyboardEvent } from "react";

import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

import {
  hashtagFragmentAt,
  hashtagSuggestions,
  insertHashtag,
} from "./hashtag-suggestions";

/** One ruled line of the sheet; the text's line-height matches it so words sit on the rules. */
const RULE = "1.75rem";
const RULED_PAPER = {
  backgroundImage: `linear-gradient(to bottom, transparent calc(${RULE} - 1px), color-mix(in oklab, var(--border) 28%, transparent) calc(${RULE} - 1px))`,
  backgroundSize: `100% ${RULE}`,
  backgroundPositionY: "0.75rem",
  backgroundAttachment: "local",
} as const;

/**
 * The writing surface: a textarea with inline `#` suggestions from canonical TaxonomyTerms, as an ARIA listbox driven
 * by aria-activedescendant so focus never leaves the text. Terms load the first time a `#` is typed. See ./AGENTS.md.
 */
export function HashtagTextarea({
  id,
  value,
  onChange,
  labelledBy,
  disabled = false,
}: {
  id: string;
  value: string;
  onChange: (value: string) => void;
  labelledBy: string;
  disabled?: boolean;
}) {
  const fieldRef = useRef<HTMLTextAreaElement>(null);
  const pendingCaret = useRef<number | null>(null);
  const [caret, setCaret] = useState(0);
  const [activeIndex, setActiveIndex] = useState(0);
  const [dismissedAt, setDismissedAt] = useState<number | null>(null);
  const [termsRequested, setTermsRequested] = useState(false);

  const terms = api.taxonomy.getAll.useQuery(undefined, {
    enabled: termsRequested,
    staleTime: Infinity,
    refetchOnWindowFocus: false,
  });

  const fragment = hashtagFragmentAt(value, caret);
  const suggestions =
    fragment && terms.data
      ? hashtagSuggestions(terms.data, fragment.query)
      : [];
  const open =
    !disabled &&
    suggestions.length > 0 &&
    dismissedAt !== (fragment?.start ?? null);
  const active = Math.min(activeIndex, Math.max(0, suggestions.length - 1));
  const listId = `${id}-tags`;
  const optionId = (index: number) => `${id}-tag-${index}`;

  // Put the caret after an accepted tag once React has written the new value.
  useLayoutEffect(() => {
    const field = fieldRef.current;
    if (pendingCaret.current === null || !field) return;
    field.setSelectionRange(pendingCaret.current, pendingCaret.current);
    setCaret(pendingCaret.current);
    pendingCaret.current = null;
  }, [value]);

  const syncCaret = () => setCaret(fieldRef.current?.selectionStart ?? 0);

  const accept = (index: number) => {
    const suggestion = suggestions[index];
    if (!fragment || !suggestion) return;
    const next = insertHashtag(value, fragment, caret, suggestion.tag);
    pendingCaret.current = next.caret;
    setActiveIndex(0);
    onChange(next.text);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && (event.metaKey || event.ctrlKey)) {
      event.preventDefault();
      event.currentTarget.form?.requestSubmit();
      return;
    }
    if (!open) return;
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        setActiveIndex((active + 1) % suggestions.length);
        return;
      case "ArrowUp":
        event.preventDefault();
        setActiveIndex((active - 1 + suggestions.length) % suggestions.length);
        return;
      case "Enter":
      case "Tab":
        event.preventDefault();
        accept(active);
        return;
      case "Escape":
        event.preventDefault();
        setDismissedAt(fragment?.start ?? null);
        return;
    }
  };

  return (
    <div className="relative">
      <textarea
        id={id}
        ref={fieldRef}
        value={value}
        disabled={disabled}
        rows={3}
        aria-labelledby={labelledBy}
        aria-autocomplete="list"
        aria-controls={open ? listId : undefined}
        aria-activedescendant={open ? optionId(active) : undefined}
        onChange={(event) => {
          const text = event.target.value;
          const at = event.target.selectionStart ?? text.length;
          onChange(text);
          setCaret(at);
          setActiveIndex(0);
          if (!termsRequested && hashtagFragmentAt(text, at)) {
            setTermsRequested(true);
          }
        }}
        onSelect={syncCaret}
        onClick={syncCaret}
        onKeyUp={(event) => {
          if (!open && event.key.startsWith("Arrow")) syncCaret();
        }}
        onKeyDown={onKeyDown}
        onBlur={() => setDismissedAt(fragment?.start ?? null)}
        onFocus={() => setDismissedAt(null)}
        style={RULED_PAPER}
        className="block field-sizing-content min-h-[6.75rem] w-full resize-none bg-transparent px-4 py-3 text-[15px] leading-7 outline-none disabled:opacity-60"
      />

      {open ? (
        <ul
          id={listId}
          role="listbox"
          aria-label="Tags"
          className="border-foreground bg-card absolute inset-x-4 top-full z-20 -mt-2 max-w-sm border shadow-[4px_4px_0_0_var(--foreground)] sm:right-auto sm:min-w-64"
        >
          {suggestions.map((suggestion, index) => (
            <li
              key={suggestion.tag}
              id={optionId(index)}
              role="option"
              aria-selected={index === active}
              // Keep focus (and the caret) in the text while a pointer picks a tag.
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => accept(index)}
              onMouseEnter={() => setActiveIndex(index)}
              className={cn(
                "flex min-h-10 cursor-pointer items-baseline justify-between gap-4 px-3 py-2",
                index === active && "bg-foreground text-background",
              )}
            >
              <span className="font-mono text-[13px] font-semibold">
                #{suggestion.tag}
              </span>
              <span
                className={cn(
                  "truncate text-[11px]",
                  index === active ? "opacity-80" : "text-muted-foreground",
                )}
              >
                {suggestion.label}
              </span>
            </li>
          ))}
        </ul>
      ) : null}

      <p className="sr-only" aria-live="polite">
        {open
          ? `${suggestions.length} ${suggestions.length === 1 ? "tag" : "tags"} found`
          : ""}
      </p>
    </div>
  );
}

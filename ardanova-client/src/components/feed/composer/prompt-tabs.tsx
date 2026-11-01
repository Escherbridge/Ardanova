"use client";

import { useRef, type KeyboardEvent } from "react";

import { cn } from "~/lib/utils";

import { COMPOSER_PROMPTS, type ComposerPrompt } from "./prompts";

/** Id of a prompt's radio, so the writing surface can be labelled by the chosen prompt. */
export const promptRadioId = (baseId: string, prompt: ComposerPrompt) =>
  `${baseId}-prompt-${prompt}`;

/**
 * The three prompts as a radio group (roving tabindex, arrow keys wrap) drawn as tabs on the sheet's top edge.
 * The chosen tab opens into the sheet; "Who can help?" takes the help card's red rule.
 */
export function PromptTabs({
  baseId,
  value,
  onChange,
  prompts = COMPOSER_PROMPTS,
}: {
  baseId: string;
  value: ComposerPrompt;
  onChange: (prompt: ComposerPrompt) => void;
  prompts?: readonly (typeof COMPOSER_PROMPTS)[number][];
}) {
  const radios = useRef(new Map<ComposerPrompt, HTMLButtonElement>());

  const move = (event: KeyboardEvent<HTMLDivElement>) => {
    const step =
      event.key === "ArrowRight" || event.key === "ArrowDown"
        ? 1
        : event.key === "ArrowLeft" || event.key === "ArrowUp"
          ? -1
          : 0;
    const edge =
      event.key === "Home" ? 0 : event.key === "End" ? prompts.length - 1 : -1;
    if (step === 0 && edge === -1) return;
    event.preventDefault();
    const current = prompts.findIndex((prompt) => prompt.id === value);
    const next =
      edge >= 0 ? edge : (current + step + prompts.length) % prompts.length;
    const target = prompts[next];
    if (!target) return;
    onChange(target.id);
    radios.current.get(target.id)?.focus();
  };

  return (
    <div
      role="radiogroup"
      aria-label="Prompt"
      onKeyDown={move}
      className="border-border flex border-b"
    >
      {prompts.map((prompt) => {
        const checked = prompt.id === value;
        return (
          <button
            key={prompt.id}
            ref={(node) => {
              if (node) radios.current.set(prompt.id, node);
              else radios.current.delete(prompt.id);
            }}
            id={promptRadioId(baseId, prompt.id)}
            type="button"
            role="radio"
            aria-checked={checked}
            tabIndex={checked ? 0 : -1}
            onClick={() => onChange(prompt.id)}
            className={cn(
              "border-border focus-visible:outline-ring relative min-h-11 flex-1 border-r px-3 text-left text-[13px] leading-tight font-semibold transition-colors last:border-r-0 focus-visible:outline-2 focus-visible:-outline-offset-2 sm:flex-none sm:px-4",
              // The chosen tab opens into the sheet: same paper, and a bar that names it.
              "after:absolute after:inset-x-0 after:-bottom-px after:h-[2px] after:origin-left after:transition-transform motion-reduce:after:transition-none",
              checked
                ? "bg-card text-foreground after:scale-x-100"
                : "bg-muted/40 text-muted-foreground hover:text-foreground after:scale-x-0",
              prompt.id === "help" ? "after:bg-primary" : "after:bg-foreground",
            )}
          >
            {prompt.label}
          </button>
        );
      })}
    </div>
  );
}

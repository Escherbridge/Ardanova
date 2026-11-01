"use client";

import { useId, type ReactNode } from "react";
import { ChevronDown } from "lucide-react";

import { cn } from "~/lib/utils";

/** A native select with a ledger-style mono label: keyboard, screen-reader and mobile pickers come for free. */
export function ComposerSelect({
  label,
  value,
  onChange,
  children,
  className,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  children: ReactNode;
  className?: string;
}) {
  const id = useId();
  return (
    <div className={cn("flex min-w-0 items-center gap-2", className)}>
      <label
        htmlFor={id}
        className="text-muted-foreground shrink-0 font-mono text-[11px] font-semibold tracking-[0.08em] uppercase"
      >
        {label}
      </label>
      <span className="relative min-w-0">
        <select
          id={id}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          className="border-border bg-background hover:border-foreground focus-visible:outline-ring min-h-11 w-full max-w-56 min-w-0 appearance-none truncate border py-1 pr-8 pl-2.5 text-sm transition-colors focus-visible:outline-2 focus-visible:outline-offset-1"
        >
          {children}
        </select>
        <ChevronDown
          aria-hidden
          className="text-muted-foreground pointer-events-none absolute top-1/2 right-2 size-4 -translate-y-1/2"
        />
      </span>
    </div>
  );
}

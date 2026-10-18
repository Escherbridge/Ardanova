import Link from "next/link";
import type { ReactNode } from "react";

import { ArdaNovaWordmark } from "~/components/brand/wordmark";

/** Full-page branded status screen shared by not-found, error and global-error. See `src/components/ui/AGENTS.md` §status-page. */
export function StatusPage({
  eyebrow,
  title,
  body,
  actions,
}: {
  eyebrow: string;
  title: string;
  body: ReactNode;
  actions: ReactNode;
}) {
  return (
    <div className="public-shell bg-background text-foreground flex min-h-screen flex-col">
      <header className="border-b border-black/70">
        <div className="mx-auto flex min-h-16 max-w-[1440px] items-center px-4 md:px-8">
          <Link
            aria-label="ArdaNova home"
            className="inline-flex min-h-11 items-center"
            href="/"
          >
            <ArdaNovaWordmark className="text-base md:text-lg" />
          </Link>
        </div>
      </header>
      <main
        className="mx-auto flex w-full max-w-[1440px] flex-1 flex-col justify-center px-4 py-16 md:px-8"
        id="main-content"
      >
        <p className="text-brand-signal-deep mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
          {eyebrow}
        </p>
        <h1 className="mb-6 max-w-3xl">{title}</h1>
        <div className="text-muted-foreground mb-10 max-w-2xl text-lg leading-relaxed">
          {body}
        </div>
        <div className="flex flex-wrap gap-3">{actions}</div>
      </main>
    </div>
  );
}

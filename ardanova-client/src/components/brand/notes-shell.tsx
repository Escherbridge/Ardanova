import Link from "next/link";
import type { ReactNode } from "react";

import { SiteFooter } from "~/components/brand/site-footer";
import { ArdaNovaWordmark } from "~/components/brand/wordmark";

/** Public writing page. Legal pages keep their own shell. */
export function NotesShell({
  kicker,
  title,
  children,
}: {
  kicker?: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="public-shell bg-background text-foreground flex min-h-screen flex-col">
      <header className="border-b border-black/70">
        <div className="mx-auto flex min-h-16 max-w-[1440px] items-center justify-between gap-6 px-4 md:px-8">
          <Link
            aria-label="ArdaNova home"
            className="inline-flex min-h-11 items-center"
            href="/"
          >
            <ArdaNovaWordmark className="text-base md:text-lg" />
          </Link>
          <Link
            className="inline-flex min-h-11 items-center text-sm font-semibold"
            href="/notes"
          >
            Notes
          </Link>
        </div>
      </header>
      <main
        className="mx-auto w-full max-w-[1440px] flex-1 px-4 py-12 md:px-8"
        id="main-content"
      >
        {kicker ? (
          <p className="text-brand-signal-deep mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
            {kicker}
          </p>
        ) : null}
        <h1 className="mb-10 max-w-3xl">{title}</h1>
        <article className="max-w-3xl space-y-6 text-base leading-relaxed">
          {children}
        </article>
      </main>
      <SiteFooter workspaceHref="/dashboard" />
    </div>
  );
}

import Link from "next/link";
import type { ReactNode } from "react";

import { SiteFooter } from "~/components/brand/site-footer";
import { ArdaNovaWordmark } from "~/components/brand/wordmark";

/** Long-form public legal page. Money and legal copy stays in full sentences. */
export function LegalDocument({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
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
        className="mx-auto w-full max-w-[1440px] flex-1 px-4 py-12 md:px-8"
        id="main-content"
      >
        <h1 className="mb-3 max-w-3xl">{title}</h1>
        <p className="text-muted-foreground mb-10 font-mono text-xs tracking-[0.14em] uppercase">
          Effective 10 November 2026
        </p>
        <article className="max-w-3xl space-y-8 text-base leading-relaxed">
          {children}
        </article>
      </main>
      <SiteFooter workspaceHref="/dashboard" />
    </div>
  );
}

export function LegalSection({
  heading,
  children,
}: {
  heading: string;
  children: ReactNode;
}) {
  return (
    <section className="space-y-3">
      <h2 className="text-xl">{heading}</h2>
      <div className="text-muted-foreground space-y-3">{children}</div>
    </section>
  );
}

/** Operator block shared by the terms and the privacy notice. */
export function OperatorContact() {
  return (
    <address className="text-muted-foreground space-y-1 not-italic">
      <p>Escherbridge, LLC</p>
      <p>Ahmed Zaher</p>
      <p>
        <a className="underline" href="mailto:contact@escherbridge.com">
          contact@escherbridge.com
        </a>
      </p>
      <p>
        <a className="underline" href="tel:+15095784603">
          (509) 578-4603
        </a>
      </p>
      <p>1140 S Colorado Ln, Boise, ID</p>
    </address>
  );
}

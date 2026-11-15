import Link from "next/link";

import { JsonLd } from "~/components/brand/json-ld";
import { NotesShell } from "~/components/brand/notes-shell";
import { SimulationNotice } from "~/components/brand/simulation-notice";
import { WorkspaceFunnelGate } from "~/components/brand/WorkspaceFunnelGate";
import { breadcrumbList, itemList } from "~/lib/schema";
import { publicPageMetadata } from "~/lib/page-metadata";
import {
  PATTERN_SHELVES,
  patternCanonical,
  researchedPatterns,
} from "~/lib/writing";

const description =
  "Fifteen engineering pattern guides for shared workspaces: multi-ledger agreements, append-only decision logs, readable minutes, and durable membership records.";

export const metadata = publicPageMetadata({
  path: "/patterns",
  title: "Patterns",
  description,
});

export default function PatternsPage() {
  return (
    <>
      <JsonLd
        data={breadcrumbList([
          { name: "Home", path: "/" },
          { name: "Patterns", path: "/patterns" },
        ])}
      />
      <JsonLd
        data={itemList(
          "Patterns",
          researchedPatterns.map((pattern) => patternCanonical(pattern.slug)),
        )}
      />
      <NotesShell
        kicker="Architecture Patterns · Shared Custody & Records"
        title="The shape of a shared record."
        trail={[
          { href: "/", label: "Home" },
          { href: "/patterns", label: "Patterns" },
        ]}
      >
        <p className="text-lg leading-relaxed">
          Shared work breaks down less often over bad intentions than over
          sloppy records: a scope change buried in chat, two partner
          spreadsheets that disagree on who approved a milestone, or meeting
          minutes written so vaguely a new steward cannot tell what was actually
          decided.
        </p>
        <p>
          These fifteen pattern notes break down durable shapes for keeping
          shared records clear, auditable, and inspectable across
          organizations—from separating work logs (Record A) from governance
          voice (Record B) to reconciling ledgers without handing sole control
          to an opaque middleman.
        </p>

        {PATTERN_SHELVES.map((shelf) => {
          const items = researchedPatterns.filter(
            (pattern) => "shelf" in pattern && pattern.shelf === shelf.id,
          );
          if (items.length === 0) return null;
          return (
            <section className="space-y-5 pt-4" key={shelf.id}>
              <div className="border-b border-black/70 pb-3">
                <h2 className="text-2xl font-bold tracking-[-0.02em]">
                  {shelf.title}
                </h2>
                <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                  {shelf.summary}
                </p>
              </div>
              <ul className="grid gap-5">
                {items.map((pattern) => (
                  <li
                    className="bg-brand-paper-bright border border-black/70 p-6"
                    key={pattern.slug}
                  >
                    <p className="text-brand-signal-deep font-mono text-[11px] font-semibold tracking-[0.14em] uppercase">
                      {pattern.publishedAt}
                    </p>
                    <h3 className="mt-1.5 text-2xl font-bold tracking-[-0.02em]">
                      <Link
                        className="underline-offset-4 hover:underline"
                        href={`/patterns/${pattern.slug}`}
                      >
                        {pattern.title}
                      </Link>
                    </h3>
                    {"indexBlurb" in pattern && pattern.indexBlurb ? (
                      <p className="text-muted-foreground mt-3 leading-relaxed">
                        {pattern.indexBlurb}
                      </p>
                    ) : null}
                    <p className="mt-4">
                      <Link
                        className="inline-flex min-h-11 items-center font-semibold underline underline-offset-4 hover:underline"
                        href={`/patterns/${pattern.slug}`}
                      >
                        Read pattern
                      </Link>
                    </p>
                  </li>
                ))}
              </ul>
            </section>
          );
        })}

        <WorkspaceFunnelGate surface="patterns-index" />

        <SimulationNotice includeAzoa includeLegalAdvice />

        <p className="text-muted-foreground text-sm">
          Author: Escherbridge · Last reviewed: 2026-11-14 ·{" "}
          <Link className="underline underline-offset-4" href="/notes">
            Engineering notes
          </Link>{" "}
          ·{" "}
          <Link className="underline underline-offset-4" href="/terms">
            Terms
          </Link>{" "}
          ·{" "}
          <Link className="underline underline-offset-4" href="/feed.xml">
            RSS feed
          </Link>
        </p>
      </NotesShell>
    </>
  );
}

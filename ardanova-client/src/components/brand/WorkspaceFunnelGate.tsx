import Link from "next/link";
import { ArrowRight } from "lucide-react";

/**
 * Clean bottom-of-page funnel gate moving builders from engineering patterns
 * or notes into opening an ArdaNova workspace, browsing sibling patterns, or
 * inspecting stateful workflow execution on AZOA.
 */
export function WorkspaceFunnelGate({
  surface,
}: {
  surface: "patterns-index" | "pattern-detail" | "notes-index" | "note-detail";
}) {
  const showPatternCta =
    surface === "notes-index" ||
    surface === "note-detail" ||
    surface === "pattern-detail";
  const showNotesCta =
    surface === "patterns-index" || surface === "pattern-detail";

  return (
    <section
      aria-label="Next steps for builders"
      className="bg-brand-paper-bright border border-black/70 p-6 md:p-8"
    >
      <p className="text-brand-signal-deep font-mono text-xs font-semibold tracking-[0.14em] uppercase">
        Shared ledgers in practice
      </p>
      <h2 className="mt-2 text-2xl font-bold tracking-[-0.02em]">
        Building a shared workspace or multi-party ledger?
      </h2>
      <p className="text-muted-foreground mt-2 max-w-2xl text-base leading-relaxed">
        Start a shared problem space with split work and decision logs, or trace
        how automated workflow steps execute safely in AZOA.
      </p>
      <div className="mt-5 flex flex-wrap gap-3">
        <Link
          className="bg-brand-ink text-brand-paper hover:bg-brand-ink-hover inline-flex min-h-11 items-center gap-2 px-5 py-2.5 text-sm font-semibold transition-colors"
          href="/auth/signin?mode=signup&callbackUrl=%2Fdashboard"
        >
          Open a shared workspace
          <ArrowRight aria-hidden="true" className="size-4" />
        </Link>
        {showPatternCta ? (
          <Link
            className="bg-brand-paper text-foreground hover:bg-brand-paper-bright inline-flex min-h-11 items-center border border-black/70 px-5 py-2.5 text-sm font-semibold transition-colors"
            href="/patterns"
          >
            Explore the full pattern library
          </Link>
        ) : null}
        {showNotesCta ? (
          <Link
            className="bg-brand-paper text-foreground hover:bg-brand-paper-bright inline-flex min-h-11 items-center border border-black/70 px-5 py-2.5 text-sm font-semibold transition-colors"
            href="/notes"
          >
            Read engineering notes
          </Link>
        ) : null}
        <a
          className="bg-brand-paper text-foreground hover:bg-brand-paper-bright inline-flex min-h-11 items-center border border-black/70 px-5 py-2.5 text-sm font-semibold transition-colors"
          href="https://azoa.ardanova.com/workflows"
        >
          Inspect workflow execution on AZOA
        </a>
      </div>
    </section>
  );
}

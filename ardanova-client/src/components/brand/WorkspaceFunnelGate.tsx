import Link from "next/link";
import { ArrowRight } from "lucide-react";

/**
 * Clean bottom-of-page funnel gate moving readers from solutionary essays or
 * stewardship patterns into starting a shared project, reading sibling notes,
 * or inspecting companion workflow safeguards on AZOA.
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
      aria-label="Next steps for community stewards"
      className="bg-brand-paper-bright border border-black/70 p-6 md:p-8"
    >
      <p className="text-brand-signal-deep font-mono text-xs font-semibold tracking-[0.14em] uppercase">
        Solutionary stewardship
      </p>
      <h2 className="mt-2 text-2xl font-bold tracking-[-0.02em]">
        Care about a problem worth solving together?
      </h2>
      <p className="text-muted-foreground mt-2 max-w-2xl text-base leading-relaxed">
        Bring collaborators, skilled guilds, and community sponsors into a
        transparent workspace where stewardship shares and democratic voice stay
        legible—or inspect how workflow safeguards execute on AZOA.
      </p>
      <div className="mt-5 flex flex-wrap gap-3">
        <Link
          className="bg-brand-ink text-brand-paper hover:bg-brand-ink-hover inline-flex min-h-11 items-center gap-2 px-5 py-2.5 text-sm font-semibold transition-colors"
          href="/auth/signin?mode=signup&callbackUrl=%2Fdashboard"
        >
          Start a shared project
          <ArrowRight aria-hidden="true" className="size-4" />
        </Link>
        {showPatternCta ? (
          <Link
            className="bg-brand-paper text-foreground hover:bg-brand-paper-bright inline-flex min-h-11 items-center border border-black/70 px-5 py-2.5 text-sm font-semibold transition-colors"
            href="/patterns"
          >
            Browse stewardship patterns
          </Link>
        ) : null}
        {showNotesCta ? (
          <Link
            className="bg-brand-paper text-foreground hover:bg-brand-paper-bright inline-flex min-h-11 items-center border border-black/70 px-5 py-2.5 text-sm font-semibold transition-colors"
            href="/notes"
          >
            Read the essays
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

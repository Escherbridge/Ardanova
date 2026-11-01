import Link from "next/link";
import { Plus } from "lucide-react";

import { Button } from "~/components/ui/button";
import { feedRoutes } from "~/lib/feed/routes";
import { sectorTag } from "~/lib/feed/stream";
import { categoryLabel } from "~/lib/taxonomy/labels";

/**
 * Shown where `nextCursor` is null: sector chips (only sectors seen on loaded cards, passed in) and
 * "Start a project". With no cards at all it is one line plus the same action.
 */
export function ColdStartRow({
  sectors,
  isEmpty = false,
}: {
  /** Sector codes with content, e.g. from `sectorsSeen(cards)`. */
  sectors: readonly string[];
  isEmpty?: boolean;
}) {
  if (isEmpty) {
    return (
      <div className="border-border flex flex-col items-center gap-4 border border-dashed px-5 py-10 text-center">
        <p className="text-muted-foreground text-sm">Nothing posted yet.</p>
        <Button asChild>
          <Link href={feedRoutes.startProject}>Start a project</Link>
        </Button>
      </div>
    );
  }

  return (
    // The double rule closes the stream the way a ledger closes its books.
    <div className="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-t-[3px] border-double pt-4">
      {sectors.length > 0 ? (
        <nav
          aria-label="Sectors"
          className="flex min-w-0 flex-wrap items-center gap-2"
        >
          <span
            aria-hidden
            className="text-muted-foreground font-mono text-[11px] font-semibold tracking-[0.08em] uppercase"
          >
            Sectors
          </span>
          <ul className="flex flex-wrap gap-1.5">
            {sectors.map((sector) => (
              <li key={sector}>
                <Link
                  href={feedRoutes.tag(sectorTag(sector))}
                  className="border-border bg-card hover:border-foreground inline-flex min-h-8 items-center border px-2.5 text-xs font-medium transition-colors"
                >
                  {categoryLabel(sector)}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      ) : null}
      <Button asChild variant="outline" size="sm" className="ml-auto">
        <Link href={feedRoutes.startProject}>
          <Plus aria-hidden />
          Start a project
        </Link>
      </Button>
    </div>
  );
}

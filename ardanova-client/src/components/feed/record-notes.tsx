import { cn } from "~/lib/utils";

/** Rights and consent notes for the proposals, events and funding a stream carries; kept verbatim. See ./AGENTS.md §Record notes. */
export function RecordNotes({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "border-border text-muted-foreground space-y-3 border-t-[3px] border-double pt-3 text-xs leading-relaxed",
        className,
      )}
    >
      <p>
        Proposals remain proposals until the recorded process resolves them.
        Events are shown as scheduled, not as attendance or consent.
      </p>
      <p>
        Participation is not proof of ownership. Governance credentials, project
        utility, and economic rights remain separate, reviewable records.
      </p>
    </div>
  );
}

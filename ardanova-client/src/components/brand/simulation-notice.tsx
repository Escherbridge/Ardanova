import Link from "next/link";

/**
 * Quiet footer sandbox and legal notice placed beneath editorial content
 * rather than leading ahead of the engineering narrative.
 */
export function SimulationNotice({
  includeAzoa = false,
  includeLegalAdvice = false,
}: {
  includeAzoa?: boolean;
  includeLegalAdvice?: boolean;
}) {
  return (
    <aside
      aria-label="Sandbox and legal notice"
      className="text-muted-foreground border-t border-black/30 pt-5 text-sm leading-relaxed"
    >
      <p>
        <span className="text-foreground font-mono text-xs font-semibold tracking-[0.12em] uppercase">
          Sandbox / Demo Environment (ECONOMY: SIMULATED)
        </span>{" "}
        — Platform records inside the hosted ArdaNova workspace do not
        automatically create legal ownership, equity, or payment rights.
        {includeLegalAdvice
          ? " Pattern guides and statutory examples are engineering references, not legal advice."
          : null}{" "}
        See{" "}
        <Link className="underline underline-offset-4" href="/terms">
          Terms of use
        </Link>{" "}
        for governing Idaho terms.
        {includeAzoa ? (
          <>
            {" "}
            Companion workflow engine{" "}
            <a
              className="underline underline-offset-4"
              href="https://azoa.ardanova.com/workflows"
            >
              AZOA
            </a>{" "}
            likewise demonstrates simulated workflow payloads in its hosted
            demo.
          </>
        ) : null}
      </p>
    </aside>
  );
}

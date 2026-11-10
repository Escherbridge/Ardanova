import Link from "next/link";

import { EscherbridgeProjects } from "~/components/brand/escherbridge-projects";
import { ArdaNovaWordmark } from "~/components/brand/wordmark";

/** Public-site footer. Legal links stay here so terms and privacy are reachable from every public page. */
export function SiteFooter({ workspaceHref }: { workspaceHref: string }) {
  return (
    <footer className="bg-brand-ink text-brand-paper border-t border-black/70">
      <div className="mx-auto grid max-w-[1440px] gap-10 px-4 py-10 md:grid-cols-[1fr_auto] md:px-8">
        <div>
          <ArdaNovaWordmark className="text-lg" />
        </div>
        <nav
          aria-label="Footer navigation"
          className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm md:grid-cols-3"
        >
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/projects"
          >
            Projects
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/opportunities"
          >
            Opportunities
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/governance"
          >
            Governance
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/#method"
          >
            Method
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href={workspaceHref}
          >
            Workspace
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/terms"
          >
            Terms of use
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/privacy"
          >
            Privacy notice
          </Link>
          <Link
            className="hover:text-brand-cyan inline-flex min-h-11 items-center"
            href="/notes"
          >
            Notes
          </Link>
        </nav>
      </div>
      <div className="border-brand-paper/25 mx-auto max-w-[1440px] border-t px-4 py-6 md:px-8">
        <EscherbridgeProjects linkClassName="hover:text-brand-cyan" />
      </div>
      <div className="border-brand-paper/25 text-brand-stone border-t px-4 py-4 text-center font-mono text-[10px] tracking-[0.14em] uppercase">
        <p>© {new Date().getFullYear()} ArdaNova</p>
        <p className="mt-2 tracking-normal normal-case">
          Operated by Escherbridge, LLC, Boise, Idaho.
        </p>
      </div>
    </footer>
  );
}

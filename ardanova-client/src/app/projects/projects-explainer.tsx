import Link from "next/link";

import { SiteFooter } from "~/components/brand/site-footer";
import { ArdaNovaWordmark } from "~/components/brand/wordmark";
import { Button } from "~/components/ui/button";
import { buildSignInHref } from "~/lib/auth-navigation";

const PROJECTS_DESCRIPTION =
  "Projects, opportunities and governance records are visible after you sign in.";

const PROJECTS_RECORD =
  "Each project starts with a problem, then a proposed solution others can test, then iterations in public. The work, and who did it, goes in one record, and the community's say goes in another.";

const SIMULATED_ECONOMY =
  "The economy on the deployed service is simulated. A record is not a promise of payment.";

export { PROJECTS_DESCRIPTION, PROJECTS_RECORD, SIMULATED_ECONOMY };

/** Public index for a signed-out visitor. The workspace list stays behind sign-in. */
export function ProjectsExplainer() {
  return (
    <div className="public-shell bg-background text-foreground flex min-h-screen flex-col">
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>
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
        <h1 className="mb-6 max-w-3xl">Projects</h1>
        <div className="max-w-2xl space-y-4 text-lg leading-relaxed">
          <p>{PROJECTS_DESCRIPTION}</p>
          <p>{PROJECTS_RECORD}</p>
          <p className="text-muted-foreground text-base">{SIMULATED_ECONOMY}</p>
        </div>
        <div className="mt-8 flex flex-wrap gap-3">
          <Button asChild>
            <Link href={buildSignInHref("/projects", { mode: "signup" })}>
              Create an account with Google
            </Link>
          </Button>
          <Button asChild variant="outline">
            <Link href={buildSignInHref("/projects")}>Sign in</Link>
          </Button>
        </div>
        <p className="mt-6">
          <Link className="underline underline-offset-4" href="/patterns">
            Read the patterns first
          </Link>
        </p>
      </main>
      <SiteFooter
        governanceHref="/projects"
        opportunitiesHref="/projects"
        projectsHref="/projects"
        workspaceHref="/dashboard"
      />
    </div>
  );
}

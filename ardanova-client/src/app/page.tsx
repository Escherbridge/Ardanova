import { ArrowRight, Check, FileText, Handshake, Scale } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";

import { SiteFooter } from "~/components/brand/site-footer";
import { JsonLd } from "~/components/brand/json-ld";
import { ArdaNovaWordmark } from "~/components/brand/wordmark";
import { SignalCanvas } from "~/components/brand/signal-canvas";
import { HomePrimaryNavMenu } from "~/components/marketing/home-primary-nav";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { HOME_DESCRIPTION, HOME_TITLE } from "~/lib/public-copy";
import { itemList } from "~/lib/schema";
import { publicCanonical, shareImage } from "~/lib/site";
import { authForPage } from "~/server/auth";
import { HydrateClient } from "~/trpc/server";

const homeCanonical = "https://ardanova.com/";

export const metadata: Metadata = {
  title: { absolute: HOME_TITLE },
  description: HOME_DESCRIPTION,
  robots: { index: true, follow: true },
  alternates: {
    canonical: homeCanonical,
    types: {
      "application/rss+xml": publicCanonical("/feed.xml"),
    },
  },
  openGraph: {
    type: "website",
    url: homeCanonical,
    title: HOME_TITLE,
    description: HOME_DESCRIPTION,
    siteName: "ArdaNova",
    locale: "en_US",
    images: [shareImage],
  },
  twitter: {
    card: "summary_large_image",
    title: HOME_TITLE,
    description: HOME_DESCRIPTION,
    images: [
      {
        url: shareImage.url,
        alt: shareImage.alt,
        width: shareImage.width,
        height: shareImage.height,
      },
    ],
  },
};

const operatingSteps = [
  {
    number: "01",
    title: "Discover a problem",
    description:
      "Begin in lived social and ecological context—observing what breaks for the people affected instead of chasing outrage headlines.",
    icon: <FileText aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
  {
    number: "02",
    title: "Define a solution",
    description:
      "Shape a root-cause proposal others can test openly: who participates, what downstream harm gets avoided, and on what shared terms.",
    icon: <Scale aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
  {
    number: "03",
    title: "Iterate",
    description:
      "Build, compare results in public with the community, and adjust or stop honestly when evidence calls for it.",
    icon: <Handshake aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
];

const trustCues = [
  {
    title: "Ownership without surveillance",
    description:
      "Only collect and share the project context participants explicitly agree to—never profiling attention or turning contributors into data.",
  },
  {
    title: "Explicit, documented rights",
    description:
      "Shared ownership rests on clear, readable terms: what work or funds contributed, what rights attach, and where those terms live.",
  },
  {
    title: "Accountable community memory",
    description:
      "Proposals, dissenting questions, amendments, and outcomes remain legible so stewards joining next year inherit clarity, not guesswork.",
  },
];

export default async function Home() {
  const session = await authForPage();
  const workspaceHref = session ? "/dashboard" : "/auth/signin";
  const signupHref = session
    ? "/dashboard"
    : "/auth/signin?mode=signup&callbackUrl=%2Fdashboard";
  const projectsHref = session
    ? "/projects"
    : "/auth/signin?callbackUrl=%2Fprojects";

  return (
    <HydrateClient>
      <JsonLd
        data={itemList("ArdaNova", [
          publicCanonical("/notes"),
          publicCanonical("/patterns"),
          publicCanonical("/terms"),
          publicCanonical("/privacy"),
          publicCanonical("/contact"),
        ])}
      />
      <div className="public-shell bg-background text-foreground min-h-screen">
        <a className="skip-link" href="#main-content">
          Skip to main content
        </a>

        <header className="bg-brand-paper sticky top-0 z-50 border-b border-black/70">
          <div className="mx-auto flex min-h-16 max-w-[1440px] items-center justify-between gap-6 px-4 md:px-8">
            <Link
              aria-label="ArdaNova home"
              className="inline-flex min-h-11 items-center"
              href="/"
            >
              <ArdaNovaWordmark className="text-base md:text-lg" />
            </Link>

            <nav
              aria-label="Primary navigation"
              className="hidden items-center gap-7 text-sm font-semibold md:flex"
            >
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href="#method"
              >
                Method
              </Link>
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href="#rights"
              >
                Rights
              </Link>
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href={projectsHref}
              >
                Projects
              </Link>
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href="/notes"
              >
                Notes
              </Link>
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href="/patterns"
              >
                Patterns
              </Link>
            </nav>

            <div className="flex items-center gap-2">
              <HomePrimaryNavMenu projectsHref={projectsHref} />
              {!session && (
                <Button asChild className="hidden sm:inline-flex" size="sm">
                  <Link href={signupHref}>Create account</Link>
                </Button>
              )}
              <Button asChild size="sm" variant="outline">
                <Link href={workspaceHref}>
                  {session ? "Open workspace" : "Sign in"}
                  <ArrowRight aria-hidden="true" />
                </Link>
              </Button>
            </div>
          </div>
        </header>

        <main id="main-content" tabIndex={-1}>
          <section className="border-b border-black/70">
            <div className="mx-auto grid max-w-[1440px] lg:grid-cols-[minmax(0,1.25fr)_minmax(360px,0.75fr)]">
              <div className="flex flex-col justify-between gap-14 px-4 py-16 md:px-8 md:py-24 lg:min-h-[calc(100svh-4rem)] lg:gap-4 lg:border-r lg:border-black/70 lg:py-8">
                <div className="min-w-0">
                  <h1 className="display-type max-w-5xl uppercase">
                    Turn attention.{" "}
                    <span className="text-brand-signal-hot block">Into </span>
                    action.
                  </h1>
                </div>

                <div className="max-w-[34rem] border-t border-black/70 pt-6 lg:pt-4">
                  <div>
                    <p className="text-brand-signal-deep font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                      Solutionary Action · Shared Ownership · Social Ecology
                    </p>
                    <p className="mt-4 text-lg leading-relaxed md:text-xl">
                      Not an endless feed. ArdaNova is where people who care
                      about a real-world problem bring together collaborators,
                      guilds, and community sponsors to fund it, build it,
                      govern it together, and share in what it becomes.
                    </p>
                    <p className="text-muted-foreground mt-4 text-base leading-relaxed">
                      The economy on the deployed service is simulated. A record
                      is not a promise of payment.
                    </p>
                    <p className="mt-4 flex flex-wrap gap-x-4 gap-y-1 text-sm leading-relaxed">
                      <Link
                        className="underline underline-offset-4"
                        href="/notes"
                      >
                        Read essays on ownership, solutionary thinking &amp;
                        social ecology
                      </Link>
                      <span aria-hidden="true">·</span>
                      <Link
                        className="underline underline-offset-4"
                        href="/patterns"
                      >
                        Stewardship &amp; governance patterns
                      </Link>
                      <span aria-hidden="true">·</span>
                      <a
                        className="underline underline-offset-4"
                        href="https://primusneo.com"
                      >
                        Part of the PrimusNeo stack
                      </a>
                    </p>
                  </div>
                  <div className="mt-8 flex flex-wrap gap-3">
                    <Button asChild size="lg">
                      <Link href={session ? workspaceHref : signupHref}>
                        {session ? "Continue the work" : "Describe a problem"}
                        <ArrowRight aria-hidden="true" />
                      </Link>
                    </Button>
                    <Button asChild size="lg" variant="outline">
                      <Link href={projectsHref}>Discover projects</Link>
                    </Button>
                  </div>
                </div>
              </div>

              <div className="bg-brand-signal p-4 md:p-8 lg:flex lg:items-center">
                <SignalCanvas />
              </div>
            </div>
          </section>

          <section className="bg-brand-ink text-brand-paper border-b border-black/70">
            <div className="divide-brand-paper/35 border-brand-paper/35 mx-auto grid max-w-[1440px] divide-y border-x-0 md:grid-cols-4 md:divide-x md:divide-y-0 lg:border-x">
              <div className="p-5 font-mono text-xs tracking-[0.16em] uppercase">
                The solutionary loop
              </div>
              {["Discover", "Define", "Iterate"].map((label, index) => (
                <div
                  className="flex items-center justify-between p-5 text-sm font-bold uppercase"
                  key={label}
                >
                  <span>{label}</span>
                  <span className="text-brand-cyan font-mono">
                    0{index + 1}
                  </span>
                </div>
              ))}
            </div>
          </section>

          <section className="border-b border-black/70" id="method">
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-14">
                <div>
                  <p className="text-brand-signal-deep mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    01 / Method
                  </p>
                  <h2 className="max-w-3xl text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                    Start where the problem is.
                  </h2>
                </div>
              </div>

              <div className="grid border-t border-l border-black/70 md:grid-cols-3">
                {operatingSteps.map((step) => {
                  return (
                    <Card
                      className="min-h-[340px] justify-between border-t-0 border-l-0 border-black/70 bg-transparent"
                      key={step.number}
                      padding="none"
                    >
                      <CardContent className="flex h-full flex-col justify-between gap-12 p-6 md:p-8">
                        <div className="flex items-start justify-between">
                          <span className="font-mono text-xs font-semibold tracking-[0.16em]">
                            {step.number}
                          </span>
                          {step.icon}
                        </div>
                        <div>
                          <h3 className="mb-4 text-3xl font-black tracking-[-0.04em] uppercase">
                            {step.title}
                          </h3>
                          <p className="text-muted-foreground max-w-sm leading-relaxed">
                            {step.description}
                          </p>
                        </div>
                      </CardContent>
                    </Card>
                  );
                })}
              </div>
            </div>
          </section>

          <section
            className="bg-brand-paper-bright border-b border-black/70"
            id="rights"
          >
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-14 max-w-4xl">
                <p className="text-brand-signal-deep mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                  02 / Shared Ownership &amp; Voice
                </p>
                <h2 className="text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                  Two records. One agreement.
                </h2>
              </div>

              <div className="grid border border-black/70 lg:grid-cols-2">
                <article className="bg-brand-signal text-brand-ink flex min-h-[360px] flex-col justify-between p-7 md:p-10 lg:border-r lg:border-black/70">
                  <div className="flex items-center justify-between border-b border-black/70 pb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    <span>Record A</span>
                    <span>Stewardship + shared value</span>
                  </div>
                  <div>
                    <h3 className="mb-5 max-w-lg text-4xl leading-none font-black tracking-[-0.05em] uppercase md:text-5xl">
                      Stewardship, Work &amp; Shared Value
                    </h3>
                    <p className="max-w-xl text-lg leading-relaxed">
                      Track time contributed, material support, and agreed value
                      shares cleanly so builders and community sponsors can
                      share honestly in outcomes under explicit, documented
                      terms.
                    </p>
                  </div>
                </article>

                <article className="bg-brand-ink text-brand-paper flex min-h-[360px] flex-col justify-between p-7 md:p-10">
                  <div className="border-brand-paper/50 flex items-center justify-between border-b pb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    <span>Record B</span>
                    <span className="text-brand-cyan">1 member = 1 voice</span>
                  </div>
                  <div>
                    <h3 className="mb-5 max-w-lg text-4xl leading-none font-black tracking-[-0.05em] uppercase md:text-5xl">
                      Community Voice &amp; Governance
                    </h3>
                    <p className="text-brand-stone-light max-w-xl text-lg leading-relaxed">
                      Keep proposals, delegated stewardship, and democratic
                      votes independent of money contributed—so deep pockets
                      never quietly purchase control over a commons.
                    </p>
                  </div>
                </article>
              </div>

              <div className="grid gap-5 border-x border-b border-black/70 p-6 md:grid-cols-[auto_1fr] md:items-center md:p-8">
                <Badge variant="warning">Sandbox &amp; legal boundary</Badge>
                <div className="text-muted-foreground max-w-4xl space-y-3 text-sm leading-relaxed">
                  <p>
                    <strong className="text-foreground font-mono">
                      Sandbox / Demo Environment (ECONOMY: SIMULATED):
                    </strong>{" "}
                    Balances, unit counts, and funding records are bookkeeping
                    inside the workspace, not a payment and not a transfer of
                    property. Platform records do not automatically create legal
                    ownership, equity, or payment rights—project agreements,
                    participant review, and governing Idaho law determine their
                    effect. A recorded vote does not, by itself, move funds,
                    change who is a member, or rewrite a project&apos;s terms.
                  </p>
                  <p className="flex flex-wrap gap-x-4 gap-y-1">
                    <Link
                      className="underline underline-offset-4"
                      href="/patterns/a-record-is-not-a-right"
                    >
                      Pattern: A record is not a right
                    </Link>
                    <a
                      className="underline underline-offset-4"
                      href="https://azoa.ardanova.com/workflows"
                    >
                      Inspect workflow execution on AZOA
                    </a>
                  </p>
                </div>
              </div>
            </div>
          </section>

          <section className="border-b border-black/70">
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-12">
                <div>
                  <p className="text-brand-signal-deep mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    03 / Trust
                  </p>
                  <h2 className="text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                    Make power legible.
                  </h2>
                </div>
              </div>

              <div className="grid border-t border-l border-black/70 md:grid-cols-3">
                {trustCues.map((cue, index) => (
                  <article
                    className="min-h-[250px] border-r border-b border-black/70 p-6 md:p-8"
                    key={cue.title}
                  >
                    <div className="mb-12 flex items-center justify-between">
                      <Check
                        aria-hidden="true"
                        className="text-primary size-6"
                      />
                      <span className="font-mono text-xs">0{index + 1}</span>
                    </div>
                    <h3 className="mb-3 text-2xl font-black tracking-[-0.035em] uppercase">
                      {cue.title}
                    </h3>
                    <p className="text-muted-foreground leading-relaxed">
                      {cue.description}
                    </p>
                  </article>
                ))}
              </div>
            </div>
          </section>

          <section className="bg-brand-signal text-brand-ink">
            <div className="mx-auto grid max-w-[1440px] gap-10 px-4 py-20 md:px-8 md:py-24 lg:grid-cols-[1fr_auto] lg:items-end">
              <h2 className="max-w-5xl text-4xl leading-[0.88] font-black tracking-[-0.06em] break-words uppercase sm:text-5xl md:text-8xl">
                Problems are not in short supply.
              </h2>
              <div className="flex flex-wrap gap-3">
                <Button
                  asChild
                  className="border-brand-ink bg-brand-ink text-brand-paper hover:bg-brand-ink-hover"
                  size="xl"
                >
                  <Link href={session ? workspaceHref : signupHref}>
                    {session ? "Continue the work" : "Start with a problem"}
                    <ArrowRight aria-hidden="true" />
                  </Link>
                </Button>
                <Button
                  asChild
                  className="border-brand-ink bg-brand-paper text-brand-ink hover:bg-brand-paper-bright"
                  size="xl"
                  variant="outline"
                >
                  <Link href="/notes">Read the essays</Link>
                </Button>
              </div>
            </div>
          </section>
        </main>

        <SiteFooter workspaceHref={workspaceHref} />
      </div>
    </HydrateClient>
  );
}

import {
  ArrowRight,
  Bot,
  Check,
  FileCheck2,
  FileText,
  Handshake,
  Presentation,
  Scale,
} from "lucide-react";
import Link from "next/link";

import { ArdaNovaWordmark } from "~/components/brand/wordmark";
import { SignalCanvas } from "~/components/brand/signal-canvas";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { authForPage } from "~/server/auth";
import { HydrateClient } from "~/trpc/server";

const operatingSteps = [
  {
    number: "01",
    title: "Discover a problem",
    description:
      "Begin with affected people, lived context, evidence, constraints, and open questions—not whatever happens to be trending.",
    icon: <FileText aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
  {
    number: "02",
    title: "Define a solution",
    description:
      "Shape a reviewable proposal with visible assumptions, roles, milestones, terms, rights, and measures of progress.",
    icon: <Scale aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
  {
    number: "03",
    title: "Iterate",
    description:
      "Contribute, test, compare what happened with what was intended, and revise—or stop—with the people affected.",
    icon: <Handshake aria-hidden="true" className="size-8" strokeWidth={1.5} />,
  },
];

const trustCues = [
  {
    title: "Context stays scoped",
    description:
      "Show what information is being used, why it is needed, what is excluded, and who can act on it.",
  },
  {
    title: "Rights stay explicit",
    description:
      "Ownership and decision authority come from documented terms, never from activity, membership, or a token alone.",
  },
  {
    title: "Progress over engagement",
    description:
      "Surface open questions, contributions, decisions, and next moves—not popularity or time spent scrolling.",
  },
];

const novaArtifacts = [
  {
    icon: <FileCheck2 aria-hidden="true" className="size-4" />,
    title: "Project brief",
    note: "2 questions flagged",
  },
  {
    icon: <Scale aria-hidden="true" className="size-4" />,
    title: "Rights comparison",
    note: "1 conflict to reconcile",
  },
  {
    icon: <Presentation aria-hidden="true" className="size-4" />,
    title: "Presentation outline",
    note: "8 proposed frames",
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
      <div className="public-shell bg-background text-foreground min-h-screen">
        <a className="skip-link" href="#main-content">
          Skip to main content
        </a>

        <header className="sticky top-0 z-50 border-b border-black/70 bg-[#f4efeb]">
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
                href="#nova"
              >
                Nova AI
              </Link>
              <Link
                className="hover:text-primary inline-flex min-h-11 min-w-11 items-center justify-center"
                href={projectsHref}
              >
                Projects
              </Link>
            </nav>

            <div className="flex items-center gap-2">
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
              <div className="flex flex-col justify-between gap-14 px-4 py-16 md:px-8 md:py-24 lg:min-h-[calc(100svh-4rem)] lg:border-r lg:border-black/70 lg:py-16">
                <div className="flex items-center justify-between gap-4 border-t border-black/70 pt-3 font-mono text-[10px] font-semibold tracking-[0.16em] uppercase">
                  <span>Social layer for doing</span>
                  <span className="text-[#b8322a]">Human review / always</span>
                </div>

                <div className="min-w-0">
                  <Badge className="mb-7" variant="outline">
                    Social media for doing—not doom-scrolling
                  </Badge>
                  <h1 className="display-type max-w-5xl uppercase">
                    Turn attention.
                    <span className="block text-[#cf392f]">Into</span>
                    action.
                  </h1>
                </div>

                <div className="grid gap-8 border-t border-black/70 pt-6 md:grid-cols-[minmax(0,1fr)_auto] md:items-end">
                  <p className="max-w-2xl text-lg leading-relaxed md:text-xl">
                    Not an endless feed. ArdaNova is a shared workspace where
                    people discover a problem, define a solution, and
                    iterate—with decisions, contributions, and documented rights
                    visible for review.
                  </p>
                  <div className="flex flex-wrap gap-3">
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

              <div className="bg-[#ef4638] p-4 md:p-8 lg:flex lg:items-center">
                <SignalCanvas />
              </div>
            </div>
          </section>

          <section className="border-b border-black/70 bg-[#151513] text-[#f4efeb]">
            <div className="mx-auto grid max-w-[1440px] divide-y divide-[#f4efeb]/35 border-x-0 border-[#f4efeb]/35 md:grid-cols-4 md:divide-x md:divide-y-0 lg:border-x">
              <div className="p-5 font-mono text-xs tracking-[0.16em] uppercase">
                The solutionary loop
              </div>
              {["Discover", "Define", "Iterate"].map((label, index) => (
                <div
                  className="flex items-center justify-between p-5 text-sm font-bold uppercase"
                  key={label}
                >
                  <span>{label}</span>
                  <span className="font-mono text-[#70d7e2]">0{index + 1}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="border-b border-black/70" id="method">
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-14 grid gap-8 md:grid-cols-2 md:items-end">
                <div>
                  <p className="text-primary mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    01 / The solutionary loop
                  </p>
                  <h2 className="max-w-3xl text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                    Solutionary is revolutionary.
                  </h2>
                </div>
                <p className="text-muted-foreground max-w-xl text-lg leading-relaxed md:justify-self-end">
                  Start with lived context, shape a proposal people can inspect,
                  and keep learning with those affected. Every iteration can
                  move forward, return to discovery, or end in a documented
                  stop.
                </p>
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
            className="border-b border-black/70 bg-[#fffdfa]"
            id="rights"
          >
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-14 max-w-4xl">
                <p className="mb-4 font-mono text-xs font-semibold tracking-[0.16em] text-[#b8322a] uppercase">
                  02 / Rights without hand-waving
                </p>
                <h2 className="text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                  Two records. One understandable agreement.
                </h2>
              </div>

              <div className="grid border border-black/70 lg:grid-cols-2">
                <article className="flex min-h-[360px] flex-col justify-between bg-[#ef4638] p-7 text-[#151513] md:p-10 lg:border-r lg:border-black/70">
                  <div className="flex items-center justify-between border-b border-black/70 pb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    <span>Record A</span>
                    <span>Work + value</span>
                  </div>
                  <div>
                    <h3 className="mb-5 max-w-lg text-4xl leading-none font-black tracking-[-0.05em] uppercase md:text-5xl">
                      Contribution and economic interests
                    </h3>
                    <p className="max-w-xl text-lg leading-relaxed">
                      Keep the work performed, agreed value, allocation logic,
                      and reconciliation status visible to the people affected.
                    </p>
                  </div>
                </article>

                <article className="flex min-h-[360px] flex-col justify-between bg-[#151513] p-7 text-[#f4efeb] md:p-10">
                  <div className="flex items-center justify-between border-b border-[#f4efeb]/50 pb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    <span>Record B</span>
                    <span className="text-[#70d7e2]">Voice + permissions</span>
                  </div>
                  <div>
                    <h3 className="mb-5 max-w-lg text-4xl leading-none font-black tracking-[-0.05em] uppercase md:text-5xl">
                      Governance and decision rights
                    </h3>
                    <p className="max-w-xl text-lg leading-relaxed text-[#d6d0ca]">
                      Keep proposals, voting or approval rules, delegated roles,
                      and recorded outcomes distinct from economic interests.
                    </p>
                  </div>
                </article>
              </div>

              <div className="grid gap-5 border-x border-b border-black/70 p-6 md:grid-cols-[auto_1fr] md:items-center md:p-8">
                <Badge variant="warning">Important distinction</Badge>
                <p className="text-muted-foreground max-w-4xl text-sm leading-relaxed">
                  Platform records do not automatically create legal ownership,
                  equity, or payment rights. Project agreements, participant
                  review, and applicable law determine their effect.
                </p>
              </div>
            </div>
          </section>

          <section
            className="border-b border-black/70 bg-[#151513] text-[#f4efeb]"
            id="nova"
          >
            <div className="mx-auto grid max-w-[1440px] lg:grid-cols-2">
              <div className="flex flex-col justify-between gap-14 border-[#f4efeb]/35 p-6 md:p-12 lg:min-h-[760px] lg:border-r">
                <div>
                  <div className="mb-8 flex items-center gap-3 font-mono text-xs font-semibold tracking-[0.16em] text-[#70d7e2] uppercase">
                    <Bot aria-hidden="true" className="size-5" />
                    Nova / optional assistive layer
                  </div>
                  <h2 className="max-w-3xl text-4xl leading-[0.9] font-black tracking-[-0.06em] break-words uppercase sm:text-5xl md:text-7xl">
                    An assistant that returns work to people.
                  </h2>
                </div>

                <div className="max-w-xl">
                  <p className="mb-8 text-lg leading-relaxed text-[#d6d0ca]">
                    Nova is designed to help shape project briefs, compare
                    proposals, prepare presentations, and surface reconciliation
                    questions. It can accelerate the draft—not become the
                    authority.
                  </p>
                  <div className="border-l-4 border-[#ef4638] pl-5 text-xl leading-snug font-bold">
                    Nothing publishes, signs, or settles without deliberate
                    human review.
                  </div>
                </div>
              </div>

              <div className="bg-[#252522] p-4 md:p-8 lg:flex lg:items-center">
                <div className="w-full border border-[#f4efeb]/45 bg-[#151513]">
                  <div className="flex items-center justify-between border-b border-[#f4efeb]/45 px-4 py-3 font-mono text-[10px] font-semibold tracking-[0.16em] uppercase">
                    <span>Illustrative Nova workflow</span>
                    <span className="text-[#70d7e2]">Draft state</span>
                  </div>

                  <div className="space-y-4 p-4 md:p-6">
                    <div className="border border-[#f4efeb]/35 bg-[#252522] p-4">
                      <p className="mb-2 font-mono text-[10px] tracking-[0.14em] text-[#aaa59f] uppercase">
                        Human request
                      </p>
                      <p className="leading-relaxed">
                        Turn our working notes into a project brief and a
                        presentation outline. Flag unresolved rights and payment
                        questions.
                      </p>
                    </div>

                    <div className="border border-[#70d7e2] bg-[#1c2d2e] p-4 md:p-5">
                      <div className="mb-5 flex items-center justify-between gap-4">
                        <p className="font-mono text-[10px] tracking-[0.14em] text-[#70d7e2] uppercase">
                          Nova prepared 3 artifacts
                        </p>
                        <Badge
                          className="border-[#ef4638] text-[#ff8b80]"
                          variant="outline"
                        >
                          Review required
                        </Badge>
                      </div>
                      <ul className="space-y-3" role="list">
                        {novaArtifacts.map((artifact) => {
                          return (
                            <li
                              className="grid grid-cols-[auto_1fr_auto] items-center gap-3 border-t border-[#f4efeb]/20 pt-3"
                              key={artifact.title}
                            >
                              {artifact.icon}
                              <span className="text-sm font-semibold">
                                {artifact.title}
                              </span>
                              <span className="text-right font-mono text-[9px] tracking-[0.08em] text-[#aaa59f] uppercase">
                                {artifact.note}
                              </span>
                            </li>
                          );
                        })}
                      </ul>
                    </div>

                    <div className="grid grid-cols-2 border border-[#f4efeb]/35 text-center text-xs font-bold uppercase">
                      <span className="border-r border-[#f4efeb]/35 p-3">
                        Compare changes
                      </span>
                      <span className="bg-[#f4efeb] p-3 text-[#151513]">
                        Approve or revise
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </section>

          <section className="border-b border-black/70">
            <div className="mx-auto max-w-[1440px] px-4 py-20 md:px-8 md:py-28">
              <div className="mb-12 grid gap-6 md:grid-cols-2 md:items-end">
                <div>
                  <p className="text-primary mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                    03 / Technology for ownership, not surveillance
                  </p>
                  <h2 className="text-4xl leading-[0.92] font-black tracking-[-0.055em] break-words uppercase sm:text-5xl md:text-7xl">
                    Make power legible.
                  </h2>
                </div>
                <p className="text-muted-foreground max-w-lg text-lg leading-relaxed md:justify-self-end">
                  A humane system explains what it uses and who can act. It
                  supports explicit, documented rights without treating human
                  activity as raw material for attention extraction.
                </p>
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

          <section className="bg-[#ef4638] text-[#151513]">
            <div className="mx-auto grid max-w-[1440px] gap-10 px-4 py-20 md:px-8 md:py-24 lg:grid-cols-[1fr_auto] lg:items-end">
              <div>
                <p className="mb-4 font-mono text-xs font-semibold tracking-[0.16em] uppercase">
                  Discover. Define. Iterate.
                </p>
                <h2 className="max-w-5xl text-4xl leading-[0.88] font-black tracking-[-0.06em] break-words uppercase sm:text-5xl md:text-8xl">
                  Give the next iteration a place to happen.
                </h2>
              </div>
              <Button
                asChild
                className="border-[#151513] bg-[#151513] text-[#f4efeb] hover:bg-[#2b2b27]"
                size="xl"
              >
                <Link href={session ? workspaceHref : signupHref}>
                  {session ? "Continue the work" : "Start with a problem"}
                  <ArrowRight aria-hidden="true" />
                </Link>
              </Button>
            </div>
          </section>
        </main>

        <footer className="border-t border-black/70 bg-[#151513] text-[#f4efeb]">
          <div className="mx-auto grid max-w-[1440px] gap-10 px-4 py-10 md:grid-cols-[1fr_auto] md:px-8">
            <div>
              <ArdaNovaWordmark className="mb-4 text-lg" />
              <p className="max-w-lg text-sm leading-relaxed text-[#bdb8b1]">
                A social layer for discovering problems, defining solutions, and
                iterating together—with explicit rights and people in control of
                every consequential action.
              </p>
            </div>
            <nav
              aria-label="Footer navigation"
              className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm md:grid-cols-3"
            >
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href="/projects"
              >
                Projects
              </Link>
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href="/opportunities"
              >
                Opportunities
              </Link>
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href="/governance"
              >
                Governance
              </Link>
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href="#method"
              >
                Method
              </Link>
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href="#nova"
              >
                Nova AI
              </Link>
              <Link
                className="inline-flex min-h-11 items-center hover:text-[#70d7e2]"
                href={workspaceHref}
              >
                Workspace
              </Link>
            </nav>
          </div>
          <div className="border-t border-[#f4efeb]/25 px-4 py-4 text-center font-mono text-[10px] tracking-[0.14em] text-[#aaa59f] uppercase">
            © {new Date().getFullYear()} ArdaNova / Discover · define · iterate
          </div>
        </footer>
      </div>
    </HydrateClient>
  );
}

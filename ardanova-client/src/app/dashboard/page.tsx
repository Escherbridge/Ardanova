"use client";

import Link from "next/link";
import { useSession } from "next-auth/react";
import {
  ArrowUpRight,
  CalendarDays,
  CheckSquare2,
  CircleDot,
  FileText,
  Lightbulb,
  RefreshCw,
  Search,
  Sparkles,
} from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { api } from "~/trpc/react";

const LIST_LIMIT = 3;

interface DashboardProposal {
  id: string;
  title: string;
  description?: string;
  type: string;
  status: string;
  votingEnd?: string;
}

interface DashboardEvent {
  id: string;
  title: string;
  startDate: string;
  timezone: string;
  status?: string;
  type?: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function normalizeDashboardProposals(value: unknown): DashboardProposal[] {
  if (!Array.isArray(value)) return [];

  return value.flatMap((item) => {
    if (
      !isRecord(item) ||
      typeof item.id !== "string" ||
      typeof item.title !== "string"
    ) {
      return [];
    }

    return [
      {
        id: item.id,
        title: item.title,
        description:
          typeof item.description === "string" ? item.description : undefined,
        type: typeof item.type === "string" ? item.type : "Proposal",
        status:
          typeof item.status === "string" ? item.status : "Status unavailable",
        votingEnd:
          typeof item.votingEnd === "string" ? item.votingEnd : undefined,
      },
    ];
  });
}

function normalizeDashboardEvents(value: unknown): DashboardEvent[] {
  if (!Array.isArray(value)) return [];

  return value.flatMap((item) => {
    if (
      !isRecord(item) ||
      typeof item.id !== "string" ||
      typeof item.title !== "string" ||
      typeof item.startDate !== "string"
    ) {
      return [];
    }

    return [
      {
        id: item.id,
        title: item.title,
        startDate: item.startDate,
        timezone:
          typeof item.timezone === "string"
            ? item.timezone
            : "Timezone not recorded",
        status: typeof item.status === "string" ? item.status : undefined,
        type: typeof item.type === "string" ? item.type : undefined,
      },
    ];
  });
}

function displayStatus(status: string) {
  return status.replaceAll("_", " ");
}

function displayDate(value: string) {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return new Intl.DateTimeFormat(undefined, {
    month: "short",
    day: "numeric",
    year:
      date.getFullYear() === new Date().getFullYear() ? undefined : "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(date);
}

function LoadingRecord({ label }: { label: string }) {
  return (
    <p
      className="text-muted-foreground border-t border-current/25 py-4 text-sm"
      role="status"
      aria-live="polite"
    >
      {label}
    </p>
  );
}

function ErrorRecord({ label }: { label: string }) {
  return (
    <p
      className="border-destructive text-destructive border-t py-4 text-sm"
      role="alert"
    >
      {label}
    </p>
  );
}

function EmptyRecord({ children }: { children: React.ReactNode }) {
  return (
    <p className="text-muted-foreground border-t border-current/25 py-4 text-sm">
      {children}
    </p>
  );
}

function RecordLink({
  href,
  eyebrow,
  title,
  detail,
  tone = "light",
}: {
  href: string;
  eyebrow?: string;
  title: string;
  detail?: string | null;
  tone?: "light" | "dark";
}) {
  return (
    <li
      className={
        tone === "dark"
          ? "border-primary-foreground border-t"
          : "border-border border-t"
      }
    >
      <Link
        href={href}
        className={
          tone === "dark"
            ? "group focus-visible:outline-primary-foreground flex min-h-24 items-start justify-between gap-4 py-4 focus-visible:outline-offset-[-3px]"
            : "group flex min-h-24 items-start justify-between gap-4 py-4 focus-visible:outline-offset-[-3px]"
        }
      >
        <span className="min-w-0">
          {eyebrow ? (
            <span
              className={
                tone === "dark"
                  ? "text-primary-foreground mb-1 block font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase"
                  : "text-muted-foreground mb-1 block font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase"
              }
            >
              {eyebrow}
            </span>
          ) : null}
          <span className="block leading-snug font-semibold">{title}</span>
          {detail ? (
            <span
              className={
                tone === "dark"
                  ? "text-primary-foreground mt-1 line-clamp-2 block text-sm leading-relaxed"
                  : "text-muted-foreground mt-1 line-clamp-2 block text-sm leading-relaxed"
              }
            >
              {detail}
            </span>
          ) : null}
        </span>
        <ArrowUpRight
          className="mt-1 size-4 shrink-0 transition-transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5"
          aria-hidden="true"
        />
      </Link>
    </li>
  );
}

export default function DashboardPage() {
  const { data: session } = useSession();

  const featuredProjectsQuery = api.project.getFeatured.useQuery();
  const myProjectsQuery = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: Boolean(session?.user) },
  );
  const myTasksQuery = api.task.getMyTasks.useQuery(
    { limit: 100 },
    { enabled: Boolean(session?.user) },
  );
  const activeProposalsQuery = api.governance.getActive.useQuery({
    limit: LIST_LIMIT,
  });
  const upcomingEventsQuery = api.event.getUpcoming.useQuery({
    limit: LIST_LIMIT,
  });

  const featuredProjects = (featuredProjectsQuery.data ?? []).slice(
    0,
    LIST_LIMIT,
  );
  const myProjects = (myProjectsQuery.data?.items ?? []).slice(0, LIST_LIMIT);
  const myTasks = (myTasksQuery.data?.items ?? []).slice(0, LIST_LIMIT);
  const activeProposals = normalizeDashboardProposals(
    activeProposalsQuery.data,
  ).slice(0, LIST_LIMIT);
  const upcomingEvents = normalizeDashboardEvents(
    upcomingEventsQuery.data,
  ).slice(0, LIST_LIMIT);
  const firstName = session?.user?.name?.trim().split(/\s+/)[0];

  return (
    <div className="space-y-10 py-8 sm:py-12">
      <section
        className="border-foreground grid overflow-hidden border lg:grid-cols-[minmax(0,1.45fr)_minmax(18rem,0.55fr)]"
        aria-labelledby="dashboard-title"
      >
        <div className="bg-accent text-accent-foreground p-6 sm:p-9 lg:p-12">
          <p className="mb-6 font-mono text-xs font-semibold tracking-[0.14em] uppercase">
            Workspace{firstName ? ` / ${firstName}` : ""}
          </p>
          <h1
            id="dashboard-title"
            className="max-w-4xl text-5xl leading-[0.9] sm:text-6xl lg:text-7xl"
          >
            Social software for doing.
          </h1>
          <p className="mt-7 max-w-2xl text-lg leading-relaxed sm:text-xl">
            Discover a problem. Define a solution with the people affected.
            Iterate from evidence. The solutionary approach is the revolutionary
            one.
          </p>
        </div>

        <div className="bg-foreground text-background flex flex-col justify-between gap-10 p-6 sm:p-9">
          <div>
            <p className="text-background/65 font-mono text-xs font-semibold tracking-[0.14em] uppercase">
              Agency, not attention
            </p>
            <p className="mt-4 text-2xl leading-tight font-semibold">
              No engagement score. No infinite feed. No manufactured urgency.
            </p>
            <p className="text-background/70 mt-4 text-sm leading-relaxed">
              Participation is not proof of ownership. Governance credentials,
              project utility, and economic rights remain separate, reviewable
              records.
            </p>
          </div>
          <div className="grid gap-3">
            <Button asChild size="lg" className="w-full justify-between">
              <Link href="/studio">
                Open Nova Studio
                <Sparkles aria-hidden="true" />
              </Link>
            </Button>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="border-background/45 text-background hover:border-background hover:bg-background hover:text-foreground w-full justify-between bg-transparent"
            >
              <Link href="/projects/create">
                Start a project record
                <ArrowUpRight aria-hidden="true" />
              </Link>
            </Button>
          </div>
        </div>
      </section>

      <section aria-labelledby="core-loop-title">
        <div className="border-foreground mb-5 flex flex-col justify-between gap-3 border-b pb-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-muted-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
              Core loop
            </p>
            <h2 id="core-loop-title" className="mt-2 text-3xl sm:text-4xl">
              Move one useful thing forward.
            </h2>
          </div>
          <p className="text-muted-foreground max-w-md text-sm leading-relaxed">
            Each list is intentionally short. Choose a record, take an action,
            then return when the work changes.
          </p>
        </div>

        <ol className="border-foreground grid border-x border-b lg:grid-cols-3">
          <li className="border-foreground bg-card flex min-w-0 flex-col border-t p-5 sm:p-7 lg:border-r">
            <div className="flex items-start justify-between gap-4">
              <div>
                <span className="text-accent font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                  01 / Discover
                </span>
                <h3 className="mt-3 text-2xl">Find the real problem.</h3>
              </div>
              <Search
                className="text-accent size-6 shrink-0"
                aria-hidden="true"
              />
            </div>
            <p className="text-muted-foreground mt-3 min-h-16 text-sm leading-relaxed">
              Start with lived experience and evidence. Inspect what is already
              being tried before proposing another answer.
            </p>

            <div className="mt-6 flex-1">
              <p className="text-muted-foreground mb-2 font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase">
                Featured project records
              </p>
              {featuredProjectsQuery.isLoading ? (
                <LoadingRecord label="Loading projects to inspect…" />
              ) : featuredProjectsQuery.error ? (
                <ErrorRecord label="Project records are unavailable right now." />
              ) : featuredProjects.length === 0 ? (
                <EmptyRecord>
                  No featured project records are available.
                </EmptyRecord>
              ) : (
                <ul>
                  {featuredProjects.map((project) => (
                    <RecordLink
                      key={project.id}
                      href={`/projects/${project.slug}`}
                      eyebrow={(project.categories ?? [])[0]?.replaceAll(
                        "_",
                        " ",
                      )}
                      title={project.title}
                      detail={project.problemStatement || project.description}
                    />
                  ))}
                </ul>
              )}
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <Button asChild variant="outline" className="flex-1">
                <Link href="/projects">Explore projects</Link>
              </Button>
              <Button asChild variant="ghost" className="flex-1">
                <Link href="/opportunities">Find open work</Link>
              </Button>
            </div>
          </li>

          <li className="border-foreground bg-secondary flex min-w-0 flex-col border-t p-5 sm:p-7 lg:border-r">
            <div className="flex items-start justify-between gap-4">
              <div>
                <span className="text-primary font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                  02 / Define
                </span>
                <h3 className="mt-3 text-2xl">Make a testable solution.</h3>
              </div>
              <Lightbulb
                className="text-primary size-6 shrink-0"
                aria-hidden="true"
              />
            </div>
            <p className="text-muted-foreground mt-3 min-h-16 text-sm leading-relaxed">
              Name the people, evidence, scope, and next decision. Keep
              assumptions visible so collaborators can challenge them.
            </p>

            <div className="mt-6 flex-1">
              <p className="text-muted-foreground mb-2 font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase">
                Your project records
              </p>
              {myProjectsQuery.isLoading ? (
                <LoadingRecord label="Loading your project records…" />
              ) : myProjectsQuery.error ? (
                <ErrorRecord label="Your project records are unavailable right now." />
              ) : myProjects.length === 0 ? (
                <EmptyRecord>You do not have a project record yet.</EmptyRecord>
              ) : (
                <ul>
                  {myProjects.map((project) => (
                    <RecordLink
                      key={project.id}
                      href={`/projects/${project.slug}`}
                      eyebrow={displayStatus(project.status)}
                      title={project.title}
                      detail={project.solution || project.description}
                    />
                  ))}
                </ul>
              )}
            </div>

            <div className="mt-6 flex flex-wrap gap-3">
              <Button asChild className="flex-1">
                <Link href="/studio">Draft with Nova</Link>
              </Button>
              <Button asChild variant="outline" className="flex-1">
                <Link href="/projects/create">Create project</Link>
              </Button>
            </div>
          </li>

          <li className="border-foreground bg-primary text-primary-foreground flex min-w-0 flex-col border-t p-5 sm:p-7">
            <div className="flex items-start justify-between gap-4">
              <div>
                <span className="text-primary-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                  03 / Iterate
                </span>
                <h3 className="mt-3 text-2xl">Learn through the work.</h3>
              </div>
              <RefreshCw className="size-6 shrink-0" aria-hidden="true" />
            </div>
            <p className="text-primary-foreground mt-3 min-h-16 text-sm leading-relaxed">
              Complete a bounded task, review the evidence, and change the plan.
              A task state does not imply an award has been reconciled.
            </p>

            <div className="mt-6 flex-1">
              <p className="text-primary-foreground mb-2 font-mono text-[0.68rem] font-semibold tracking-[0.12em] uppercase">
                Work assigned to you
              </p>
              {myTasksQuery.isLoading ? (
                <p
                  className="border-primary-foreground text-primary-foreground border-t py-4 text-sm"
                  role="status"
                  aria-live="polite"
                >
                  Loading assigned work…
                </p>
              ) : myTasksQuery.error ? (
                <p
                  className="border-primary-foreground/50 border-t py-4 text-sm"
                  role="alert"
                >
                  Assigned work is unavailable right now.
                </p>
              ) : myTasks.length === 0 ? (
                <p className="border-primary-foreground text-primary-foreground border-t py-4 text-sm">
                  No task records are currently assigned to you.
                </p>
              ) : (
                <ul>
                  {myTasks.map((task) => (
                    <RecordLink
                      key={task.id}
                      href="/tasks"
                      eyebrow={`${displayStatus(task.status)} / ${displayStatus(task.priority)}`}
                      title={task.title}
                      detail={task.description}
                      tone="dark"
                    />
                  ))}
                </ul>
              )}
            </div>

            <div className="mt-6">
              <Button
                asChild
                variant="outline"
                className="border-primary-foreground text-primary-foreground hover:border-primary-foreground hover:bg-primary-foreground hover:text-primary focus-visible:outline-primary-foreground w-full bg-transparent"
              >
                <Link href="/tasks">Open assigned work</Link>
              </Button>
            </div>
          </li>
        </ol>
      </section>

      <section aria-labelledby="coordinate-title">
        <div className="border-foreground mb-5 flex flex-col justify-between gap-3 border-b pb-5 sm:flex-row sm:items-end">
          <div>
            <p className="text-muted-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
              Coordinate
            </p>
            <h2 id="coordinate-title" className="mt-2 text-3xl sm:text-4xl">
              Review windows, not attention traps.
            </h2>
          </div>
          <p className="text-muted-foreground max-w-md text-sm leading-relaxed">
            Proposals remain proposals until the recorded process resolves them.
            Events are shown as scheduled, not as attendance or consent.
          </p>
        </div>

        <div className="border-foreground grid border lg:grid-cols-2">
          <article className="lg:border-foreground p-5 sm:p-7 lg:border-r">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-muted-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                  Governance
                </p>
                <h3 className="mt-2 text-2xl">Active proposals</h3>
              </div>
              <FileText className="text-accent size-6" aria-hidden="true" />
            </div>

            <div className="mt-5">
              {activeProposalsQuery.isLoading ? (
                <LoadingRecord label="Loading active proposals…" />
              ) : activeProposalsQuery.error ? (
                <ErrorRecord label="Active proposals are unavailable right now." />
              ) : activeProposals.length === 0 ? (
                <EmptyRecord>No active proposals are recorded.</EmptyRecord>
              ) : (
                <ul>
                  {activeProposals.map((proposal) => {
                    const votingEnd = proposal.votingEnd
                      ? displayDate(proposal.votingEnd)
                      : null;

                    return (
                      <RecordLink
                        key={proposal.id}
                        href={`/governance/${proposal.id}`}
                        eyebrow={`${proposal.type} / ${displayStatus(proposal.status)}${votingEnd ? ` / closes ${votingEnd}` : ""}`}
                        title={proposal.title}
                        detail={proposal.description}
                      />
                    );
                  })}
                </ul>
              )}
            </div>

            <Button
              asChild
              variant="outline"
              className="mt-5 w-full justify-between"
            >
              <Link href="/governance">
                Review governance
                <ArrowUpRight aria-hidden="true" />
              </Link>
            </Button>
          </article>

          <article className="border-foreground border-t p-5 sm:p-7 lg:border-t-0">
            <div className="flex items-center justify-between gap-4">
              <div>
                <p className="text-muted-foreground font-mono text-xs font-semibold tracking-[0.14em] uppercase">
                  Working sessions
                </p>
                <h3 className="mt-2 text-2xl">Upcoming events</h3>
              </div>
              <CalendarDays
                className="text-primary size-6"
                aria-hidden="true"
              />
            </div>

            <div className="mt-5">
              {upcomingEventsQuery.isLoading ? (
                <LoadingRecord label="Loading upcoming events…" />
              ) : upcomingEventsQuery.error ? (
                <ErrorRecord label="Upcoming events are unavailable right now." />
              ) : upcomingEvents.length === 0 ? (
                <EmptyRecord>No upcoming events are scheduled.</EmptyRecord>
              ) : (
                <ul>
                  {upcomingEvents.map((event) => {
                    const startsAt = displayDate(event.startDate);

                    return (
                      <RecordLink
                        key={event.id}
                        href="/events"
                        eyebrow={
                          event.status
                            ? displayStatus(event.status)
                            : (event.type ?? undefined)
                        }
                        title={event.title}
                        detail={
                          startsAt
                            ? `${startsAt} / ${event.timezone}`
                            : event.timezone
                        }
                      />
                    );
                  })}
                </ul>
              )}
            </div>

            <Button
              asChild
              variant="outline"
              className="mt-5 w-full justify-between"
            >
              <Link href="/events">
                Open event calendar
                <ArrowUpRight aria-hidden="true" />
              </Link>
            </Button>
          </article>
        </div>
      </section>

      <aside
        className="border-foreground bg-card grid border sm:grid-cols-[auto_1fr_auto] sm:items-center"
        aria-label="End of workspace snapshot"
      >
        <div className="border-foreground border-b p-5 sm:border-r sm:border-b-0">
          <CircleDot className="text-primary size-6" aria-hidden="true" />
        </div>
        <div className="p-5">
          <p className="font-semibold">That is the whole snapshot.</p>
          <p className="text-muted-foreground mt-1 text-sm">
            Finite by design. Choose one useful action, or close the tab and
            return to the work when something changes.
          </p>
        </div>
        <div className="border-foreground border-t p-5 sm:border-t-0 sm:border-l">
          <Badge variant="outline" className="gap-2">
            <CheckSquare2 aria-hidden="true" />
            End of list
          </Badge>
        </div>
      </aside>
    </div>
  );
}

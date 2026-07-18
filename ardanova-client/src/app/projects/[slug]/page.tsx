"use client";

import { useState, type ReactNode } from "react";
import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  Bell,
  CheckSquare,
  Coins,
  DollarSign,
  Eye,
  FileText,
  Loader2,
  Map,
  MessageCircle,
  ShieldCheck,
  Target,
  Users,
  Vote,
  Zap,
} from "lucide-react";

import { api } from "~/trpc/react";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Progress } from "~/components/ui/progress";
import {
  CommentsTab,
  EquityTab,
  FundingTab,
  OpportunitiesTab,
  OverviewTab,
  ProposalsTab,
  RoadmapTab,
  SprintsTab,
  TasksTab,
  TeamTab,
  UpdatesTab,
} from "~/components/projects";
import type {
  ProjectDuration,
  ProjectStatus,
  ProjectType,
} from "~/lib/contracts/project-contract";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { cn } from "~/lib/utils";

const tabs = [
  { id: "overview", label: "Overview", icon: <FileText /> },
  { id: "updates", label: "Updates", icon: <Bell /> },
  { id: "team", label: "Team", icon: <Users /> },
  { id: "funding", label: "Funding", icon: <DollarSign /> },
  { id: "equity", label: "Token position", icon: <Coins /> },
  { id: "proposals", label: "Proposals", icon: <Vote /> },
  { id: "opportunities", label: "Work", icon: <Target /> },
  { id: "tasks", label: "Tasks", icon: <CheckSquare /> },
  { id: "roadmap", label: "Roadmap", icon: <Map /> },
  { id: "sprints", label: "Sprints", icon: <Zap /> },
  { id: "comments", label: "Comments", icon: <MessageCircle /> },
] as const;

type ProjectTab = (typeof tabs)[number]["id"];

const statusVariants: Record<
  ProjectStatus,
  | "neon"
  | "neon-green"
  | "neon-purple"
  | "warning"
  | "secondary"
  | "destructive"
> = {
  DRAFT: "secondary",
  PUBLISHED: "neon",
  SEEKING_SUPPORT: "warning",
  FUNDED: "neon-green",
  IN_PROGRESS: "neon-purple",
  COMPLETED: "neon-green",
  CANCELLED: "destructive",
};

const projectTypeLabels: Record<ProjectType, string> = {
  TEMPORARY: "Temporary",
  LONG_TERM: "Long term",
  FOUNDATION: "Foundation",
  BUSINESS: "Business",
  PRODUCT: "Product",
  OPEN_SOURCE: "Open source",
  COMMUNITY: "Community",
};

const durationLabels: Record<ProjectDuration, string> = {
  ONE_TWO_WEEKS: "1-2 weeks",
  ONE_THREE_MONTHS: "1-3 months",
  THREE_SIX_MONTHS: "3-6 months",
  SIX_TWELVE_MONTHS: "6-12 months",
  ONE_TWO_YEARS: "1-2 years",
  TWO_PLUS_YEARS: "2+ years",
  ONGOING: "Ongoing",
};

function humanize(value: string) {
  return value
    .split("_")
    .map((part) => part.charAt(0) + part.slice(1).toLowerCase())
    .join(" ");
}

function formatUSD(value: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    maximumFractionDigits: 0,
  }).format(value);
}

export default function ProjectDetailPage() {
  const params = useParams();
  const searchParams = useSearchParams();
  const routeId = params.slug as string;
  const proposalId = searchParams.get("proposalId");
  const [activeTab, setActiveTab] = useState<ProjectTab>(
    proposalId ? "proposals" : "overview",
  );
  const { data: session } = useSession();

  const projectQuery = api.project.getById.useQuery({ id: routeId });
  const project = projectQuery.data;
  const membersQuery = api.project.getMembers.useQuery(
    { projectId: project?.id ?? "" },
    { enabled: Boolean(project?.id) },
  );
  const credentialsQuery =
    api.membershipCredential.getActiveByProjectId.useQuery(
      { projectId: project?.id ?? "" },
      { enabled: Boolean(project?.id) },
    );

  const currentUserId = session?.user?.id;
  const isOwner = Boolean(
    currentUserId && project?.createdById === currentUserId,
  );
  const userMember = membersQuery.data?.find(
    (member) => member.userId === currentUserId,
  );
  const isMember = isOwner || Boolean(userMember);
  const canManageProject =
    isOwner ||
    userMember?.role === "FOUNDER" ||
    userMember?.role === "LEADER" ||
    userMember?.role === "CORE_CONTRIBUTOR";
  const canonicalRouteId = project?.slug || project?.id || routeId;

  if (projectQuery.isLoading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center p-6">
        <div className="flex items-center gap-3" role="status">
          <Loader2 className="text-primary size-5 animate-spin" />
          <span className="font-mono text-sm">Loading project record...</span>
        </div>
      </div>
    );
  }

  if (projectQuery.error || !project) {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-xl flex-col items-start justify-center gap-5 p-6">
        <div>
          <p className="text-destructive font-mono text-xs font-bold tracking-widest uppercase">
            Project unavailable
          </p>
          <h1 className="mt-2 text-3xl font-black">
            We could not open this project
          </h1>
          <p className="text-muted-foreground mt-2 text-sm">
            The project may not exist, or its API record did not match the
            current project contract.
          </p>
        </div>
        <Button asChild variant="outline">
          <Link href="/projects">
            <ArrowLeft className="size-4" />
            Back to projects
          </Link>
        </Button>
      </div>
    );
  }

  const creatorName = project.createdBy?.name ?? "Creator name unavailable";
  const creatorInitial =
    project.createdBy?.name?.charAt(0).toUpperCase() ?? "?";
  const fundingGoal = project.fundingGoal;
  const fundingProgress =
    fundingGoal && fundingGoal > 0
      ? Math.min((project.currentFunding / fundingGoal) * 100, 100)
      : null;

  return (
    <div className="bg-background min-h-screen">
      <header className="border-foreground border-b">
        <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8">
          <Button variant="ghost" asChild className="mb-5 -ml-3">
            <Link href="/projects">
              <ArrowLeft className="size-4" />
              Back to projects
            </Link>
          </Button>

          <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_18rem]">
            <div>
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant={statusVariants[project.status]}>
                  {humanize(project.status)}
                </Badge>
                <Badge variant="secondary">
                  {projectTypeLabels[project.projectType]}
                </Badge>
                {project.duration && (
                  <Badge variant="secondary">
                    {durationLabels[project.duration]}
                  </Badge>
                )}
                {project.categories.map((category) => (
                  <Badge key={category} variant="secondary">
                    {humanize(category)}
                  </Badge>
                ))}
              </div>

              <h1 className="mt-5 max-w-4xl text-4xl font-black tracking-tight sm:text-6xl">
                {project.title}
              </h1>
              <p className="text-muted-foreground mt-4 max-w-3xl text-base leading-relaxed sm:text-lg">
                {project.description}
              </p>

              <Link
                href={`/dashboard/profile/${project.createdById}`}
                className="border-border hover:border-foreground mt-6 inline-flex min-h-11 items-center gap-3 border px-3 py-2"
              >
                <Avatar className="border-border size-8 border">
                  <AvatarImage src={project.createdBy?.image ?? undefined} />
                  <AvatarFallback>{creatorInitial}</AvatarFallback>
                </Avatar>
                <span>
                  <span className="block text-sm font-semibold">
                    {creatorName}
                  </span>
                  <span className="text-muted-foreground block text-xs">
                    Project creator
                  </span>
                </span>
              </Link>
            </div>

            <dl className="border-border grid grid-cols-2 border lg:grid-cols-1">
              <Metric
                label="Supporters"
                value={project.supportersCount.toLocaleString()}
              />
              <Metric
                label="Votes recorded"
                value={project.votesCount.toLocaleString()}
              />
              <Metric
                label="Views recorded"
                value={project.viewsCount.toLocaleString()}
                icon={<Eye className="size-3.5" aria-hidden="true" />}
              />
              <Metric
                label="Current funding"
                value={formatUSD(project.currentFunding)}
              />
              <Metric
                label="Active credentials"
                value={
                  credentialsQuery.isLoading
                    ? "Loading"
                    : credentialsQuery.error
                      ? "Unavailable"
                      : String(credentialsQuery.data?.length ?? 0)
                }
                icon={<ShieldCheck className="size-3.5" aria-hidden="true" />}
              />
            </dl>
          </div>

          {fundingProgress !== null && fundingGoal !== null && (
            <div className="border-border mt-8 max-w-3xl border-t pt-5">
              <div className="mb-2 flex flex-wrap justify-between gap-2 text-sm">
                <span className="font-semibold">Funding record</span>
                <span className="text-muted-foreground font-mono">
                  {formatUSD(project.currentFunding)} of{" "}
                  {formatUSD(fundingGoal)}
                </span>
              </div>
              <Progress
                value={fundingProgress}
                variant="neon"
                aria-label={`Funding progress for ${project.title}`}
              />
            </div>
          )}

          {project.commerceEnabled && (
            <div className="border-primary bg-primary/5 mt-6 max-w-3xl border-l-4 p-4">
              <p className="text-primary font-mono text-xs font-bold tracking-widest uppercase">
                Commerce enabled
              </p>
              <p className="text-muted-foreground mt-2 text-sm">
                {project.storefrontDescription ??
                  "This project can expose commerce workflows."}{" "}
                Project tokens, membership credentials, and separately approved
                ownership shares remain distinct records with distinct rights.
              </p>
            </div>
          )}
        </div>
      </header>

      <div className="border-border bg-background border-b">
        <div
          className="mx-auto flex max-w-6xl overflow-x-auto px-4 sm:px-6"
          role="tablist"
          aria-label="Project workspace"
          onKeyDown={handleTabListKeyDown}
        >
          {tabs.map((tab) => {
            const selected = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                role="tab"
                id={`project-workspace-tab-${tab.id}`}
                aria-selected={selected}
                aria-controls={`project-workspace-panel-${tab.id}`}
                tabIndex={selected ? 0 : -1}
                onClick={() => setActiveTab(tab.id)}
                className={cn(
                  "border-border relative flex min-h-12 shrink-0 items-center gap-2 border-r px-4 text-sm font-semibold",
                  selected
                    ? "bg-primary text-primary-foreground"
                    : "text-muted-foreground hover:bg-secondary hover:text-foreground",
                )}
              >
                <span className="size-4 [&>svg]:size-4" aria-hidden="true">
                  {tab.icon}
                </span>
                {tab.label}
              </button>
            );
          })}
        </div>
      </div>

      <section
        className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-8"
        role="tabpanel"
        id={`project-workspace-panel-${activeTab}`}
        aria-labelledby={`project-workspace-tab-${activeTab}`}
      >
        {activeTab === "overview" && <OverviewTab project={project} />}
        {activeTab === "updates" && (
          <UpdatesTab projectId={project.id} isOwner={isOwner} />
        )}
        {activeTab === "team" && (
          <TeamTab
            projectId={project.id}
            projectSlug={canonicalRouteId}
            isOwner={isOwner}
          />
        )}
        {activeTab === "funding" && (
          <FundingTab projectId={project.id} projectSlug={canonicalRouteId} />
        )}
        {activeTab === "equity" && <EquityTab projectId={project.id} />}
        {activeTab === "proposals" && (
          <ProposalsTab
            projectId={project.id}
            isOwner={isOwner}
            isMember={isMember}
            selectedProposalId={proposalId ?? undefined}
            userId={currentUserId}
          />
        )}
        {activeTab === "opportunities" && (
          <OpportunitiesTab
            projectId={project.id}
            projectSlug={canonicalRouteId}
            isOwner={isOwner}
            canComment={Boolean(currentUserId)}
            userRole={userMember?.role}
          />
        )}
        {activeTab === "tasks" && (
          <TasksTab projectId={project.id} isOwner={isOwner} />
        )}
        {activeTab === "roadmap" && (
          <RoadmapTab projectId={project.id} isOwner={isOwner} />
        )}
        {activeTab === "sprints" && (
          <SprintsTab projectId={project.id} canManage={canManageProject} />
        )}
        {activeTab === "comments" && <CommentsTab projectId={project.id} />}
      </section>
    </div>
  );
}

function Metric({
  label,
  value,
  icon,
}: {
  label: string;
  value: string;
  icon?: ReactNode;
}) {
  return (
    <div className="border-border border-r border-b p-4 last:border-b-0 lg:border-r-0">
      <dt className="text-muted-foreground flex items-center gap-2 text-xs">
        {icon}
        {label}
      </dt>
      <dd className="mt-1 font-mono text-lg font-bold">{value}</dd>
    </div>
  );
}

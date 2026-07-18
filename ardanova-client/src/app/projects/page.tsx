"use client";

import { useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import {
  ArrowRight,
  Clock,
  FolderKanban,
  Plus,
  RefreshCw,
  Search,
  Sparkles,
  Users,
  Vote,
  X,
} from "lucide-react";

import { api } from "~/trpc/react";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { FeedLayout } from "~/components/layouts/feed-layout";
import { Progress } from "~/components/ui/progress";
import { useEnumOptions } from "~/hooks/use-enum";
import type { Project } from "~/lib/api/ardanova/endpoints/projects";
import { handleTabListKeyDown } from "~/lib/accessibility";
import {
  isProjectStatus,
  isProjectType,
  PROJECT_STATUSES,
  PROJECT_TYPES,
  type ProjectStatus,
  type ProjectType,
} from "~/lib/contracts/project-contract";
import { cn } from "~/lib/utils";

const projectTabs = [
  {
    id: "all",
    label: "All",
    icon: <FolderKanban className="size-4" aria-hidden="true" />,
  },
  {
    id: "active",
    label: "Recent activity",
    icon: <RefreshCw className="size-4" aria-hidden="true" />,
  },
  {
    id: "newest",
    label: "Newest",
    icon: <Clock className="size-4" aria-hidden="true" />,
  },
  {
    id: "funded",
    label: "Funded",
    icon: <Sparkles className="size-4" aria-hidden="true" />,
  },
] as const;

type ProjectTab = (typeof projectTabs)[number]["id"];

const projectTypeLabels: Record<ProjectType, string> = {
  TEMPORARY: "Temporary",
  LONG_TERM: "Long term",
  FOUNDATION: "Foundation",
  BUSINESS: "Business",
  PRODUCT: "Product",
  OPEN_SOURCE: "Open source",
  COMMUNITY: "Community",
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

function formatRelativeTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Date unavailable";

  const elapsed = Date.now() - date.getTime();
  const days = Math.floor(elapsed / 86_400_000);
  if (days < 0) return date.toLocaleDateString();
  if (days === 0) return "Today";
  if (days < 7) return `${days}d ago`;
  return date.toLocaleDateString();
}

function filterAndSortProjects(
  projects: Project[],
  options: {
    tab: ProjectTab;
    search: string;
    category: string;
    projectType: ProjectType | "all";
    status: ProjectStatus | "all";
  },
) {
  const query = options.search.trim().toLowerCase();
  const filtered = projects.filter((project) => {
    if (
      options.tab === "funded" &&
      project.status !== "FUNDED" &&
      project.status !== "COMPLETED"
    ) {
      return false;
    }
    if (
      query &&
      !project.title.toLowerCase().includes(query) &&
      !project.description.toLowerCase().includes(query) &&
      !project.problemStatement.toLowerCase().includes(query) &&
      !project.tags?.toLowerCase().includes(query)
    ) {
      return false;
    }
    if (
      options.category !== "all" &&
      !project.categories.includes(options.category)
    ) {
      return false;
    }
    if (
      options.projectType !== "all" &&
      project.projectType !== options.projectType
    ) {
      return false;
    }
    if (options.status !== "all" && project.status !== options.status) {
      return false;
    }
    return true;
  });

  if (options.tab === "active") {
    return filtered.sort(
      (a, b) =>
        new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
    );
  }
  if (options.tab === "newest") {
    return filtered.sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
  }
  return filtered;
}

export default function ProjectsPage() {
  const [activeTab, setActiveTab] = useState<ProjectTab>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedProjectType, setSelectedProjectType] = useState<
    ProjectType | "all"
  >("all");
  const [selectedStatus, setSelectedStatus] = useState<ProjectStatus | "all">(
    "all",
  );

  const categoryQuery = useEnumOptions("ProjectCategory");
  const projectsQuery = api.project.getAll.useQuery({ limit: 50, page: 1 });
  const projects = useMemo(
    () => projectsQuery.data?.items ?? [],
    [projectsQuery.data?.items],
  );

  const categories = useMemo(
    () =>
      Array.from(
        new Set([
          ...categoryQuery.options.map(({ id }) => id),
          ...projects.flatMap(({ categories: values }) => values),
        ]),
      ).sort(),
    [categoryQuery.options, projects],
  );

  const filteredProjects = filterAndSortProjects(projects, {
    tab: activeTab,
    search: searchQuery,
    category: selectedCategory,
    projectType: selectedProjectType,
    status: selectedStatus,
  });
  const hasFilters =
    searchQuery.trim().length > 0 ||
    selectedCategory !== "all" ||
    selectedProjectType !== "all" ||
    selectedStatus !== "all";
  const clearFilters = () => {
    setSearchQuery("");
    setSelectedCategory("all");
    setSelectedProjectType("all");
    setSelectedStatus("all");
  };

  const currentViewStats = {
    loaded: projects.length,
    funded: projects.filter(
      ({ status }) => status === "FUNDED" || status === "COMPLETED",
    ).length,
    recordedFunding: projects.reduce(
      (total, project) => total + project.currentFunding,
      0,
    ),
    supporters: projects.reduce(
      (total, project) => total + project.supportersCount,
      0,
    ),
  };
  const recentlyUpdatedInView = [...projects]
    .sort(
      (a, b) =>
        new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
    )
    .slice(0, 3);

  return (
    <FeedLayout
      sidebar={
        <div className="space-y-4">
          <Card className="border-foreground">
            <CardHeader className="border-border border-b pb-4">
              <CardTitle className="text-base">Current API page</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 p-4">
              <Stat
                label="Projects loaded"
                value={String(currentViewStats.loaded)}
              />
              <Stat
                label="Funded or complete"
                value={String(currentViewStats.funded)}
              />
              <Stat
                label="Funding recorded"
                value={formatUSD(currentViewStats.recordedFunding)}
              />
              <Stat
                label="Supporters recorded"
                value={String(currentViewStats.supporters)}
              />
              <p className="border-border text-muted-foreground border-t pt-3 text-xs">
                These totals summarize the loaded first page, not the entire
                platform.
              </p>
            </CardContent>
          </Card>

          <Card className="border-border">
            <CardHeader className="border-border border-b pb-4">
              <CardTitle className="flex items-center gap-2 text-base">
                <RefreshCw className="text-primary size-4" />
                Recently updated in this view
              </CardTitle>
            </CardHeader>
            <CardContent className="divide-border divide-y p-0">
              {recentlyUpdatedInView.length === 0 ? (
                <p className="text-muted-foreground p-4 text-sm">
                  No loaded records.
                </p>
              ) : (
                recentlyUpdatedInView.map((project) => (
                  <Link
                    key={project.id}
                    href={`/projects/${project.slug || project.id}`}
                    className="hover:bg-secondary flex min-h-14 items-center justify-between gap-3 p-4"
                  >
                    <span className="line-clamp-2 text-sm font-semibold">
                      {project.title}
                    </span>
                    <span className="text-muted-foreground shrink-0 font-mono text-xs">
                      {formatRelativeTime(project.updatedAt)}
                    </span>
                  </Link>
                ))
              )}
            </CardContent>
          </Card>
        </div>
      }
    >
      <header className="border-foreground bg-background border-b p-4 sm:p-6">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-primary font-mono text-xs font-bold tracking-[0.2em] uppercase">
              Discover a problem / define a solution / iterate
            </p>
            <h1 className="mt-2 text-3xl font-black sm:text-5xl">
              Projects for doing
            </h1>
            <p className="text-muted-foreground mt-2 max-w-2xl text-sm sm:text-base">
              Open a project to understand the problem, inspect the proposed
              solution, and find the work that moves it forward.
            </p>
          </div>
          <Button asChild>
            <Link href="/projects/create">
              <Plus className="size-4" />
              Define a project
            </Link>
          </Button>
        </div>

        <div className="mt-6 grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto_auto_auto]">
          <label className="relative block">
            <span className="sr-only">Search loaded projects</span>
            <Search
              className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2"
              aria-hidden="true"
            />
            <input
              type="search"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search problems, solutions, or tags"
              className="border-foreground bg-background min-h-11 w-full border py-2 pr-10 pl-10 text-sm outline-none"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery("")}
                className="text-muted-foreground hover:text-foreground absolute top-1/2 right-1 flex size-10 -translate-y-1/2 items-center justify-center"
                aria-label="Clear search"
              >
                <X className="size-4" />
              </button>
            )}
          </label>

          <FilterSelect
            label="Category"
            value={selectedCategory}
            onChange={setSelectedCategory}
            options={categories.map((value) => ({
              value,
              label: humanize(value),
            }))}
          />
          <FilterSelect
            label="Type"
            value={selectedProjectType}
            onChange={(value) => {
              if (value === "all" || isProjectType(value)) {
                setSelectedProjectType(value);
              }
            }}
            options={PROJECT_TYPES.map((value) => ({
              value,
              label: projectTypeLabels[value],
            }))}
          />
          <FilterSelect
            label="Status"
            value={selectedStatus}
            onChange={(value) => {
              if (value === "all" || isProjectStatus(value)) {
                setSelectedStatus(value);
              }
            }}
            options={PROJECT_STATUSES.map((value) => ({
              value,
              label: humanize(value),
            }))}
          />
        </div>

        {hasFilters && (
          <div className="border-border mt-3 flex items-center justify-between border-t pt-3">
            <p className="text-muted-foreground text-xs">
              Filters apply to the currently loaded API page.
            </p>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={clearFilters}
            >
              Clear filters
            </Button>
          </div>
        )}
      </header>

      <div
        className="border-border grid grid-cols-4 border-b"
        role="tablist"
        aria-label="Project ordering"
        onKeyDown={handleTabListKeyDown}
      >
        {projectTabs.map((tab) => {
          const selected = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              type="button"
              role="tab"
              id={`projects-tab-${tab.id}`}
              aria-label={tab.label}
              aria-selected={selected}
              aria-controls={`projects-panel-${tab.id}`}
              tabIndex={selected ? 0 : -1}
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                "border-border flex min-h-12 items-center justify-center gap-2 border-r px-2 text-xs font-semibold last:border-r-0 sm:text-sm",
                selected
                  ? "bg-primary text-primary-foreground"
                  : "text-muted-foreground hover:bg-secondary hover:text-foreground",
              )}
            >
              {tab.icon}
              <span className="hidden sm:inline">{tab.label}</span>
            </button>
          );
        })}
      </div>

      <div
        role="tabpanel"
        id={`projects-panel-${activeTab}`}
        aria-labelledby={`projects-tab-${activeTab}`}
        aria-live="polite"
      >
        {projectsQuery.isLoading ? (
          <div
            className="flex min-h-64 items-center justify-center"
            role="status"
          >
            <div className="flex items-center gap-3">
              <span className="border-primary size-5 animate-spin border-2 border-t-transparent" />
              <span className="font-mono text-sm">
                Loading project records...
              </span>
            </div>
          </div>
        ) : projectsQuery.error ? (
          <div className="flex min-h-64 flex-col items-start justify-center gap-4 p-6">
            <div>
              <p className="text-destructive font-mono text-xs font-bold tracking-widest uppercase">
                Project query failed
              </p>
              <h2 className="mt-2 text-2xl font-bold">
                The current project page is unavailable
              </h2>
              <p className="text-muted-foreground mt-2 text-sm">
                No sample records are substituted when the API fails.
              </p>
            </div>
            <Button
              type="button"
              variant="outline"
              onClick={() => projectsQuery.refetch()}
            >
              Try again
            </Button>
          </div>
        ) : filteredProjects.length === 0 ? (
          <div className="flex min-h-64 flex-col items-start justify-center gap-4 p-6">
            <FolderKanban
              className="text-muted-foreground size-10"
              aria-hidden="true"
            />
            <div>
              <h2 className="text-2xl font-bold">
                {projects.length === 0
                  ? "No projects returned"
                  : "No current-view matches"}
              </h2>
              <p className="text-muted-foreground mt-2 text-sm">
                {projects.length === 0
                  ? "The first API page is empty. Define a project to start a problem-solving loop."
                  : "Change or clear the local filters to inspect the loaded records."}
              </p>
            </div>
            {projects.length === 0 ? (
              <Button asChild>
                <Link href="/projects/create">Define a project</Link>
              </Button>
            ) : (
              <Button type="button" variant="outline" onClick={clearFilters}>
                Clear filters
              </Button>
            )}
          </div>
        ) : (
          <div className="divide-border divide-y">
            {filteredProjects.map((project) => (
              <ProjectRecord key={project.id} project={project} />
            ))}
            <div className="text-muted-foreground p-5 text-center text-xs">
              End of current results: showing {filteredProjects.length} from the
              loaded first page
              {projectsQuery.data?.totalCount !== undefined
                ? ` (${projectsQuery.data.totalCount} total reported by the API).`
                : "."}
            </div>
          </div>
        )}
      </div>
    </FeedLayout>
  );
}

function ProjectRecord({ project }: { project: Project }) {
  const creatorName = project.createdBy?.name ?? "Creator name unavailable";
  const creatorInitial =
    project.createdBy?.name?.charAt(0).toUpperCase() ?? "?";
  const routeId = project.slug || project.id;
  const fundingProgress =
    project.fundingGoal && project.fundingGoal > 0
      ? Math.min((project.currentFunding / project.fundingGoal) * 100, 100)
      : null;

  return (
    <article className="bg-card p-4 sm:p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <Link
          href={`/dashboard/profile/${project.createdById}`}
          className="flex min-h-11 items-center gap-3"
        >
          <Avatar className="border-border size-10 border">
            <AvatarImage src={project.createdBy?.image ?? undefined} />
            <AvatarFallback>{creatorInitial}</AvatarFallback>
          </Avatar>
          <span>
            <span className="block text-sm font-semibold">{creatorName}</span>
            <span className="text-muted-foreground block text-xs">
              Project creator / {formatRelativeTime(project.createdAt)}
            </span>
          </span>
        </Link>
        <div className="flex flex-wrap gap-2">
          <Badge variant="secondary">{humanize(project.status)}</Badge>
          <Badge variant="secondary">
            {projectTypeLabels[project.projectType]}
          </Badge>
          {project.featured && <Badge variant="warning">Featured</Badge>}
        </div>
      </div>

      <div className="mt-5 grid gap-5 sm:grid-cols-[minmax(0,1fr)_14rem]">
        <div>
          <h2 className="text-2xl font-black tracking-tight">
            <Link
              href={`/projects/${routeId}`}
              className="hover:text-primary inline-flex min-h-11 items-center"
            >
              {project.title}
            </Link>
          </h2>
          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <div className="border-accent border-l-4 pl-3">
              <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                Problem
              </p>
              <p className="mt-2 line-clamp-3 text-sm leading-relaxed">
                {project.problemStatement}
              </p>
            </div>
            <div className="border-primary border-l-4 pl-3">
              <p className="text-muted-foreground font-mono text-xs font-bold tracking-widest uppercase">
                Proposed solution
              </p>
              <p className="mt-2 line-clamp-3 text-sm leading-relaxed">
                {project.solution}
              </p>
            </div>
          </div>
          <div className="mt-4 flex flex-wrap gap-2">
            {project.categories.map((category) => (
              <Badge key={category} variant="secondary">
                {humanize(category)}
              </Badge>
            ))}
            {project.tags
              ?.split(",")
              .map((tag) => tag.trim())
              .filter(Boolean)
              .slice(0, 3)
              .map((tag) => (
                <Badge key={tag} variant="secondary">
                  {tag}
                </Badge>
              ))}
          </div>
        </div>

        <div className="border-border border p-4">
          <dl className="space-y-3">
            <CompactStat
              icon={<Users className="size-3.5" aria-hidden="true" />}
              label="Supporters"
              value={project.supportersCount}
            />
            <CompactStat
              icon={<Vote className="size-3.5" aria-hidden="true" />}
              label="Votes recorded"
              value={project.votesCount}
            />
          </dl>
          {fundingProgress !== null && project.fundingGoal !== null && (
            <div className="border-border mt-4 border-t pt-4">
              <div className="mb-2 flex justify-between gap-3 font-mono text-xs">
                <span>{formatUSD(project.currentFunding)}</span>
                <span className="text-muted-foreground">
                  {formatUSD(project.fundingGoal)} goal
                </span>
              </div>
              <Progress
                value={fundingProgress}
                variant="neon"
                aria-label={`Funding progress for ${project.title}`}
              />
            </div>
          )}
          <Button asChild variant="outline" className="mt-5 w-full">
            <Link href={`/projects/${routeId}`}>
              Open project
              <ArrowRight className="size-4" />
            </Link>
          </Button>
        </div>
      </div>
    </article>
  );
}

function FilterSelect({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  options: Array<{ value: string; label: string }>;
}) {
  return (
    <label className="text-muted-foreground grid gap-1 text-xs font-semibold">
      <span className="sr-only">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="border-border bg-background text-foreground min-h-11 border px-3 text-sm"
        aria-label={label}
      >
        <option value="all">All</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </label>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4">
      <span className="text-muted-foreground text-sm">{label}</span>
      <span className="font-mono text-sm font-bold">{value}</span>
    </div>
  );
}

function CompactStat({
  icon,
  label,
  value,
}: {
  icon: ReactNode;
  label: string;
  value: number;
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <dt className="text-muted-foreground flex items-center gap-2 text-xs">
        {icon}
        {label}
      </dt>
      <dd className="font-mono text-sm font-bold">{value.toLocaleString()}</dd>
    </div>
  );
}

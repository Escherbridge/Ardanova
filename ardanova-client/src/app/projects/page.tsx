"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";
import Link from "next/link";
import {
  ArrowRight,
  ChevronLeft,
  ChevronRight,
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

const PROJECTS_PAGE_SIZE = 20;

/**
 * search/category/type/status/page are now sent straight to the API (see
 * projectRouter.getAll), so this only adjusts the ordering/visibility of the
 * page that already came back — it never hides a match the server reported.
 */
function applyTabView(projects: Project[], tab: ProjectTab) {
  if (tab === "funded") {
    return projects.filter(
      (project) =>
        project.status === "FUNDED" || project.status === "COMPLETED",
    );
  }
  if (tab === "active") {
    return [...projects].sort(
      (a, b) =>
        new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime(),
    );
  }
  if (tab === "newest") {
    return [...projects].sort(
      (a, b) =>
        new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    );
  }
  return projects;
}

export default function ProjectsPage() {
  const [activeTab, setActiveTab] = useState<ProjectTab>("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedProjectType, setSelectedProjectType] = useState<
    ProjectType | "all"
  >("all");
  const [selectedStatus, setSelectedStatus] = useState<ProjectStatus | "all">(
    "all",
  );
  const [page, setPage] = useState(1);

  // Debounce free-text search so every keystroke doesn't fire a request.
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchQuery), 300);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  // A filter change invalidates whatever page we were on.
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, selectedCategory, selectedProjectType, selectedStatus]);

  const categoryQuery = useEnumOptions("ProjectCategory");
  const projectsQuery = api.project.getAll.useQuery({
    limit: PROJECTS_PAGE_SIZE,
    page,
    search: debouncedSearch.trim() || undefined,
    category: selectedCategory !== "all" ? selectedCategory : undefined,
    projectType:
      selectedProjectType !== "all" ? selectedProjectType : undefined,
    status: selectedStatus !== "all" ? selectedStatus : undefined,
  });
  const projects = useMemo(
    () => projectsQuery.data?.items ?? [],
    [projectsQuery.data?.items],
  );
  const totalCount = projectsQuery.data?.totalCount ?? 0;
  const totalPages = projectsQuery.data?.totalPages ?? 0;

  // If a filter change (or a project disappearing) leaves `page` past the
  // end, clamp it back rather than stranding the view on an empty page.
  useEffect(() => {
    if (totalPages > 0 && page > totalPages) {
      setPage(totalPages);
    }
  }, [totalPages, page]);

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

  const viewProjects = useMemo(
    () => applyTabView(projects, activeTab),
    [projects, activeTab],
  );
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
  const canGoPrevious = page > 1;
  const canGoNext = totalPages > 0 && page < totalPages;

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
              <CardTitle className="text-base">Current page</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 p-4">
              <Stat
                label="Projects on this page"
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
                {totalCount > 0
                  ? `These totals summarize page ${page} of ${totalPages} — ${totalCount} projects match the current filters.`
                  : "These totals summarize this page only."}
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
            <span className="sr-only">Search all projects</span>
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
              Filters run against the full project catalog, not just this page.
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
        ) : projects.length === 0 ? (
          <div className="flex min-h-64 flex-col items-start justify-center gap-4 p-6">
            <FolderKanban
              className="text-muted-foreground size-10"
              aria-hidden="true"
            />
            <div>
              <h2 className="text-2xl font-bold">
                {hasFilters ? "No matches for these filters" : "No projects returned"}
              </h2>
              <p className="text-muted-foreground mt-2 text-sm">
                {hasFilters
                  ? "Change or clear the filters to search a wider set of projects."
                  : "No one has posted a project yet. If you have seen a problem worth solving, define it here so others can help fund and build it."}
              </p>
            </div>
            {hasFilters ? (
              <Button type="button" variant="outline" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : (
              <Button asChild>
                <Link href="/projects/create">Define a project</Link>
              </Button>
            )}
          </div>
        ) : (
          <div>
            {viewProjects.length === 0 ? (
              <div className="text-muted-foreground p-6 text-sm">
                None of the {projects.length} project
                {projects.length === 1 ? "" : "s"} on this page are{" "}
                {activeTab === "funded" ? "funded or complete" : "in this view"}
                . Try another tab or page.
              </div>
            ) : (
              <div className="divide-border divide-y">
                {viewProjects.map((project) => (
                  <ProjectRecord key={project.id} project={project} />
                ))}
              </div>
            )}
            <div className="border-border flex flex-wrap items-center justify-between gap-3 border-t p-4 text-xs">
              <span className="text-muted-foreground">
                {viewProjects.length === projects.length
                  ? `Showing ${projects.length} on this page`
                  : `Showing ${viewProjects.length} of ${projects.length} on this page`}
                {totalCount > 0 ? ` · ${totalCount} total match these filters` : ""}
              </span>
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                  disabled={!canGoPrevious}
                >
                  <ChevronLeft className="size-4" />
                  Prev
                </Button>
                <span className="font-mono">
                  Page {page}
                  {totalPages > 0 ? ` of ${totalPages}` : ""}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setPage((current) => current + 1)}
                  disabled={!canGoNext}
                >
                  Next
                  <ChevronRight className="size-4" />
                </Button>
              </div>
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

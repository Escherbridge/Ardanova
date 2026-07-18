"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Briefcase,
  Plus,
  ArrowUpRight,
  Sparkles,
  TrendingUp,
  DollarSign,
  MapPin,
  Calendar,
  Users,
  SlidersHorizontal,
  X,
  Loader2,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { FeedLayout } from "~/components/layouts/feed-layout";
import { api } from "~/trpc/react";

// Feed tabs for opportunities
const opportunityTabs: { id: string; label: string }[] = [
  { id: "all", label: "All" },
  { id: "bounties", label: "Bounties" },
  { id: "tasks", label: "Task Bounties" },
  { id: "freelance", label: "Freelance" },
  { id: "full-time", label: "Full-time" },
];

// Type badge variants
const typeVariants: Record<
  string,
  "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"
> = {
  Bounty: "neon-green",
  Freelance: "neon",
  "Part-time": "neon-purple",
  "Full-time": "neon-pink",
  Contract: "warning",
};

// Skill badge variants
const skillVariants: Record<
  string,
  "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"
> = {
  React: "neon",
  TypeScript: "neon",
  "Node.js": "neon-green",
  Python: "neon-green",
  "UI/UX": "neon-pink",
  Design: "neon-pink",
  Solidity: "neon-purple",
  Web3: "neon-purple",
  Marketing: "warning",
  Writing: "secondary",
};

// Filter options
const typeFilters = [
  { id: "all", label: "All Types" },
  { id: "Bounty", label: "Bounty" },
  { id: "Freelance", label: "Freelance" },
  { id: "Contract", label: "Contract" },
  { id: "Part-time", label: "Part-time" },
  { id: "Full-time", label: "Full-time" },
];

const skillFilters = [
  { id: "all", label: "All Skills" },
  { id: "React", label: "React" },
  { id: "TypeScript", label: "TypeScript" },
  { id: "Node.js", label: "Node.js" },
  { id: "Python", label: "Python" },
  { id: "UI/UX", label: "UI/UX" },
  { id: "Solidity", label: "Solidity" },
  { id: "Web3", label: "Web3" },
  { id: "Marketing", label: "Marketing" },
];

const compensationFilters = [
  { id: "all", label: "Any Budget" },
  { id: "0-1000", label: "Under $1,000" },
  { id: "1000-5000", label: "$1,000 - $5,000" },
  { id: "5000-10000", label: "$5,000 - $10,000" },
  { id: "10000+", label: "Over $10,000" },
];

const locationFilters = [
  { id: "all", label: "All Locations" },
  { id: "Remote", label: "Remote" },
  { id: "US", label: "United States" },
  { id: "EU", label: "Europe" },
  { id: "Asia", label: "Asia" },
];

const originFilters = [
  { id: "all", label: "All Sources" },
  { id: "TASK_GENERATED", label: "Task Bounties" },
  { id: "TEAM_POSITION", label: "Team Positions" },
];

// Helper to check if opportunity is urgent
function isOpportunityUrgent(
  deadline?: string | null,
  status?: string | null,
): boolean {
  if (!deadline || status !== "OPEN") return false;
  const deadlineDate = new Date(deadline);
  const now = new Date();
  const diffMs = deadlineDate.getTime() - now.getTime();
  const diffDays = diffMs / (1000 * 60 * 60 * 24);
  return diffDays < 3 && diffDays >= 0;
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Yesterday";
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

function formatDeadline(dateString?: string | null): string {
  if (!dateString) return "Open";
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = date.getTime() - now.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay < 0) return "Expired";
  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Tomorrow";
  if (diffDay < 7) return `${diffDay} days left`;
  return `${Math.floor(diffDay / 7)} weeks left`;
}

function formatCompensation(
  amount?: number | null,
  details?: string | null,
): string {
  if (!amount) return "Negotiable";
  const formatted =
    amount >= 1000
      ? `$${(amount / 1000).toFixed(amount % 1000 === 0 ? 0 : 1)}k`
      : `$${amount}`;
  if (details === "hourly") return `${formatted}/hr`;
  if (details === "yearly") return `${formatted}/yr`;
  return formatted;
}

export default function OpportunitiesPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedSkill, setSelectedSkill] = useState("all");
  const [selectedCompensation, setSelectedCompensation] = useState("all");
  const [selectedLocation, setSelectedLocation] = useState("all");
  const [selectedOrigin, setSelectedOrigin] = useState("all");

  // Fetch opportunities from API
  const {
    data: opportunitiesResult,
    isLoading,
    error: opportunitiesError,
  } = api.opportunity.getAll.useQuery({ limit: 100 });
  const allOpportunities = opportunitiesResult?.items ?? [];

  // Filter opportunities based on all criteria
  const filteredOpportunities = allOpportunities.filter((opp) => {
    // Tab filter
    if (activeTab === "bounties" && opp.type !== "Bounty") return false;
    if (activeTab === "tasks" && opp.origin !== "TASK_GENERATED") return false;
    if (
      activeTab === "freelance" &&
      opp.type !== "Freelance" &&
      opp.type !== "Contract"
    )
      return false;
    if (
      activeTab === "full-time" &&
      opp.type !== "Full-time" &&
      opp.type !== "Part-time"
    )
      return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = opp.title.toLowerCase().includes(query);
      const matchesDescription = opp.description.toLowerCase().includes(query);
      const skillsList = opp.skills
        ? opp.skills.split(",").map((s) => s.trim())
        : [];
      const matchesSkills = skillsList.some((s) =>
        s.toLowerCase().includes(query),
      );
      if (!matchesTitle && !matchesDescription && !matchesSkills) return false;
    }

    // Type filter
    if (selectedType !== "all" && opp.type !== selectedType) return false;

    // Skill filter
    if (selectedSkill !== "all") {
      const skillsList = opp.skills
        ? opp.skills.split(",").map((s) => s.trim())
        : [];
      if (!skillsList.includes(selectedSkill)) return false;
    }

    // Origin filter
    if (selectedOrigin !== "all" && opp.origin !== selectedOrigin) return false;

    // Compensation filter
    if (selectedCompensation !== "all" && opp.compensation) {
      const amount = opp.compensation;
      if (selectedCompensation === "0-1000" && amount >= 1000) return false;
      if (
        selectedCompensation === "1000-5000" &&
        (amount < 1000 || amount >= 5000)
      )
        return false;
      if (
        selectedCompensation === "5000-10000" &&
        (amount < 5000 || amount >= 10000)
      )
        return false;
      if (selectedCompensation === "10000+" && amount < 10000) return false;
    }

    // Location filter
    if (selectedLocation !== "all") {
      if (selectedLocation === "Remote" && !opp.isRemote) return false;
      if (
        selectedLocation === "US" &&
        opp.location &&
        !opp.location.includes("CA") &&
        !opp.location.includes("NY") &&
        !opp.location.includes("US")
      )
        return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedType !== "all" ||
    selectedSkill !== "all" ||
    selectedCompensation !== "all" ||
    selectedLocation !== "all" ||
    selectedOrigin !== "all";

  const clearFilters = () => {
    setActiveTab("all");
    setSearchQuery("");
    setSelectedType("all");
    setSelectedSkill("all");
    setSelectedCompensation("all");
    setSelectedLocation("all");
    setSelectedOrigin("all");
  };

  const activeFilterCount =
    (selectedType !== "all" ? 1 : 0) +
    (selectedSkill !== "all" ? 1 : 0) +
    (selectedCompensation !== "all" ? 1 : 0) +
    (selectedLocation !== "all" ? 1 : 0) +
    (selectedOrigin !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: filteredOpportunities.length,
    bounties: filteredOpportunities.filter((o) => o.type === "Bounty").length,
    taskBounties: filteredOpportunities.filter(
      (o) => o.origin === "TASK_GENERATED",
    ).length,
    totalApplicants: filteredOpportunities.reduce(
      (sum, o) => sum + (o.applicationsCount || 0),
      0,
    ),
  };

  // Most active opportunities among the records returned by this query.
  const mostActiveOpportunitiesInView = [...filteredOpportunities]
    .sort((a, b) => (b.applicationsCount || 0) - (a.applicationsCount || 0))
    .slice(0, 3);

  return (
    <FeedLayout
      sidebar={
        <>
          {/* Stats */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Sparkles className="text-warning size-4" />
                Visible results
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Opportunities in this view
                </span>
                <span className="text-foreground font-medium">
                  {opportunitiesError ? "—" : stats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Bounties in this view
                </span>
                <span className="text-success font-medium">
                  {opportunitiesError ? "—" : stats.bounties}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Task-generated in view
                </span>
                <span className="text-system font-medium">
                  {opportunitiesError ? "—" : stats.taskBounties}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Applications in this view
                </span>
                <span className="text-foreground font-medium">
                  {opportunitiesError ? "—" : stats.totalApplicants}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Most active among visible results */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <TrendingUp className="text-primary size-4" />
                Most active in this view
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {mostActiveOpportunitiesInView.map((opp) => (
                <Link
                  key={opp.id}
                  href={`/opportunities/${opp.slug}`}
                  className="block"
                >
                  <div className="mb-1 flex items-start justify-between">
                    <p className="text-foreground hover:text-primary line-clamp-1 text-sm font-medium transition-colors">
                      {opp.title}
                    </p>
                    <Badge
                      variant={
                        opp.type
                          ? (typeVariants[opp.type] ?? "secondary")
                          : "secondary"
                      }
                      size="sm"
                    >
                      {opp.type ?? "Opportunity"}
                    </Badge>
                  </div>
                  <div className="text-muted-foreground flex items-center gap-2 text-xs">
                    <span className="text-success font-medium">
                      {formatCompensation(
                        opp.compensation,
                        opp.compensationDetails,
                      )}
                    </span>
                    <span>·</span>
                    <span>{opp.applicationsCount || 0} applicants</span>
                  </div>
                </Link>
              ))}
            </CardContent>
          </Card>

          {/* Skills Filter */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Filter className="text-system size-4" />
                Skills
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(skillVariants).map((skill) => (
                <button
                  key={skill}
                  type="button"
                  aria-pressed={selectedSkill === skill}
                  onClick={() =>
                    setSelectedSkill(selectedSkill === skill ? "all" : skill)
                  }
                  className="focus-visible:ring-ring focus-visible:ring-2 focus-visible:outline-none"
                >
                  <Badge
                    variant={skillVariants[skill]}
                    size="sm"
                    className="cursor-pointer hover:opacity-80"
                  >
                    {skill}
                  </Badge>
                </button>
              ))}
            </CardContent>
          </Card>

          {/* Footer */}
          <div className="text-muted-foreground px-2 text-xs">
            <p>&copy; 2026 ArdaNova</p>
            <p className="mt-1">Counts reflect the current filtered view.</p>
          </div>
        </>
      }
    >
      {/* Header */}
      <div className="border-border bg-background relative z-10 border-b-2">
        <div className="flex items-center justify-between p-4">
          <h1 className="text-foreground flex items-center gap-2 text-xl font-bold">
            <Briefcase className="text-warning size-5" />
            Opportunities
          </h1>
          <Button variant="neon" size="sm" asChild>
            <Link href="/opportunities/create">
              <Plus className="mr-2 size-4" />
              Post Job
            </Link>
          </Button>
        </div>

        {/* Search */}
        <div className="flex flex-col gap-2 px-4 pb-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <label htmlFor="opportunities-search" className="sr-only">
              Search opportunities
            </label>
            <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
            <input
              type="text"
              id="opportunities-search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search opportunities..."
              className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary w-full border-2 py-2 pr-4 pl-10 text-sm focus:outline-none"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery("")}
                aria-label="Clear opportunity search"
                className="text-muted-foreground hover:text-foreground absolute top-1/2 right-0 flex min-h-11 min-w-11 -translate-y-1/2 items-center justify-center"
              >
                <X className="size-4" />
              </button>
            )}
          </div>
          <Button
            variant={showFilters ? "neon" : "outline"}
            size="sm"
            onClick={() => setShowFilters(!showFilters)}
            aria-expanded={showFilters}
            aria-controls="opportunity-filters"
            className="w-full gap-1.5 sm:w-auto"
          >
            <SlidersHorizontal className="size-4" />
            Filters
            {activeFilterCount > 0 && !showFilters && (
              <Badge variant="neon" size="sm" className="ml-1">
                {activeFilterCount}
              </Badge>
            )}
          </Button>
        </div>

        {/* Expanded Filters */}
        {showFilters && (
          <div
            id="opportunity-filters"
            className="border-border space-y-3 border-t px-4 pt-3 pb-4"
          >
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {/* Type Filter */}
              <div>
                <label
                  htmlFor="opportunities-type-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Job Type
                </label>
                <select
                  id="opportunities-type-filter"
                  value={selectedType}
                  onChange={(e) => setSelectedType(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {typeFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Skill Filter */}
              <div>
                <label
                  htmlFor="opportunities-skill-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Required Skill
                </label>
                <select
                  id="opportunities-skill-filter"
                  value={selectedSkill}
                  onChange={(e) => setSelectedSkill(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {skillFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Origin Filter */}
              <div>
                <label
                  htmlFor="opportunities-origin-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Origin
                </label>
                <select
                  id="opportunities-origin-filter"
                  value={selectedOrigin}
                  onChange={(e) => setSelectedOrigin(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {originFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {/* Compensation Filter */}
              <div>
                <label
                  htmlFor="opportunities-budget-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Budget Range
                </label>
                <select
                  id="opportunities-budget-filter"
                  value={selectedCompensation}
                  onChange={(e) => setSelectedCompensation(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {compensationFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Location Filter */}
              <div>
                <label
                  htmlFor="opportunities-location-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Location
                </label>
                <select
                  id="opportunities-location-filter"
                  value={selectedLocation}
                  onChange={(e) => setSelectedLocation(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {locationFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {hasActiveFilters && (
              <div className="flex flex-col gap-3 pt-2 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex flex-wrap gap-2">
                  {searchQuery && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      Search: {searchQuery}
                      <button
                        type="button"
                        aria-label="Remove search filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSearchQuery("")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedType !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {typeFilters.find((f) => f.id === selectedType)?.label}
                      <button
                        type="button"
                        aria-label="Remove job type filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedType("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedSkill !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {skillFilters.find((f) => f.id === selectedSkill)?.label}
                      <button
                        type="button"
                        aria-label="Remove skill filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedSkill("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedCompensation !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        compensationFilters.find(
                          (f) => f.id === selectedCompensation,
                        )?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove budget filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedCompensation("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedLocation !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        locationFilters.find((f) => f.id === selectedLocation)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove location filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedLocation("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedOrigin !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        originFilters.find((f) => f.id === selectedOrigin)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove origin filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedOrigin("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearFilters}
                  className="text-muted-foreground"
                >
                  Clear all
                </Button>
              </div>
            )}
          </div>
        )}

        {/* Tabs */}
        <div
          className="border-border flex overflow-x-auto border-b-2"
          role="tablist"
          aria-label="Opportunity scope"
          onKeyDown={handleTabListKeyDown}
        >
          {opportunityTabs.map((tab) => {
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                role="tab"
                id={`opportunities-tab-${tab.id}`}
                aria-selected={activeTab === tab.id}
                aria-controls={`opportunities-panel-${tab.id}`}
                tabIndex={activeTab === tab.id ? 0 : -1}
                aria-label={tab.label}
                className={cn(
                  "relative flex min-h-11 min-w-24 flex-1 items-center justify-center gap-2 px-3 py-3 text-sm font-medium whitespace-nowrap transition-colors",
                  activeTab === tab.id
                    ? "text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-card",
                )}
              >
                <span className="hidden sm:inline">{tab.label}</span>
                {activeTab === tab.id && (
                  <span className="bg-primary absolute right-0 bottom-0 left-0 h-0.5" />
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Opportunities Feed */}
      <div
        role="tabpanel"
        id={`opportunities-panel-${activeTab}`}
        aria-labelledby={`opportunities-tab-${activeTab}`}
      >
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <Loader2 className="text-primary size-8 animate-spin" />
          </div>
        ) : opportunitiesError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 border-b-2 px-4 py-10 text-center"
          >
            <p className="text-destructive font-medium">
              Opportunities could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No opportunity counts or results are shown.
            </p>
          </div>
        ) : filteredOpportunities.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-4 py-20">
            <Briefcase className="text-muted-foreground mb-4 size-12" />
            <p className="text-foreground text-lg font-medium">
              No opportunities found
            </p>
            <p className="text-muted-foreground mt-1">
              {allOpportunities.length > 0
                ? "No opportunities match the current view."
                : "No opportunity records were returned."}
            </p>
            {allOpportunities.length > 0 ? (
              <Button variant="outline" className="mt-4" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : (
              <Button variant="neon" className="mt-4" asChild>
                <Link href="/opportunities/create">
                  <Plus className="mr-2 size-4" />
                  Post opportunity
                </Link>
              </Button>
            )}
          </div>
        ) : (
          filteredOpportunities.map((opp) => {
            const skillsList = opp.skills
              ? opp.skills.split(",").map((s) => s.trim())
              : [];
            const isUrgent = isOpportunityUrgent(opp.deadline, opp.status);
            const posterName = opp.poster?.name ?? "Unknown";
            const posterImage = opp.poster?.image ?? undefined;

            return (
              <article
                key={opp.id}
                className="border-border bg-card hover:bg-card/80 border-b-2 transition-colors"
              >
                <div className="p-4">
                  {/* Header */}
                  <div className="flex items-start gap-3">
                    <Link
                      href={`/dashboard/profile/${opp.posterId}`}
                      className="shrink-0"
                      aria-label={`View ${posterName}'s profile`}
                    >
                      <Avatar className="border-border hover:border-primary size-10 border-2 transition-colors">
                        <AvatarImage src={posterImage} />
                        <AvatarFallback>{posterName.charAt(0)}</AvatarFallback>
                      </Avatar>
                    </Link>

                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <Link
                          href={`/dashboard/profile/${opp.posterId}`}
                          className="text-foreground hover:text-primary font-medium transition-colors"
                        >
                          {posterName}
                        </Link>
                        <span className="text-muted-foreground text-sm">·</span>
                        <span className="text-muted-foreground text-sm">
                          {formatRelativeTime(opp.createdAt)}
                        </span>
                      </div>
                      <p className="text-muted-foreground mt-0.5 text-sm">
                        posted a new opportunity
                      </p>
                    </div>

                    <div className="flex items-center gap-2">
                      {isUrgent && (
                        <Badge variant="destructive" size="sm">
                          Urgent
                        </Badge>
                      )}
                      <Badge
                        variant={
                          opp.type
                            ? (typeVariants[opp.type] ?? "secondary")
                            : "secondary"
                        }
                        size="sm"
                      >
                        {opp.type ?? "Opportunity"}
                      </Badge>
                    </div>
                  </div>

                  {/* Opportunity Content */}
                  <Link
                    href={`/opportunities/${opp.slug}`}
                    className="mt-3 block pl-13"
                  >
                    <h3 className="text-foreground hover:text-primary text-lg font-semibold transition-colors">
                      {opp.title}
                    </h3>
                    <p className="text-foreground mt-2 line-clamp-2">
                      {opp.description}
                    </p>

                    {/* Skills */}
                    {skillsList.length > 0 && (
                      <div className="mt-3 flex flex-wrap gap-2">
                        {skillsList.map((skill, i) => (
                          <Badge
                            key={i}
                            variant={skillVariants[skill] ?? "secondary"}
                            size="sm"
                          >
                            {skill}
                          </Badge>
                        ))}
                      </div>
                    )}

                    {/* Meta Info */}
                    <div className="text-muted-foreground mt-3 flex flex-wrap items-center gap-4 text-sm">
                      <div className="text-success flex items-center gap-1 font-medium">
                        <DollarSign className="size-4" />
                        <span>
                          {formatCompensation(
                            opp.compensation,
                            opp.compensationDetails,
                          )}
                        </span>
                      </div>
                      {(opp.location || opp.isRemote) && (
                        <div className="flex items-center gap-1">
                          <MapPin className="size-4" />
                          <span>{opp.isRemote ? "Remote" : opp.location}</span>
                        </div>
                      )}
                      <div className="flex items-center gap-1">
                        <Calendar className="size-4" />
                        <span>{formatDeadline(opp.deadline)}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Users className="size-4" />
                        <span>{opp.applicationsCount || 0} applicants</span>
                      </div>
                    </div>
                  </Link>

                  <div className="mt-4 flex justify-end pl-13">
                    <Button variant="outline" size="sm" asChild>
                      <Link href={`/opportunities/${opp.slug}`}>
                        View opportunity
                        <ArrowUpRight className="ml-2 size-4" />
                      </Link>
                    </Button>
                  </div>
                </div>
              </article>
            );
          })
        )}
      </div>
    </FeedLayout>
  );
}

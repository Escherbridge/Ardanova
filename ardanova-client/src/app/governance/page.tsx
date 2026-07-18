"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Vote,
  ArrowUpRight,
  Sparkles,
  Timer,
  ThumbsUp,
  ThumbsDown,
  Users,
  Scale,
  SlidersHorizontal,
  X,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import { cn } from "~/lib/utils";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { api } from "~/trpc/react";
import { FeedLayout } from "~/components/layouts/feed-layout";
import type { Proposal as ApiProposal } from "~/lib/api/ardanova/endpoints/governance";

// Types for display
interface DisplayProposal {
  id: string;
  title: string;
  type: string;
  status: string;
  project: { id: string; name: string; slug: string };
  creator: { id: string; name: string; avatar?: string };
  description: string;
  options: Array<{ label: string; votes: number; percentage: number }>;
  quorum: number;
  currentQuorum: number;
  threshold: number;
  totalVotes: number;
  totalVotingPower: number;
  votingEnds: Date;
  createdAt: Date;
}

// Transform API proposal to display format
interface ProposalWithRelations extends ApiProposal {
  votesCount?: number;
  totalVotingPower?: number;
  project?: {
    id?: string | null;
    title?: string | null;
    slug?: string | null;
  };
  creator?: {
    id?: string | null;
    name?: string | null;
    image?: string | null;
  };
}

function transformProposal(
  apiProposal: ProposalWithRelations,
): DisplayProposal {
  // Parse options from JSON string
  let options = [
    { label: "For", votes: 0, percentage: 0 },
    { label: "Against", votes: 0, percentage: 0 },
    { label: "Abstain", votes: 0, percentage: 0 },
  ];
  try {
    const parsed: unknown = JSON.parse(apiProposal.options || "[]");
    if (Array.isArray(parsed) && parsed.length > 0) {
      const totalVotes = apiProposal.votesCount || 1;
      options = parsed.map((option) => {
        const record =
          typeof option === "object" && option !== null
            ? (option as Record<string, unknown>)
            : {};
        const votes = typeof record.votes === "number" ? record.votes : 0;
        return {
          label: typeof record.label === "string" ? record.label : "Option",
          votes,
          percentage:
            totalVotes > 0 ? Math.round((votes / totalVotes) * 100) : 0,
        };
      });
    }
  } catch {}

  // Calculate current quorum percentage
  const totalVotingPower = apiProposal.totalVotingPower ?? 0;
  const votesCount = apiProposal.votesCount ?? 0;
  const currentQuorum =
    totalVotingPower > 0
      ? Math.round((votesCount / totalVotingPower) * 100)
      : 0;

  return {
    id: apiProposal.id,
    title: apiProposal.title,
    type: apiProposal.type,
    status: apiProposal.status,
    project: {
      id: apiProposal.project?.id || apiProposal.projectId,
      name: apiProposal.project?.title || "Project",
      slug: apiProposal.project?.slug || "",
    },
    creator: {
      id: apiProposal.creator?.id || apiProposal.creatorId,
      name: apiProposal.creator?.name || "Unknown",
      avatar: apiProposal.creator?.image ?? undefined,
    },
    description: apiProposal.description,
    options,
    quorum: apiProposal.quorum,
    currentQuorum,
    threshold: apiProposal.threshold,
    totalVotes: votesCount,
    totalVotingPower: Math.round(totalVotingPower),
    votingEnds: apiProposal.votingEnd
      ? new Date(apiProposal.votingEnd)
      : new Date(),
    createdAt: new Date(apiProposal.createdAt),
  };
}

// Feed tabs for governance
const governanceTabs: { id: string; label: string }[] = [
  { id: "all", label: "All" },
  { id: "active", label: "Active" },
  { id: "passed", label: "Passed" },
  { id: "rejected", label: "Rejected" },
];

// Proposal type variants
const typeVariants: Record<
  string,
  | "neon"
  | "neon-pink"
  | "neon-green"
  | "neon-purple"
  | "warning"
  | "secondary"
  | "destructive"
> = {
  Treasury: "neon-green",
  Governance: "neon-purple",
  Strategic: "neon",
  Operational: "secondary",
  Emergency: "destructive",
  Constitutional: "warning",
  Token: "neon-pink",
};

// Status variants
const statusVariants: Record<
  string,
  | "neon"
  | "neon-pink"
  | "neon-green"
  | "neon-purple"
  | "warning"
  | "secondary"
  | "destructive"
> = {
  Active: "neon",
  Passed: "neon-green",
  Rejected: "destructive",
  Executed: "neon-purple",
  Expired: "secondary",
  Draft: "secondary",
};

// Filter options
const proposalTypeFilters = [
  { id: "all", label: "All Types" },
  { id: "Treasury", label: "Treasury" },
  { id: "Governance", label: "Governance" },
  { id: "Strategic", label: "Strategic" },
  { id: "Operational", label: "Operational" },
  { id: "Emergency", label: "Emergency" },
  { id: "Constitutional", label: "Constitutional" },
  { id: "Token", label: "Token" },
];

const proposalStatusFilters = [
  { id: "all", label: "All Statuses" },
  { id: "Active", label: "Active" },
  { id: "Passed", label: "Passed" },
  { id: "Rejected", label: "Rejected" },
  { id: "Executed", label: "Executed" },
  { id: "Expired", label: "Expired" },
];

const quorumFilters = [
  { id: "all", label: "Any Quorum" },
  { id: "reached", label: "Quorum Reached" },
  { id: "not-reached", label: "Below Quorum" },
];

const timeFilters = [
  { id: "all", label: "All Time" },
  { id: "day", label: "Ending in 24h" },
  { id: "week", label: "Ending in 7 days" },
  { id: "month", label: "Created this month" },
];

function formatTimeRemaining(date: Date): string {
  const now = new Date();
  const diffMs = date.getTime() - now.getTime();

  if (diffMs < 0) return "Ended";

  const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
  const diffDays = Math.floor(diffHours / 24);

  if (diffDays > 0) return `${diffDays}d ${diffHours % 24}h left`;
  if (diffHours > 0) return `${diffHours}h left`;
  return "< 1h left";
}

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffDay = Math.floor(diffMs / (1000 * 60 * 60 * 24));

  if (diffDay === 0) return "Today";
  if (diffDay === 1) return "Yesterday";
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

export default function GovernancePage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [selectedQuorum, setSelectedQuorum] = useState("all");
  const [selectedTime, setSelectedTime] = useState("all");

  // Debounce search query
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchQuery);
    }, 300);

    return () => clearTimeout(timer);
  }, [searchQuery]);

  // Fetch proposals from API with debounced search
  const {
    data: proposalsResult,
    isLoading,
    error: proposalsError,
  } = api.governance.getAll.useQuery({
    search: debouncedSearch || undefined,
    type:
      selectedType !== "all"
        ? (selectedType as
            | "Treasury"
            | "Governance"
            | "Strategic"
            | "Operational"
            | "Emergency"
            | "Constitutional"
            | "Token")
        : undefined,
    status:
      selectedStatus !== "all"
        ? (selectedStatus as
            | "Draft"
            | "Active"
            | "Passed"
            | "Rejected"
            | "Executed"
            | "Expired")
        : undefined,
    limit: 50,
    page: 1,
  });

  // Transform API proposals to display format
  const proposals = (proposalsResult?.items || []).map((proposal) =>
    transformProposal(proposal as ProposalWithRelations),
  );

  // Filter proposals based on client-side criteria (tab, quorum, time)
  const filteredProposals = proposals.filter((prop) => {
    // Tab filter
    if (activeTab === "active" && prop.status !== "Active") return false;
    if (
      activeTab === "passed" &&
      prop.status !== "Passed" &&
      prop.status !== "Executed"
    )
      return false;
    if (
      activeTab === "rejected" &&
      prop.status !== "Rejected" &&
      prop.status !== "Expired"
    )
      return false;

    // Quorum filter
    if (selectedQuorum !== "all") {
      const quorumReached = prop.currentQuorum >= prop.quorum;
      if (selectedQuorum === "reached" && !quorumReached) return false;
      if (selectedQuorum === "not-reached" && quorumReached) return false;
    }

    // Time filter
    if (selectedTime !== "all" && prop.status === "Active") {
      const now = new Date();
      const endTime = new Date(prop.votingEnds);
      const hoursRemaining =
        (endTime.getTime() - now.getTime()) / (1000 * 60 * 60);

      if (selectedTime === "day" && hoursRemaining > 24) return false;
      if (selectedTime === "week" && hoursRemaining > 168) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedType !== "all" ||
    selectedStatus !== "all" ||
    selectedQuorum !== "all" ||
    selectedTime !== "all";

  const clearFilters = () => {
    setActiveTab("all");
    setSearchQuery("");
    setSelectedType("all");
    setSelectedStatus("all");
    setSelectedQuorum("all");
    setSelectedTime("all");
  };

  const activeFilterCount =
    (selectedType !== "all" ? 1 : 0) +
    (selectedStatus !== "all" ? 1 : 0) +
    (selectedQuorum !== "all" ? 1 : 0) +
    (selectedTime !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: filteredProposals.length,
    active: filteredProposals.filter((p) => p.status === "Active").length,
    passed: filteredProposals.filter(
      (p) => p.status === "Passed" || p.status === "Executed",
    ).length,
    totalVotes: filteredProposals.reduce((sum, p) => sum + p.totalVotes, 0),
  };

  // Active proposals among the records returned by this query.
  const activeProposalsInView = filteredProposals
    .filter((p) => p.status === "Active")
    .slice(0, 3);

  return (
    <FeedLayout
      sidebar={
        <>
          {/* Stats */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Sparkles className="text-neon-yellow size-4" />
                Visible results
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Proposals in this view
                </span>
                <span className="text-foreground font-medium">
                  {proposalsError ? "—" : stats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Active in this view
                </span>
                <span className="text-neon font-medium">
                  {proposalsError ? "—" : stats.active}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Passed in this view
                </span>
                <span className="text-neon-green font-medium">
                  {proposalsError ? "—" : stats.passed}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Votes recorded in this view
                </span>
                <span className="text-foreground font-medium">
                  {proposalsError ? "—" : stats.totalVotes}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Active proposals among visible results */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Timer className="text-warning size-4" />
                Active in this view
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {activeProposalsInView.map((proposal) => (
                <Link
                  key={proposal.id}
                  href={`/governance/${proposal.id}`}
                  className="block"
                >
                  <p className="text-foreground hover:text-primary line-clamp-1 text-sm font-medium transition-colors">
                    {proposal.title}
                  </p>
                  <div className="text-muted-foreground mt-1 flex items-center gap-2 text-xs">
                    <Badge
                      variant={typeVariants[proposal.type] ?? "secondary"}
                      size="sm"
                    >
                      {proposal.type}
                    </Badge>
                    <span className="text-warning">
                      {formatTimeRemaining(proposal.votingEnds)}
                    </span>
                  </div>
                </Link>
              ))}
            </CardContent>
          </Card>

          {/* Proposal Types */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Filter className="text-neon-pink size-4" />
                Proposal Types
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(typeVariants).map((type) => (
                <button
                  key={type}
                  type="button"
                  aria-pressed={selectedType === type}
                  onClick={() =>
                    setSelectedType(selectedType === type ? "all" : type)
                  }
                  className="focus-visible:ring-ring min-h-11 focus-visible:ring-2 focus-visible:outline-none"
                >
                  <Badge
                    variant={typeVariants[type]}
                    size="sm"
                    className="cursor-pointer hover:opacity-80"
                  >
                    {type}
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
        <div className="flex flex-col gap-2 p-4 sm:flex-row sm:items-center sm:justify-between">
          <h1 className="text-foreground flex items-center gap-2 text-xl font-bold">
            <Vote className="text-neon-purple size-5" />
            Governance
          </h1>
          <div className="text-muted-foreground text-sm">
            Proposals are created within projects
          </div>
        </div>

        {/* Search */}
        <div className="flex flex-col gap-2 px-4 pb-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <label htmlFor="governance-search" className="sr-only">
              Search proposals
            </label>
            <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
            <input
              type="text"
              id="governance-search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search proposals..."
              className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary w-full border-2 py-2 pr-4 pl-10 text-sm focus:outline-none"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery("")}
                aria-label="Clear proposal search"
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
            aria-controls="governance-filters"
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
            id="governance-filters"
            className="border-border space-y-3 border-t px-4 pt-3 pb-4"
          >
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {/* Type Filter */}
              <div>
                <label
                  htmlFor="governance-type-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Proposal Type
                </label>
                <select
                  id="governance-type-filter"
                  value={selectedType}
                  onChange={(e) => setSelectedType(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {proposalTypeFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Status Filter */}
              <div>
                <label
                  htmlFor="governance-status-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Status
                </label>
                <select
                  id="governance-status-filter"
                  value={selectedStatus}
                  onChange={(e) => setSelectedStatus(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {proposalStatusFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Quorum Filter */}
              <div>
                <label
                  htmlFor="governance-quorum-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Quorum Status
                </label>
                <select
                  id="governance-quorum-filter"
                  value={selectedQuorum}
                  onChange={(e) => setSelectedQuorum(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {quorumFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Time Filter */}
              <div>
                <label
                  htmlFor="governance-time-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Time Period
                </label>
                <select
                  id="governance-time-filter"
                  value={selectedTime}
                  onChange={(e) => setSelectedTime(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {timeFilters.map((filter) => (
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
                      {
                        proposalTypeFilters.find((f) => f.id === selectedType)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove proposal type filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedType("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedStatus !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        proposalStatusFilters.find(
                          (f) => f.id === selectedStatus,
                        )?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove status filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedStatus("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedQuorum !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        quorumFilters.find((f) => f.id === selectedQuorum)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove quorum filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedQuorum("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedTime !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {timeFilters.find((f) => f.id === selectedTime)?.label}
                      <button
                        type="button"
                        aria-label="Remove time filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedTime("all")}
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
          aria-label="Proposal scope"
          onKeyDown={handleTabListKeyDown}
        >
          {governanceTabs.map((tab) => {
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                role="tab"
                id={`governance-tab-${tab.id}`}
                aria-selected={activeTab === tab.id}
                aria-controls={`governance-panel-${tab.id}`}
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

      {/* Proposals Feed */}
      <div
        role="tabpanel"
        id={`governance-panel-${activeTab}`}
        aria-labelledby={`governance-tab-${activeTab}`}
      >
        {isLoading ? (
          <div className="flex items-center justify-center py-20">
            <div className="border-primary size-8 animate-spin rounded-full border-2 border-t-transparent" />
          </div>
        ) : proposalsError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 border-b-2 px-4 py-10 text-center"
          >
            <p className="text-destructive font-medium">
              Governance proposals could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No proposal or vote counts are shown.
            </p>
          </div>
        ) : filteredProposals.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-4 py-20">
            <Vote className="text-muted-foreground mb-4 size-12" />
            <p className="text-foreground text-lg font-medium">
              No proposals found
            </p>
            <p className="text-muted-foreground mt-1">
              {hasActiveFilters || activeTab !== "all"
                ? "No proposals match the current view."
                : "No proposal records were returned."}
            </p>
            {hasActiveFilters || activeTab !== "all" ? (
              <Button variant="outline" className="mt-4" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : (
              <Button variant="neon" className="mt-4" asChild>
                <Link href="/projects">Browse projects</Link>
              </Button>
            )}
          </div>
        ) : (
          filteredProposals.map((proposal) => (
            <article
              key={proposal.id}
              className="border-border bg-card hover:bg-card/80 border-b-2 transition-colors"
            >
              <div className="p-4">
                {/* Header */}
                <div className="flex items-start gap-3">
                  <Link
                    href={`/dashboard/profile/${proposal.creator.id}`}
                    className="shrink-0"
                    aria-label={`View ${proposal.creator.name}'s profile`}
                  >
                    <Avatar className="border-border hover:border-primary size-10 border-2 transition-colors">
                      <AvatarImage src={proposal.creator.avatar} />
                      <AvatarFallback>
                        {proposal.creator.name.charAt(0)}
                      </AvatarFallback>
                    </Avatar>
                  </Link>

                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link
                        href={`/dashboard/profile/${proposal.creator.id}`}
                        className="text-foreground hover:text-primary font-medium transition-colors"
                      >
                        {proposal.creator.name}
                      </Link>
                      <span className="text-muted-foreground text-sm">·</span>
                      {proposal.project.slug ? (
                        <Link
                          href={`/projects/${proposal.project.slug}`}
                          className="text-muted-foreground hover:text-primary text-sm transition-colors"
                        >
                          {proposal.project.name}
                        </Link>
                      ) : (
                        <span className="text-muted-foreground text-sm">
                          {proposal.project.name}
                        </span>
                      )}
                      <span className="text-muted-foreground text-sm">·</span>
                      <span className="text-muted-foreground text-sm">
                        {formatRelativeTime(proposal.createdAt)}
                      </span>
                    </div>
                    <p className="text-muted-foreground mt-0.5 text-sm">
                      created a proposal
                    </p>
                  </div>

                  <div className="flex items-center gap-2">
                    <Badge
                      variant={typeVariants[proposal.type] ?? "secondary"}
                      size="sm"
                    >
                      {proposal.type}
                    </Badge>
                    <Badge
                      variant={statusVariants[proposal.status] ?? "secondary"}
                      size="sm"
                    >
                      {proposal.status}
                    </Badge>
                  </div>
                </div>

                {/* Proposal Content */}
                <Link
                  href={`/governance/${proposal.id}`}
                  className="mt-3 block pl-13"
                >
                  <h3 className="text-foreground hover:text-primary text-lg font-semibold transition-colors">
                    {proposal.title}
                  </h3>
                  <p className="text-foreground mt-2 line-clamp-2">
                    {proposal.description}
                  </p>

                  {/* Voting Results */}
                  <div className="mt-4 space-y-2">
                    {proposal.options.map((option, i) => (
                      <div key={i} className="space-y-1">
                        <div className="flex items-center justify-between text-sm">
                          <div className="flex items-center gap-2">
                            {option.label === "For" && (
                              <ThumbsUp className="text-neon-green size-3" />
                            )}
                            {option.label === "Against" && (
                              <ThumbsDown className="text-destructive size-3" />
                            )}
                            <span className="text-foreground">
                              {option.label}
                            </span>
                          </div>
                          <span className="text-muted-foreground">
                            {option.percentage}%
                          </span>
                        </div>
                        <Progress
                          value={option.percentage}
                          variant={
                            option.label === "For"
                              ? "success"
                              : option.label === "Against"
                                ? "warning"
                                : "default"
                          }
                          className="h-2"
                        />
                      </div>
                    ))}
                  </div>

                  {/* Meta Info */}
                  <div className="text-muted-foreground mt-4 flex flex-wrap items-center gap-4 text-sm">
                    <div className="flex items-center gap-1">
                      <Users className="size-4" />
                      <span>
                        {proposal.totalVotes} votes ·{" "}
                        {proposal.totalVotingPower} total voting power
                      </span>
                    </div>
                    <div className="flex items-center gap-1">
                      <Scale className="size-4" />
                      <span>
                        Quorum: {proposal.currentQuorum}% / {proposal.quorum}%
                      </span>
                    </div>
                    {proposal.status === "Active" && (
                      <div className="text-warning flex items-center gap-1">
                        <Timer className="size-4" />
                        <span>{formatTimeRemaining(proposal.votingEnds)}</span>
                      </div>
                    )}
                  </div>
                </Link>

                <div className="mt-4 flex justify-end pl-13">
                  <Button variant="outline" size="sm" asChild>
                    <Link href={`/governance/${proposal.id}`}>
                      Open proposal
                      <ArrowUpRight className="ml-2 size-4" />
                    </Link>
                  </Button>
                </div>
              </div>
            </article>
          ))
        )}
      </div>
    </FeedLayout>
  );
}

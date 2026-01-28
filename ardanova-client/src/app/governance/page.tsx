"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Vote,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
  CheckCircle2,
  XCircle,
  Timer,
  ThumbsUp,
  ThumbsDown,
  Users,
  TrendingUp,
  FileText,
  Scale,
  SlidersHorizontal,
  X,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

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
  totalVoters: number;
  votingEnds: Date;
  createdAt: Date;
}

// Transform API proposal to display format
function transformProposal(apiProposal: any): DisplayProposal {
  // Parse options from JSON string
  let options = [
    { label: "For", votes: 0, percentage: 0 },
    { label: "Against", votes: 0, percentage: 0 },
    { label: "Abstain", votes: 0, percentage: 0 },
  ];
  try {
    const parsed = JSON.parse(apiProposal.options || "[]");
    if (Array.isArray(parsed) && parsed.length > 0) {
      const totalVotes = apiProposal.votesCount || 1;
      options = parsed.map((opt: any) => ({
        label: opt.label || "Option",
        votes: opt.votes || 0,
        percentage: totalVotes > 0 ? Math.round(((opt.votes || 0) / totalVotes) * 100) : 0,
      }));
    }
  } catch {}

  // Calculate current quorum percentage
  const currentQuorum = apiProposal.totalVotingPower > 0
    ? Math.round((apiProposal.votesCount / apiProposal.totalVotingPower) * 100)
    : 0;

  return {
    id: apiProposal.id,
    title: apiProposal.title,
    type: apiProposal.type,
    status: apiProposal.status,
    project: {
      id: apiProposal.project?.id || "",
      name: apiProposal.project?.title || "Unknown Project",  // Map title -> name
      slug: apiProposal.project?.slug || apiProposal.project?.id || "",
    },
    creator: {
      id: apiProposal.creator?.id || apiProposal.creatorId,
      name: apiProposal.creator?.name || "Unknown",
      avatar: apiProposal.creator?.image,  // Map image -> avatar
    },
    description: apiProposal.description,
    options,
    quorum: apiProposal.quorum,
    currentQuorum,
    threshold: apiProposal.threshold,
    totalVotes: apiProposal.votesCount,  // Map votesCount -> totalVotes
    totalVoters: Math.round(apiProposal.totalVotingPower),  // Map totalVotingPower -> totalVoters
    votingEnds: apiProposal.votingEnd ? new Date(apiProposal.votingEnd) : new Date(),
    createdAt: new Date(apiProposal.createdAt),
  };
}

// Feed tabs for governance
const governanceTabs = [
  { id: "all", label: "All", icon: Vote },
  { id: "active", label: "Active", icon: Timer },
  { id: "passed", label: "Passed", icon: CheckCircle2 },
  { id: "rejected", label: "Rejected", icon: XCircle },
];

// Proposal type variants
const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  "Treasury": "neon-green",
  "Governance": "neon-purple",
  "Strategic": "neon",
  "Operational": "secondary",
  "Emergency": "destructive",
  "Constitutional": "warning",
  "Token": "neon-pink",
};

// Status variants
const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  "Active": "neon",
  "Passed": "neon-green",
  "Rejected": "destructive",
  "Executed": "neon-purple",
  "Expired": "secondary",
  "Draft": "secondary",
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
  const { data: proposalsResult, isLoading } = api.governance.getAll.useQuery({
    search: debouncedSearch || undefined,
    type: selectedType !== "all" ? selectedType : undefined,
    status: selectedStatus !== "all" ? selectedStatus : undefined,
    limit: 50,
    page: 1,
  });

  // Transform API proposals to display format
  const proposals = (proposalsResult?.items || []).map(transformProposal);

  // Filter proposals based on client-side criteria (tab, quorum, time)
  const filteredProposals = proposals.filter((prop) => {
    // Tab filter
    if (activeTab === "active" && prop.status !== "Active") return false;
    if (activeTab === "passed" && prop.status !== "Passed" && prop.status !== "Executed") return false;
    if (activeTab === "rejected" && prop.status !== "Rejected" && prop.status !== "Expired") return false;

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
      const hoursRemaining = (endTime.getTime() - now.getTime()) / (1000 * 60 * 60);

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
    total: proposals.length,
    active: proposals.filter((p) => p.status === "Active").length,
    passed: proposals.filter((p) => p.status === "Passed" || p.status === "Executed").length,
    totalVotes: proposals.reduce((sum, p) => sum + p.totalVotes, 0),
  };

  // Active proposals for sidebar
  const activeProposals = proposals
    .filter((p) => p.status === "Active")
    .slice(0, 3);

  return (
    <div className="min-h-screen bg-background">
      <div className="flex justify-center">
        {/* Main Feed Column - Centered */}
        <div className="w-full max-w-2xl border-x-2 border-border">
          {/* Header */}
          <div className="sticky top-0 z-10 bg-background/95 backdrop-blur border-b-2 border-border">
            <div className="p-4 flex items-center justify-between">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <Vote className="size-5 text-neon-purple" />
                Governance
              </h1>
              <div className="text-sm text-muted-foreground">
                Proposals are created within projects
              </div>
            </div>

            {/* Search */}
            <div className="px-4 pb-3 flex items-center gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Search proposals..."
                  className="w-full pl-10 pr-4 py-2 bg-card border-2 border-border text-foreground text-sm placeholder:text-muted-foreground focus:border-primary focus:outline-none"
                />
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery("")}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  >
                    <X className="size-4" />
                  </button>
                )}
              </div>
              <Button
                variant={showFilters ? "neon" : "outline"}
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                className="gap-1.5"
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
              <div className="px-4 pb-4 space-y-3 border-t border-border pt-3">
                <div className="grid grid-cols-2 gap-3">
                  {/* Type Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Proposal Type
                    </label>
                    <select
                      value={selectedType}
                      onChange={(e) => setSelectedType(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Status
                    </label>
                    <select
                      value={selectedStatus}
                      onChange={(e) => setSelectedStatus(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Quorum Status
                    </label>
                    <select
                      value={selectedQuorum}
                      onChange={(e) => setSelectedQuorum(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Time Period
                    </label>
                    <select
                      value={selectedTime}
                      onChange={(e) => setSelectedTime(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                  <div className="flex items-center justify-between pt-2">
                    <div className="flex flex-wrap gap-2">
                      {searchQuery && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          Search: {searchQuery}
                          <button onClick={() => setSearchQuery("")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedType !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {proposalTypeFilters.find((f) => f.id === selectedType)?.label}
                          <button onClick={() => setSelectedType("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedStatus !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {proposalStatusFilters.find((f) => f.id === selectedStatus)?.label}
                          <button onClick={() => setSelectedStatus("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedQuorum !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {quorumFilters.find((f) => f.id === selectedQuorum)?.label}
                          <button onClick={() => setSelectedQuorum("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedTime !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {timeFilters.find((f) => f.id === selectedTime)?.label}
                          <button onClick={() => setSelectedTime("all")}>
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
            <div className="flex border-b-2 border-border">
              {governanceTabs.map((tab) => {
                const Icon = tab.icon;
                return (
                  <button
                    key={tab.id}
                    onClick={() => setActiveTab(tab.id)}
                    className={cn(
                      "flex-1 flex items-center justify-center gap-2 py-3 text-sm font-medium transition-colors relative",
                      activeTab === tab.id
                        ? "text-primary"
                        : "text-muted-foreground hover:text-foreground hover:bg-card"
                    )}
                  >
                    <Icon className="size-4" />
                    <span className="hidden sm:inline">{tab.label}</span>
                    {activeTab === tab.id && (
                      <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
                    )}
                  </button>
                );
              })}
            </div>
          </div>

          {/* Proposals Feed */}
          <div>
            {isLoading ? (
              <div className="flex items-center justify-center py-20">
                <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              </div>
            ) : filteredProposals.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Vote className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No proposals found</p>
                <p className="text-muted-foreground mt-1">Proposals are created within projects</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/projects">
                    Browse Projects
                  </Link>
                </Button>
              </div>
            ) : (
              filteredProposals.map((proposal) => (
                <article
                  key={proposal.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link
                        href={`/dashboard/profile/${proposal.creator.id}`}
                        className="shrink-0"
                      >
                        <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
                          <AvatarImage src={proposal.creator.avatar} />
                          <AvatarFallback>{proposal.creator.name.charAt(0)}</AvatarFallback>
                        </Avatar>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/dashboard/profile/${proposal.creator.id}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {proposal.creator.name}
                          </Link>
                          <span className="text-muted-foreground text-sm">·</span>
                          <Link
                            href={`/projects/${proposal.project.slug}`}
                            className="text-sm text-muted-foreground hover:text-primary transition-colors"
                          >
                            {proposal.project.name}
                          </Link>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(proposal.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mt-0.5">
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
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="icon-sm">
                              <MoreHorizontal className="size-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem>Copy link</DropdownMenuItem>
                            <DropdownMenuItem>Report</DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </div>

                    {/* Proposal Content */}
                    <Link href={`/projects/${proposal.project.slug}?tab=proposals&proposalId=${proposal.id}`} className="block mt-3 pl-13">
                      <h3 className="font-semibold text-lg text-foreground hover:text-primary transition-colors">
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
                                {option.label === "For" && <ThumbsUp className="size-3 text-neon-green" />}
                                {option.label === "Against" && <ThumbsDown className="size-3 text-destructive" />}
                                <span className="text-foreground">{option.label}</span>
                              </div>
                              <span className="text-muted-foreground">{option.percentage}%</span>
                            </div>
                            <Progress
                              value={option.percentage}
                              variant={option.label === "For" ? "neon-green" : option.label === "Against" ? "destructive" : "secondary"}
                              className="h-2"
                            />
                          </div>
                        ))}
                      </div>

                      {/* Meta Info */}
                      <div className="flex flex-wrap items-center gap-4 mt-4 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Users className="size-4" />
                          <span>{proposal.totalVotes} votes ({proposal.totalVoters} voters)</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Scale className="size-4" />
                          <span>Quorum: {proposal.currentQuorum}% / {proposal.quorum}%</span>
                        </div>
                        {proposal.status === "Active" && (
                          <div className="flex items-center gap-1 text-warning">
                            <Timer className="size-4" />
                            <span>{formatTimeRemaining(proposal.votingEnds)}</span>
                          </div>
                        )}
                      </div>
                    </Link>

                    {/* Actions */}
                    <div className="mt-4 pl-13 flex items-center gap-1">
                      {proposal.status === "Active" ? (
                        <>
                          <Button
                            variant="neon"
                            size="sm"
                            className="gap-1.5"
                          >
                            <Vote className="size-4" />
                            <span className="text-xs">Cast Vote</span>
                          </Button>
                        </>
                      ) : (
                        <Button
                          variant="outline"
                          size="sm"
                          className="gap-1.5"
                        >
                          <FileText className="size-4" />
                          <span className="text-xs">View Details</span>
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-primary"
                      >
                        <MessageCircle className="size-4" />
                        <span className="text-xs">Discuss</span>
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-neon-green"
                      >
                        <Share2 className="size-4" />
                      </Button>
                      <div className="flex-1" />
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-muted-foreground hover:text-neon-yellow"
                      >
                        <Bookmark className="size-4" />
                      </Button>
                    </div>
                  </div>
                </article>
              ))
            )}

            {/* Load More */}
            {filteredProposals.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more proposals</Button>
              </div>
            )}
          </div>
        </div>

        {/* Right Sidebar - Fixed to right edge */}
        <div className="hidden xl:block fixed right-0 top-0 w-80 p-4 space-y-4 h-screen overflow-y-auto border-l-2 border-border bg-background">
          {/* Stats */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Sparkles className="size-4 text-neon-yellow" />
                Governance Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Proposals</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Active Voting</span>
                <span className="font-medium text-neon">{stats.active}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Passed</span>
                <span className="font-medium text-neon-green">{stats.passed}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Votes Cast</span>
                <span className="font-medium text-foreground">{stats.totalVotes}</span>
              </div>
            </CardContent>
          </Card>

          {/* Active Proposals */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Timer className="size-4 text-warning" />
                Ending Soon
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {activeProposals.map((proposal) => (
                <Link
                  key={proposal.id}
                  href={`/projects/${proposal.project.slug}?tab=proposals&proposalId=${proposal.id}`}
                  className="block"
                >
                  <p className="font-medium text-sm text-foreground hover:text-primary transition-colors line-clamp-1">
                    {proposal.title}
                  </p>
                  <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                    <Badge
                      variant={typeVariants[proposal.type] ?? "secondary"}
                      size="sm"
                    >
                      {proposal.type}
                    </Badge>
                    <span className="text-warning">{formatTimeRemaining(proposal.votingEnds)}</span>
                  </div>
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/governance?tab=active">View all active</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Proposal Types */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-pink" />
                Proposal Types
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(typeVariants).map((type) => (
                <Badge
                  key={type}
                  variant={typeVariants[type]}
                  size="sm"
                  className="cursor-pointer hover:opacity-80"
                >
                  {type}
                </Badge>
              ))}
            </CardContent>
          </Card>

          {/* Footer */}
          <div className="text-xs text-muted-foreground space-x-2 px-2">
            <Link href="/terms" className="hover:underline">Terms</Link>
            <span>·</span>
            <Link href="/privacy" className="hover:underline">Privacy</Link>
            <span>·</span>
            <Link href="/help" className="hover:underline">Help</Link>
            <p className="mt-2">&copy; 2024 ArdaNova</p>
          </div>
        </div>
      </div>
    </div>
  );
}

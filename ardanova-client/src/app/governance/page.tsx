"use client";

import { useState } from "react";
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

// Sample proposals data
const sampleProposals = [
  {
    id: "prop1",
    title: "Allocate 15% Treasury for European Expansion",
    type: "Treasury",
    status: "Active",
    project: { id: "p1", name: "EcoWaste Solutions", slug: "ecowaste-solutions" },
    creator: { id: "u4", name: "Alex Kim", avatar: "https://i.pravatar.cc/150?u=alex" },
    description: "This proposal requests allocation of 15% of the project treasury to fund expansion into Germany and Netherlands markets. Both countries have strong environmental policies.",
    options: [
      { label: "For", votes: 67, percentage: 67 },
      { label: "Against", votes: 23, percentage: 23 },
      { label: "Abstain", votes: 10, percentage: 10 },
    ],
    quorum: 60,
    currentQuorum: 75,
    threshold: 66,
    totalVotes: 100,
    totalVoters: 124,
    votingEnds: new Date(Date.now() + 1000 * 60 * 60 * 24 * 5),
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2),
  },
  {
    id: "prop2",
    title: "Update Token Vesting Schedule for Contributors",
    type: "Token",
    status: "Active",
    project: { id: "p2", name: "HealthTrack", slug: "healthtrack" },
    creator: { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
    description: "Propose to change the contributor token vesting from 12 months to 6 months cliff with 24 months total vesting period.",
    options: [
      { label: "For", votes: 45, percentage: 55 },
      { label: "Against", votes: 37, percentage: 45 },
    ],
    quorum: 50,
    currentQuorum: 62,
    threshold: 51,
    totalVotes: 82,
    totalVoters: 89,
    votingEnds: new Date(Date.now() + 1000 * 60 * 60 * 24 * 3),
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 4),
  },
  {
    id: "prop3",
    title: "Add New Governance Council Member",
    type: "Governance",
    status: "Passed",
    project: { id: "p3", name: "EduConnect", slug: "educonnect" },
    creator: { id: "u5", name: "Jordan Lee", avatar: "https://i.pravatar.cc/150?u=jordan" },
    description: "Proposal to add Maria Santos as the 7th member of the Governance Council to improve decision-making diversity.",
    options: [
      { label: "For", votes: 156, percentage: 89 },
      { label: "Against", votes: 19, percentage: 11 },
    ],
    quorum: 40,
    currentQuorum: 85,
    threshold: 66,
    totalVotes: 175,
    totalVoters: 67,
    votingEnds: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2),
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 9),
  },
  {
    id: "prop4",
    title: "Emergency: Pause Trading During Security Audit",
    type: "Emergency",
    status: "Executed",
    project: { id: "p1", name: "EcoWaste Solutions", slug: "ecowaste-solutions" },
    creator: { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
    description: "Emergency proposal to pause token trading while we conduct a thorough security audit following a potential vulnerability report.",
    options: [
      { label: "For", votes: 98, percentage: 98 },
      { label: "Against", votes: 2, percentage: 2 },
    ],
    quorum: 30,
    currentQuorum: 95,
    threshold: 80,
    totalVotes: 100,
    totalVoters: 124,
    votingEnds: new Date(Date.now() - 1000 * 60 * 60 * 24 * 7),
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 8),
  },
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
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [selectedQuorum, setSelectedQuorum] = useState("all");
  const [selectedTime, setSelectedTime] = useState("all");

  // Filter proposals based on all criteria
  const filteredProposals = sampleProposals.filter((prop) => {
    // Tab filter
    if (activeTab === "active" && prop.status !== "Active") return false;
    if (activeTab === "passed" && prop.status !== "Passed" && prop.status !== "Executed") return false;
    if (activeTab === "rejected" && prop.status !== "Rejected" && prop.status !== "Expired") return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = prop.title.toLowerCase().includes(query);
      const matchesDescription = prop.description.toLowerCase().includes(query);
      const matchesProject = prop.project.name.toLowerCase().includes(query);
      if (!matchesTitle && !matchesDescription && !matchesProject) return false;
    }

    // Type filter
    if (selectedType !== "all" && prop.type !== selectedType) return false;

    // Status filter
    if (selectedStatus !== "all" && prop.status !== selectedStatus) return false;

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
    total: sampleProposals.length,
    active: sampleProposals.filter((p) => p.status === "Active").length,
    passed: sampleProposals.filter((p) => p.status === "Passed" || p.status === "Executed").length,
    totalVotes: sampleProposals.reduce((sum, p) => sum + p.totalVotes, 0),
  };

  // Active proposals for sidebar
  const activeProposals = sampleProposals
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
              <Button variant="neon" size="sm" asChild>
                <Link href="/governance/create">
                  <Plus className="size-4 mr-2" />
                  New Proposal
                </Link>
              </Button>
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
            {filteredProposals.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Vote className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No proposals found</p>
                <p className="text-muted-foreground mt-1">Create a new proposal to get started!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/governance/create">
                    <Plus className="size-4 mr-2" />
                    New Proposal
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
                    <Link href={`/governance/${proposal.id}`} className="block mt-3 pl-13">
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
                  href={`/governance/${proposal.id}`}
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

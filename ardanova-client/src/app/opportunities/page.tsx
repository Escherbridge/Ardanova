"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Briefcase,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
  TrendingUp,
  DollarSign,
  MapPin,
  Calendar,
  Zap,
  Target,
  Users,
  SlidersHorizontal,
  X,
  CheckSquare,
  Loader2,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { FeedLayout } from "~/components/layouts/feed-layout";
import { api } from "~/trpc/react";

// Feed tabs for opportunities
const opportunityTabs = [
  { id: "all", label: "All", icon: Briefcase },
  { id: "bounties", label: "Bounties", icon: Target },
  { id: "tasks", label: "Task Bounties", icon: CheckSquare },
  { id: "freelance", label: "Freelance", icon: Zap },
  { id: "full-time", label: "Full-time", icon: Users },
];

// Type badge variants
const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "Bounty": "neon-green",
  "Freelance": "neon",
  "Part-time": "neon-purple",
  "Full-time": "neon-pink",
  "Contract": "warning",
};

// Skill badge variants
const skillVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "React": "neon",
  "TypeScript": "neon",
  "Node.js": "neon-green",
  "Python": "neon-green",
  "UI/UX": "neon-pink",
  "Design": "neon-pink",
  "Solidity": "neon-purple",
  "Web3": "neon-purple",
  "Marketing": "warning",
  "Writing": "secondary",
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
function isOpportunityUrgent(deadline?: string, status?: string): boolean {
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

function formatDeadline(dateString?: string): string {
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

function formatCompensation(amount?: number, details?: string): string {
  if (!amount) return "Negotiable";
  const formatted = amount >= 1000 ? `$${(amount / 1000).toFixed(amount % 1000 === 0 ? 0 : 1)}k` : `$${amount}`;
  if (details === "hourly") return `${formatted}/hr`;
  if (details === "yearly") return `${formatted}/yr`;
  return formatted;
}

export default function MarketplacePage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedSkill, setSelectedSkill] = useState("all");
  const [selectedCompensation, setSelectedCompensation] = useState("all");
  const [selectedLocation, setSelectedLocation] = useState("all");
  const [selectedOrigin, setSelectedOrigin] = useState("all");

  // Fetch opportunities from API
  const { data: opportunitiesResult, isLoading } = api.opportunity.getAll.useQuery({ limit: 100 });
  const allOpportunities = opportunitiesResult?.items ?? [];

  // Filter opportunities based on all criteria
  const filteredOpportunities = allOpportunities.filter((opp) => {
    // Tab filter
    if (activeTab === "bounties" && opp.type !== "Bounty") return false;
    if (activeTab === "tasks" && opp.origin !== "TASK_GENERATED") return false;
    if (activeTab === "freelance" && opp.type !== "Freelance" && opp.type !== "Contract") return false;
    if (activeTab === "full-time" && opp.type !== "Full-time" && opp.type !== "Part-time") return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = opp.title.toLowerCase().includes(query);
      const matchesDescription = opp.description.toLowerCase().includes(query);
      const skillsList = opp.skills ? opp.skills.split(',').map(s => s.trim()) : [];
      const matchesSkills = skillsList.some((s) => s.toLowerCase().includes(query));
      if (!matchesTitle && !matchesDescription && !matchesSkills) return false;
    }

    // Type filter
    if (selectedType !== "all" && opp.type !== selectedType) return false;

    // Skill filter
    if (selectedSkill !== "all") {
      const skillsList = opp.skills ? opp.skills.split(',').map(s => s.trim()) : [];
      if (!skillsList.includes(selectedSkill)) return false;
    }

    // Origin filter
    if (selectedOrigin !== "all" && opp.origin !== selectedOrigin) return false;

    // Compensation filter
    if (selectedCompensation !== "all" && opp.compensation) {
      const amount = opp.compensation;
      if (selectedCompensation === "0-1000" && amount >= 1000) return false;
      if (selectedCompensation === "1000-5000" && (amount < 1000 || amount >= 5000)) return false;
      if (selectedCompensation === "5000-10000" && (amount < 5000 || amount >= 10000)) return false;
      if (selectedCompensation === "10000+" && amount < 10000) return false;
    }

    // Location filter
    if (selectedLocation !== "all") {
      if (selectedLocation === "Remote" && !opp.isRemote) return false;
      if (selectedLocation === "US" && opp.location && !opp.location.includes("CA") && !opp.location.includes("NY") && !opp.location.includes("US")) return false;
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
    total: allOpportunities.length,
    bounties: allOpportunities.filter((o) => o.type === "Bounty").length,
    taskBounties: allOpportunities.filter((o) => o.origin === "TASK_GENERATED").length,
    totalApplicants: allOpportunities.reduce((sum, o) => sum + (o.applicationsCount || 0), 0),
  };

  // Hot opportunities for sidebar
  const hotOpportunities = [...allOpportunities]
    .sort((a, b) => (b.applicationsCount || 0) - (a.applicationsCount || 0))
    .slice(0, 3);

  return (
    <FeedLayout
      sidebar={
        <>
          {/* Stats */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Sparkles className="size-4 text-neon-yellow" />
                Marketplace Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Open Opportunities</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Active Bounties</span>
                <span className="font-medium text-neon-green">{stats.bounties}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Task Bounties</span>
                <span className="font-medium text-neon-purple">{stats.taskBounties}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Applicants</span>
                <span className="font-medium text-foreground">{stats.totalApplicants}</span>
              </div>
            </CardContent>
          </Card>

          {/* Hot Opportunities */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="size-4 text-neon-pink" />
                Hot Opportunities
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {hotOpportunities.map((opp) => (
                <Link
                  key={opp.id}
                  href={`/marketplace/${opp.id}`}
                  className="block"
                >
                  <div className="flex items-start justify-between mb-1">
                    <p className="font-medium text-sm text-foreground hover:text-primary transition-colors line-clamp-1">
                      {opp.title}
                    </p>
                    <Badge
                      variant={typeVariants[opp.type] ?? "secondary"}
                      size="sm"
                    >
                      {opp.type}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span className="text-neon-green font-medium">
                      {formatCompensation(opp.compensation, opp.compensationDetails)}
                    </span>
                    <span>·</span>
                    <span>{opp.applicationsCount || 0} applicants</span>
                  </div>
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/marketplace?tab=all">View all opportunities</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Skills Filter */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-purple" />
                Skills
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(skillVariants).map((skill) => (
                <Badge
                  key={skill}
                  variant={skillVariants[skill]}
                  size="sm"
                  className="cursor-pointer hover:opacity-80"
                >
                  {skill}
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
        </>
      }
    >
      {/* Header */}
      <div className="sticky top-0 z-10 bg-background/95 backdrop-blur border-b-2 border-border">
            <div className="p-4 flex items-center justify-between">
              <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
                <Briefcase className="size-5 text-warning" />
                Opportunities
              </h1>
              <Button variant="neon" size="sm" asChild>
                <Link href="/opportunities/create">
                  <Plus className="size-4 mr-2" />
                  Post Job
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
                  placeholder="Search opportunities..."
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
                <div className="grid grid-cols-3 gap-3">
                  {/* Type Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Job Type
                    </label>
                    <select
                      value={selectedType}
                      onChange={(e) => setSelectedType(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Required Skill
                    </label>
                    <select
                      value={selectedSkill}
                      onChange={(e) => setSelectedSkill(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Origin
                    </label>
                    <select
                      value={selectedOrigin}
                      onChange={(e) => setSelectedOrigin(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {originFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  {/* Compensation Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Budget Range
                    </label>
                    <select
                      value={selectedCompensation}
                      onChange={(e) => setSelectedCompensation(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Location
                    </label>
                    <select
                      value={selectedLocation}
                      onChange={(e) => setSelectedLocation(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                          {typeFilters.find((f) => f.id === selectedType)?.label}
                          <button onClick={() => setSelectedType("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedSkill !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {skillFilters.find((f) => f.id === selectedSkill)?.label}
                          <button onClick={() => setSelectedSkill("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedCompensation !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {compensationFilters.find((f) => f.id === selectedCompensation)?.label}
                          <button onClick={() => setSelectedCompensation("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedLocation !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {locationFilters.find((f) => f.id === selectedLocation)?.label}
                          <button onClick={() => setSelectedLocation("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedOrigin !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {originFilters.find((f) => f.id === selectedOrigin)?.label}
                          <button onClick={() => setSelectedOrigin("all")}>
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
              {opportunityTabs.map((tab) => {
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

          {/* Opportunities Feed */}
          <div>
            {isLoading ? (
              <div className="flex justify-center items-center py-20">
                <Loader2 className="size-8 animate-spin text-primary" />
              </div>
            ) : filteredOpportunities.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Briefcase className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No opportunities found</p>
                <p className="text-muted-foreground mt-1">Check back later or post your own!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/opportunities/create">
                    <Plus className="size-4 mr-2" />
                    Post Job
                  </Link>
                </Button>
              </div>
            ) : (
              filteredOpportunities.map((opp) => {
                const skillsList = opp.skills ? opp.skills.split(',').map(s => s.trim()) : [];
                const isUrgent = isOpportunityUrgent(opp.deadline, opp.status);
                const posterName = opp.poster?.name ?? "Unknown";
                const posterImage = opp.poster?.image;

                return (
                <article
                  key={opp.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link
                        href={`/dashboard/profile/${opp.posterId}`}
                        className="shrink-0"
                      >
                        <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
                          <AvatarImage src={posterImage} />
                          <AvatarFallback>{posterName.charAt(0)}</AvatarFallback>
                        </Avatar>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/dashboard/profile/${opp.posterId}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {posterName}
                          </Link>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(opp.createdAt)}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mt-0.5">
                          posted a new opportunity
                        </p>
                      </div>

                      <div className="flex items-center gap-2">
                        {isUrgent && (
                          <Badge variant="destructive" size="sm">Urgent</Badge>
                        )}
                        <Badge
                          variant={typeVariants[opp.type] ?? "secondary"}
                          size="sm"
                        >
                          {opp.type}
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

                    {/* Opportunity Content */}
                    <Link href={`/opportunities/${opp.slug}`} className="block mt-3 pl-13">
                      <h3 className="font-semibold text-lg text-foreground hover:text-primary transition-colors">
                        {opp.title}
                      </h3>
                      <p className="text-foreground mt-2 line-clamp-2">
                        {opp.description}
                      </p>

                      {/* Skills */}
                      {skillsList.length > 0 && (
                        <div className="flex flex-wrap gap-2 mt-3">
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
                      <div className="flex flex-wrap items-center gap-4 mt-3 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1 text-neon-green font-medium">
                          <DollarSign className="size-4" />
                          <span>{formatCompensation(opp.compensation, opp.compensationDetails)}</span>
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

                    {/* Actions */}
                    <div className="mt-4 pl-13 flex items-center gap-1">
                      <Button
                        variant="neon"
                        size="sm"
                        className="gap-1.5"
                      >
                        <Zap className="size-4" />
                        <span className="text-xs">Apply Now</span>
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-primary"
                      >
                        <MessageCircle className="size-4" />
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
                );
              })
            )}

            {/* Load More */}
            {filteredOpportunities.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more opportunities</Button>
              </div>
            )}
          </div>
    </FeedLayout>
  );
}

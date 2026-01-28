"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Users,
  TrendingUp,
  Briefcase,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Clock,
  SlidersHorizontal,
  X,
  DollarSign,
  MapPin,
  Calendar,
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
import { api } from "~/trpc/react";

// Feed tabs for opportunities
const opportunityTabs = [
  { id: "all", label: "All Opportunities", icon: Briefcase },
  { id: "trending", label: "Trending", icon: TrendingUp },
  { id: "newest", label: "Newest", icon: Clock },
  { id: "bounties", label: "Bounties", icon: DollarSign },
];

// Type badge variants
const typeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  Bounty: "neon-green",
  Freelance: "neon-purple",
  Contract: "neon",
  "Part-time": "warning",
  "Full-time": "neon-pink",
};

// Status badge variants
const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  DRAFT: "secondary",
  OPEN: "neon",
  IN_REVIEW: "warning",
  FILLED: "neon-green",
  CLOSED: "secondary",
  CANCELLED: "destructive",
};

function formatRelativeTime(date: Date): string {
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHour = Math.floor(diffMin / 60);
  const diffDay = Math.floor(diffHour / 24);

  if (diffSec < 60) return "just now";
  if (diffMin < 60) return `${diffMin}m ago`;
  if (diffHour < 24) return `${diffHour}h ago`;
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString();
}

// Filter options
const typeFilters = [
  { id: "all", label: "All Types" },
  { id: "Bounty", label: "Bounty" },
  { id: "Freelance", label: "Freelance" },
  { id: "Contract", label: "Contract" },
  { id: "Part-time", label: "Part-time" },
  { id: "Full-time", label: "Full-time" },
];

const experienceLevelFilters = [
  { id: "all", label: "All Levels" },
  { id: "Entry", label: "Entry Level" },
  { id: "Intermediate", label: "Intermediate" },
  { id: "Advanced", label: "Advanced" },
  { id: "Expert", label: "Expert" },
];

const compensationFilters = [
  { id: "all", label: "Any Compensation" },
  { id: "0-1000", label: "Under $1,000" },
  { id: "1000-5000", label: "$1,000 - $5,000" },
  { id: "5000-10000", label: "$5,000 - $10,000" },
  { id: "10000+", label: "Over $10,000" },
];

const locationFilters = [
  { id: "all", label: "All Locations" },
  { id: "remote", label: "Remote Only" },
  { id: "hybrid", label: "Hybrid" },
  { id: "onsite", label: "On-site" },
];

export default function OpportunitiesPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedType, setSelectedType] = useState("all");
  const [selectedExperience, setSelectedExperience] = useState("all");
  const [selectedCompensation, setSelectedCompensation] = useState("all");
  const [selectedLocation, setSelectedLocation] = useState("all");

  // Fetch opportunities from API
  const { data: opportunitiesResult, isLoading } = api.opportunity.getAll.useQuery({
    limit: 50,
  });

  const opportunities = opportunitiesResult?.items || [];

  // Filter opportunities based on all criteria
  const filteredOpportunities = opportunities.filter((opportunity) => {
    // Tab filter
    if (activeTab === "trending" && (opportunity.applicationsCount ?? 0) === 0) return false;
    if (activeTab === "bounties" && opportunity.type !== "Bounty") return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = opportunity.title.toLowerCase().includes(query);
      const matchesDescription = opportunity.description?.toLowerCase().includes(query);
      const matchesSkills = opportunity.requiredSkills?.toLowerCase().includes(query);
      if (!matchesTitle && !matchesDescription && !matchesSkills) return false;
    }

    // Type filter
    if (selectedType !== "all" && opportunity.type !== selectedType) return false;

    // Experience level filter
    if (selectedExperience !== "all" && opportunity.experienceLevel !== selectedExperience) return false;

    // Compensation filter
    if (selectedCompensation !== "all") {
      const compensation = Number(opportunity.compensation || 0);
      if (selectedCompensation === "0-1000" && compensation >= 1000) return false;
      if (selectedCompensation === "1000-5000" && (compensation < 1000 || compensation >= 5000)) return false;
      if (selectedCompensation === "5000-10000" && (compensation < 5000 || compensation >= 10000)) return false;
      if (selectedCompensation === "10000+" && compensation < 10000) return false;
    }

    // Location filter
    if (selectedLocation !== "all") {
      if (selectedLocation === "remote" && !opportunity.isRemote) return false;
      if (selectedLocation === "onsite" && opportunity.isRemote) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedType !== "all" ||
    selectedExperience !== "all" ||
    selectedCompensation !== "all" ||
    selectedLocation !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedType("all");
    setSelectedExperience("all");
    setSelectedCompensation("all");
    setSelectedLocation("all");
  };

  const activeFilterCount =
    (selectedType !== "all" ? 1 : 0) +
    (selectedExperience !== "all" ? 1 : 0) +
    (selectedCompensation !== "all" ? 1 : 0) +
    (selectedLocation !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: opportunities.length,
    active: opportunities.filter((o) => o.status === "OPEN").length,
    totalApplicants: opportunities.reduce((sum, o) => sum + (o.applicationsCount || 0), 0),
  };

  // Trending opportunities for sidebar
  const trendingOpportunities = [...opportunities]
    .sort((a, b) => (b.applicationsCount || 0) - (a.applicationsCount || 0))
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
                <Briefcase className="size-5 text-primary" />
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
                <div className="grid grid-cols-2 gap-3">
                  {/* Type Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Type
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

                  {/* Experience Level Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Experience Level
                    </label>
                    <select
                      value={selectedExperience}
                      onChange={(e) => setSelectedExperience(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {experienceLevelFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Compensation Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Compensation Range
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
                      {selectedExperience !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {experienceLevelFilters.find((f) => f.id === selectedExperience)?.label}
                          <button onClick={() => setSelectedExperience("all")}>
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
              <div className="flex items-center justify-center py-20">
                <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              </div>
            ) : filteredOpportunities.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Briefcase className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No opportunities found</p>
                <p className="text-muted-foreground mt-1">Be the first to post one!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/opportunities/create">
                    <Plus className="size-4 mr-2" />
                    Post Job
                  </Link>
                </Button>
              </div>
            ) : (
              filteredOpportunities.map((opportunity) => (
                <article
                  key={opportunity.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link
                        href={`/dashboard/profile/${opportunity.postedById}`}
                        className="shrink-0"
                      >
                        <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
                          <AvatarImage src={(opportunity as any).postedBy?.image || undefined} />
                          <AvatarFallback>
                            {(opportunity as any).postedBy?.name?.charAt(0) || "U"}
                          </AvatarFallback>
                        </Avatar>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/dashboard/profile/${opportunity.postedById}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {(opportunity as any).postedBy?.name || "Unknown User"}
                          </Link>
                          <Badge variant="secondary" size="sm">Poster</Badge>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(new Date(opportunity.createdAt))}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mt-0.5">
                          posted a new opportunity
                        </p>
                      </div>

                      <div className="flex items-center gap-2">
                        <Badge
                          variant={statusVariants[opportunity.status] || "secondary"}
                          size="sm"
                        >
                          {opportunity.status.replace("_", " ")}
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
                    <Link href={`/opportunities/${opportunity.id}`} className="block mt-3 pl-13">
                      <h3 className="font-semibold text-lg text-foreground hover:text-primary transition-colors">
                        {opportunity.title}
                      </h3>
                      <p className="text-foreground mt-2 line-clamp-3">
                        {opportunity.description}
                      </p>

                      {/* Type & Skills */}
                      <div className="flex flex-wrap gap-2 mt-3">
                        <Badge
                          variant={typeVariants[opportunity.type] || "secondary"}
                          size="sm"
                        >
                          {opportunity.type}
                        </Badge>
                        {opportunity.requiredSkills?.split(",").slice(0, 3).map((skill, i) => (
                          <Badge key={i} variant="secondary" size="sm">
                            {skill.trim()}
                          </Badge>
                        ))}
                      </div>

                      {/* Opportunity Details */}
                      <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground flex-wrap">
                        {opportunity.compensation && (
                          <div className="flex items-center gap-1">
                            <DollarSign className="size-4" />
                            <span>${Number(opportunity.compensation).toLocaleString()}</span>
                          </div>
                        )}
                        {opportunity.location && (
                          <div className="flex items-center gap-1">
                            <MapPin className="size-4" />
                            <span>{opportunity.location}</span>
                            {opportunity.isRemote && <Badge variant="neon-green" size="sm">Remote</Badge>}
                          </div>
                        )}
                        {opportunity.deadline && (
                          <div className="flex items-center gap-1">
                            <Calendar className="size-4" />
                            <span>Deadline: {new Date(opportunity.deadline).toLocaleDateString()}</span>
                          </div>
                        )}
                      </div>

                      {/* Stats */}
                      <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Users className="size-4" />
                          <span>{opportunity.applicationsCount || 0} applicants</span>
                        </div>
                      </div>
                    </Link>

                    {/* Actions */}
                    <div className="mt-4 pl-13 flex items-center gap-1">
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
                      <Button
                        variant="neon"
                        size="sm"
                        asChild
                      >
                        <Link href={`/opportunities/${opportunity.id}`}>
                          Apply Now
                        </Link>
                      </Button>
                    </div>
                  </div>
                </article>
              ))
            )}

            {/* Load More */}
            {filteredOpportunities.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more opportunities</Button>
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
                <Briefcase className="size-4 text-primary" />
                Opportunity Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Opportunities</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Active</span>
                <span className="font-medium text-neon-green">{stats.active}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Applicants</span>
                <span className="font-medium text-foreground">{stats.totalApplicants}</span>
              </div>
            </CardContent>
          </Card>

          {/* Trending Opportunities */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="size-4 text-primary" />
                Trending Opportunities
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {trendingOpportunities.map((opportunity) => (
                <Link
                  key={opportunity.id}
                  href={`/opportunities/${opportunity.id}`}
                  className="block"
                >
                  <div className="flex items-start justify-between mb-2">
                    <div className="flex-1">
                      <p className="font-medium text-sm text-foreground hover:text-primary transition-colors line-clamp-1">
                        {opportunity.title}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {opportunity.applicationsCount || 0} applicants
                      </p>
                    </div>
                    <Badge
                      variant={typeVariants[opportunity.type] || "secondary"}
                      size="sm"
                    >
                      {opportunity.type}
                    </Badge>
                  </div>
                  {opportunity.compensation && (
                    <div className="flex items-center gap-1 text-xs text-muted-foreground">
                      <DollarSign className="size-3" />
                      <span>${Number(opportunity.compensation).toLocaleString()}</span>
                    </div>
                  )}
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/opportunities?tab=trending">View all trending</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Job Types */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-pink" />
                Job Types
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

"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Star,
  Users,
  Briefcase,
  CheckCircle,
  Plus,
  MessageCircle,
  ArrowUpRight,
  Sparkles,
  Award,
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
import { api } from "~/trpc/react";
import { FeedLayout } from "~/components/layouts/feed-layout";

// Feed tabs for guilds
const guildTabs: { id: string; label: string }[] = [
  { id: "all", label: "All Guilds" },
  { id: "verified", label: "Verified" },
  { id: "top-rated", label: "Rated 4+" },
  { id: "newest", label: "Newest" },
];

// Specialty badge variants
const specialtyVariants: Record<
  string,
  "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"
> = {
  "Web Development": "neon",
  "Mobile Development": "neon-purple",
  "UI/UX Design": "neon-pink",
  "Data Science": "neon-green",
  Marketing: "warning",
  Finance: "neon-green",
  Legal: "secondary",
  Strategy: "neon-purple",
};

// Filter options
const specialtyFilters = [
  { id: "all", label: "All Specialties" },
  { id: "Web Development", label: "Web Development" },
  { id: "Mobile Development", label: "Mobile Development" },
  { id: "UI/UX Design", label: "UI/UX Design" },
  { id: "Data Science", label: "Data Science" },
  { id: "Marketing", label: "Marketing" },
  { id: "Finance", label: "Finance" },
  { id: "Legal", label: "Legal" },
  { id: "Strategy", label: "Strategy" },
];

const ratingFilters = [
  { id: "all", label: "Any Rating" },
  { id: "4+", label: "4+ Stars" },
  { id: "3+", label: "3+ Stars" },
  { id: "2+", label: "2+ Stars" },
];

const projectsFilters = [
  { id: "all", label: "Any Projects" },
  { id: "1+", label: "1+ Projects" },
  { id: "5+", label: "5+ Projects" },
  { id: "10+", label: "10+ Projects" },
  { id: "25+", label: "25+ Projects" },
];

const timeFilters = [
  { id: "all", label: "All Time" },
  { id: "today", label: "Today" },
  { id: "week", label: "This Week" },
  { id: "month", label: "This Month" },
  { id: "year", label: "This Year" },
];

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

function renderRating(rating: number | null | undefined) {
  if (!rating) return null;
  return (
    <div className="flex items-center gap-1 text-sm">
      <Star className="fill-neon-yellow text-warning size-4" />
      <span className="text-foreground font-medium">
        {Number(rating).toFixed(1)}
      </span>
    </div>
  );
}

function GuildsFeedSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <div
      className="divide-border divide-y-2"
      role="status"
      aria-live="polite"
      aria-busy="true"
    >
      <span className="sr-only">Loading guilds, please wait.</span>
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="bg-card flex gap-3 p-4">
          <div className="bg-muted size-12 shrink-0 animate-pulse rounded-full" />
          <div className="min-w-0 flex-1 space-y-2.5 pt-1">
            <div className="bg-muted h-4 w-[min(60%,18rem)] animate-pulse rounded-md" />
            <div className="bg-muted h-3 w-[min(40%,12rem)] animate-pulse rounded-md" />
            <div className="bg-muted h-3 w-24 animate-pulse rounded-md" />
          </div>
        </div>
      ))}
    </div>
  );
}

export default function GuildsPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedSpecialty, setSelectedSpecialty] = useState("all");
  const [selectedRating, setSelectedRating] = useState("all");
  const [selectedProjects, setSelectedProjects] = useState("all");
  const [selectedTime, setSelectedTime] = useState("all");

  // Fetch guilds from API
  const {
    data: guildsResult,
    isPending: isInitialLoad,
    error: guildsError,
  } = api.guild.getAll.useQuery({
    limit: 50,
    page: 1,
  });

  const guilds = guildsResult?.items ?? [];

  // Filter guilds based on all criteria
  const filteredGuilds = guilds.filter((guild) => {
    // Tab filter
    if (activeTab === "verified" && !guild.isVerified) return false;
    if (activeTab === "top-rated" && (guild.rating ?? 0) < 4) return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesName = guild.name.toLowerCase().includes(query);
      const matchesDescription = guild.description
        ?.toLowerCase()
        .includes(query);
      const matchesSpecialties = guild.specialties
        ?.toLowerCase()
        .includes(query);
      if (!matchesName && !matchesDescription && !matchesSpecialties)
        return false;
    }

    // Specialty filter
    if (selectedSpecialty !== "all") {
      if (!guild.specialties?.includes(selectedSpecialty)) return false;
    }

    // Rating filter
    if (selectedRating !== "all") {
      const rating = Number(guild.rating || 0);
      if (selectedRating === "4+" && rating < 4) return false;
      if (selectedRating === "3+" && rating < 3) return false;
      if (selectedRating === "2+" && rating < 2) return false;
    }

    // Projects filter
    if (selectedProjects !== "all") {
      const projects = guild.projectsCount ?? 0;
      if (selectedProjects === "1+" && projects < 1) return false;
      if (selectedProjects === "5+" && projects < 5) return false;
      if (selectedProjects === "10+" && projects < 10) return false;
      if (selectedProjects === "25+" && projects < 25) return false;
    }

    // Time filter
    if (selectedTime !== "all") {
      const now = new Date();
      const guildDate = new Date(guild.createdAt);
      const diffDays =
        (now.getTime() - guildDate.getTime()) / (1000 * 60 * 60 * 24);

      if (selectedTime === "today" && diffDays > 1) return false;
      if (selectedTime === "week" && diffDays > 7) return false;
      if (selectedTime === "month" && diffDays > 30) return false;
      if (selectedTime === "year" && diffDays > 365) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedSpecialty !== "all" ||
    selectedRating !== "all" ||
    selectedProjects !== "all" ||
    selectedTime !== "all";

  const clearFilters = () => {
    setActiveTab("all");
    setSearchQuery("");
    setSelectedSpecialty("all");
    setSelectedRating("all");
    setSelectedProjects("all");
    setSelectedTime("all");
  };

  const activeFilterCount =
    (selectedSpecialty !== "all" ? 1 : 0) +
    (selectedRating !== "all" ? 1 : 0) +
    (selectedProjects !== "all" ? 1 : 0) +
    (selectedTime !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: filteredGuilds.length,
    verified: filteredGuilds.filter((g) => g.isVerified).length,
    totalProjects: filteredGuilds.reduce(
      (sum, g) => sum + (g.projectsCount ?? 0),
      0,
    ),
    totalReviews: filteredGuilds.reduce(
      (sum, g) => sum + (g.reviewsCount ?? 0),
      0,
    ),
  };

  // Highest-rated guilds among the records returned by this query.
  const highestRatedGuildsInView = [...filteredGuilds]
    .sort((a, b) => (Number(b.rating) || 0) - (Number(a.rating) || 0))
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
                  Guilds in this view
                </span>
                <span className="text-foreground font-medium">
                  {guildsError ? "—" : stats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Verified in this view
                </span>
                <span className="text-success font-medium">
                  {guildsError ? "—" : stats.verified}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Projects across visible guilds
                </span>
                <span className="text-foreground font-medium">
                  {guildsError ? "—" : stats.totalProjects}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Reviews across visible guilds
                </span>
                <span className="text-foreground font-medium">
                  {guildsError ? "—" : stats.totalReviews}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Highest rated among visible results */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Award className="text-warning size-4" />
                Highest rated in this view
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {highestRatedGuildsInView.map((guild) => (
                <Link
                  key={guild.id}
                  href={`/guilds/${guild.slug}`}
                  className="flex items-center gap-3"
                >
                  <Avatar className="border-border size-10 border-2">
                    {guild.logo ? (
                      <AvatarImage src={guild.logo} alt={guild.name} />
                    ) : null}
                    <AvatarFallback className="bg-primary/20 text-primary">
                      <Briefcase className="size-4" />
                    </AvatarFallback>
                  </Avatar>
                  <div className="min-w-0 flex-1">
                    <p className="text-foreground hover:text-primary truncate text-sm font-medium transition-colors">
                      {guild.name}
                    </p>
                    <div className="flex items-center gap-2">
                      {renderRating(guild.rating)}
                      {guild.isVerified && (
                        <CheckCircle className="text-success size-3" />
                      )}
                    </div>
                  </div>
                </Link>
              ))}
            </CardContent>
          </Card>

          {/* Specialties */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Filter className="text-primary size-4" />
                Specialties
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(specialtyVariants).map((specialty) => (
                <button
                  key={specialty}
                  type="button"
                  aria-pressed={selectedSpecialty === specialty}
                  onClick={() =>
                    setSelectedSpecialty(
                      selectedSpecialty === specialty ? "all" : specialty,
                    )
                  }
                  className="focus-visible:ring-ring min-h-11 focus-visible:ring-2 focus-visible:outline-none"
                >
                  <Badge
                    variant={specialtyVariants[specialty]}
                    size="sm"
                    className="cursor-pointer hover:opacity-80"
                  >
                    {specialty}
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
            <Users className="text-primary size-5" />
            Guilds
          </h1>
          <Button variant="neon" size="sm" asChild>
            <Link href="/guilds/create">
              <Plus className="mr-2 size-4" />
              Create Guild
            </Link>
          </Button>
        </div>

        {/* Search */}
        <div className="flex flex-col gap-2 px-4 pb-3 sm:flex-row sm:items-center">
          <div className="relative flex-1">
            <label htmlFor="guilds-search" className="sr-only">
              Search guilds
            </label>
            <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
            <input
              type="text"
              id="guilds-search"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search guilds..."
              className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary w-full border-2 py-2 pr-4 pl-10 text-sm focus:outline-none"
            />
            {searchQuery && (
              <button
                type="button"
                onClick={() => setSearchQuery("")}
                aria-label="Clear guild search"
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
            aria-controls="guild-filters"
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
            id="guild-filters"
            className="border-border space-y-3 border-t px-4 pt-3 pb-4"
          >
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {/* Specialty Filter */}
              <div>
                <label
                  htmlFor="guild-specialty-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Specialty
                </label>
                <select
                  id="guild-specialty-filter"
                  value={selectedSpecialty}
                  onChange={(e) => setSelectedSpecialty(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {specialtyFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Rating Filter */}
              <div>
                <label
                  htmlFor="guild-rating-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Minimum Rating
                </label>
                <select
                  id="guild-rating-filter"
                  value={selectedRating}
                  onChange={(e) => setSelectedRating(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {ratingFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Projects Filter */}
              <div>
                <label
                  htmlFor="guild-projects-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Projects Completed
                </label>
                <select
                  id="guild-projects-filter"
                  value={selectedProjects}
                  onChange={(e) => setSelectedProjects(e.target.value)}
                  className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                >
                  {projectsFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Time Filter */}
              <div>
                <label
                  htmlFor="guild-time-filter"
                  className="text-muted-foreground mb-1.5 block text-xs"
                >
                  Created
                </label>
                <select
                  id="guild-time-filter"
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
                  {selectedSpecialty !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        specialtyFilters.find((f) => f.id === selectedSpecialty)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove specialty filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedSpecialty("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedRating !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        ratingFilters.find((f) => f.id === selectedRating)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove rating filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedRating("all")}
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedProjects !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {
                        projectsFilters.find((f) => f.id === selectedProjects)
                          ?.label
                      }
                      <button
                        type="button"
                        aria-label="Remove projects filter"
                        className="flex min-h-11 min-w-11 items-center justify-center"
                        onClick={() => setSelectedProjects("all")}
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
                        aria-label="Remove created-date filter"
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
          aria-label="Guild scope"
          onKeyDown={handleTabListKeyDown}
        >
          {guildTabs.map((tab) => {
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                role="tab"
                id={`guilds-tab-${tab.id}`}
                aria-selected={activeTab === tab.id}
                aria-controls={`guilds-panel-${tab.id}`}
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

      {/* Guilds Feed */}
      <div
        role="tabpanel"
        id={`guilds-panel-${activeTab}`}
        aria-labelledby={`guilds-tab-${activeTab}`}
      >
        {isInitialLoad ? (
          <div className="bg-card border-border border-b-2">
            <div className="text-muted-foreground border-border flex items-center gap-2 border-b px-4 py-3 text-sm">
              <Loader2
                className="text-primary size-4 shrink-0 animate-spin"
                aria-hidden
              />
              <span>Loading guilds…</span>
            </div>
            <GuildsFeedSkeleton />
          </div>
        ) : guildsError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 border-b-2 px-4 py-10 text-center"
          >
            <p className="text-destructive font-medium">
              Guilds could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No guild counts or results are shown.
            </p>
          </div>
        ) : filteredGuilds.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-4 py-20">
            <Users className="text-muted-foreground mb-4 size-12" />
            <p className="text-foreground text-lg font-medium">
              No guilds found
            </p>
            <p className="text-muted-foreground mt-1">
              {guilds.length > 0
                ? "No guilds match the current view."
                : "No guild records were returned."}
            </p>
            {guilds.length > 0 ? (
              <Button variant="outline" className="mt-4" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : (
              <Button variant="neon" className="mt-4" asChild>
                <Link href="/guilds/create">
                  <Plus className="mr-2 size-4" />
                  Create Guild
                </Link>
              </Button>
            )}
          </div>
        ) : (
          filteredGuilds.map((guild) => (
            <article
              key={guild.id}
              className="border-border bg-card hover:bg-card/80 border-b-2 transition-colors"
            >
              <div className="p-4">
                {/* Header */}
                <div className="flex items-start gap-3">
                  <Link
                    href={`/guilds/${guild.slug}`}
                    aria-label={`Open ${guild.name} guild profile`}
                    className="shrink-0"
                  >
                    <Avatar className="border-border hover:border-primary size-12 border-2 transition-colors">
                      {guild.logo ? (
                        <AvatarImage src={guild.logo} alt={guild.name} />
                      ) : null}
                      <AvatarFallback className="bg-primary/20 text-primary">
                        <Briefcase className="size-6" />
                      </AvatarFallback>
                    </Avatar>
                  </Link>

                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link
                        href={`/guilds/${guild.slug}`}
                        className="text-foreground hover:text-primary font-medium transition-colors"
                      >
                        {guild.name}
                      </Link>
                      {guild.isVerified && (
                        <CheckCircle className="text-success size-4" />
                      )}
                      <Badge variant="neon-pink" size="sm">
                        Guild
                      </Badge>
                      <span className="text-muted-foreground text-sm">·</span>
                      <span className="text-muted-foreground text-sm">
                        {formatRelativeTime(new Date(guild.createdAt))}
                      </span>
                    </div>
                    <div className="mt-1 flex items-center gap-3">
                      {renderRating(guild.rating)}
                      <span className="text-muted-foreground text-sm">
                        {guild.projectsCount ?? 0} projects completed
                      </span>
                    </div>
                  </div>
                </div>

                {/* Guild Content */}
                <Link
                  href={`/guilds/${guild.slug}`}
                  className="mt-3 block pl-15"
                >
                  <p className="text-foreground line-clamp-3">
                    {guild.description}
                  </p>

                  {/* Specialties */}
                  {guild.specialties && (
                    <div className="mt-3 flex flex-wrap gap-2">
                      {guild.specialties
                        .split(",")
                        .slice(0, 4)
                        .map((specialty, i) => {
                          const trimmed = specialty.trim();
                          return (
                            <Badge
                              key={i}
                              variant={
                                specialtyVariants[trimmed] ?? "secondary"
                              }
                              size="sm"
                            >
                              {trimmed}
                            </Badge>
                          );
                        })}
                    </div>
                  )}

                  {/* Stats */}
                  <div className="text-muted-foreground mt-3 flex items-center gap-4 text-sm">
                    <div className="flex items-center gap-1">
                      <Briefcase className="size-4" />
                      <span>{guild.projectsCount ?? 0} projects</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <MessageCircle className="size-4" />
                      <span>{guild.reviewsCount ?? 0} reviews</span>
                    </div>
                  </div>
                </Link>

                <div className="mt-4 flex justify-end pl-15">
                  <Button variant="outline" size="sm" asChild>
                    <Link href={`/guilds/${guild.slug}`}>
                      Open guild
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

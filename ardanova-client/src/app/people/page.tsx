"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Star,
  Users,
  User as UserIcon,
  Briefcase,
  CheckCircle,
  Plus,
  MessageCircle,
  ArrowUpRight,
  Sparkles,
  Award,
  SlidersHorizontal,
  X,
  UserPlus,
  UserCheck,
  Shield,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { cn } from "~/lib/utils";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { api } from "~/trpc/react";
import { FeedLayout } from "~/components/layouts/feed-layout";

// Primary section tabs
const sectionTabs: { id: string; label: string }[] = [
  { id: "guilds", label: "Guilds" },
  { id: "people", label: "People" },
];

// Sub-tabs for guilds
const guildSubTabs: { id: string; label: string }[] = [
  { id: "all", label: "All Guilds" },
  { id: "verified", label: "Verified" },
  { id: "newest", label: "Newest" },
];

// Sub-tabs for people
const peopleSubTabs: { id: string; label: string }[] = [
  { id: "all", label: "All People" },
  { id: "verified", label: "Verified" },
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

// User type badge variants
const userTypeVariants: Record<
  string,
  "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"
> = {
  INNOVATOR: "neon",
  SUPPORTER: "neon-green",
  VOLUNTEER: "neon-purple",
  FREELANCER: "neon-pink",
  SME_OWNER: "warning",
  GUILD_MEMBER: "secondary",
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

function renderRating(rating: number | null | undefined) {
  if (!rating) return null;
  return (
    <div className="flex items-center gap-1 text-sm">
      <Star className="fill-neon-yellow text-neon-yellow size-4" />
      <span className="text-foreground font-medium">
        {Number(rating).toFixed(1)}
      </span>
    </div>
  );
}

function FollowButton({
  userId,
  className,
}: {
  userId: string;
  className?: string;
}) {
  const utils = api.useUtils();
  const {
    data: isFollowing,
    isLoading,
    error: followStateError,
  } = api.user.isFollowing.useQuery({ userId }, { retry: false });

  const followMutation = api.user.follow.useMutation({
    onSuccess: () => {
      void utils.user.isFollowing.invalidate({ userId });
      void utils.user.getFollowCounts.invalidate({ userId });
    },
  });

  const unfollowMutation = api.user.unfollow.useMutation({
    onSuccess: () => {
      void utils.user.isFollowing.invalidate({ userId });
      void utils.user.getFollowCounts.invalidate({ userId });
    },
  });

  const isPending = followMutation.isPending || unfollowMutation.isPending;

  if (isLoading || followStateError) return null;

  if (isFollowing) {
    return (
      <Button
        variant="outline"
        size="sm"
        className={cn("gap-1.5", className)}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          unfollowMutation.mutate({ userId });
        }}
        disabled={isPending}
      >
        <UserCheck className="size-4" />
        <span className="text-xs">Following</span>
      </Button>
    );
  }

  return (
    <Button
      variant="neon"
      size="sm"
      className={cn("gap-1.5", className)}
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        followMutation.mutate({ userId });
      }}
      disabled={isPending}
    >
      <UserPlus className="size-4" />
      <span className="text-xs">Follow</span>
    </Button>
  );
}

export default function PeoplePage() {
  const [activeSection, setActiveSection] = useState("guilds");
  const [guildTab, setGuildTab] = useState("all");
  const [peopleTab, setPeopleTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);

  // Guild filters
  const [selectedSpecialty, setSelectedSpecialty] = useState("all");
  const [selectedRating, setSelectedRating] = useState("all");

  // Fetch guilds from API
  const {
    data: guildsResult,
    isLoading: guildsLoading,
    error: guildsError,
  } = api.guild.getAll.useQuery({
    limit: 50,
    page: 1,
  });

  // Fetch users from API
  const {
    data: usersResult,
    isLoading: usersLoading,
    error: usersError,
  } = api.user.getAll.useQuery({
    limit: 50,
    page: 1,
  });

  const guilds = guildsResult?.items ?? [];
  const users = usersResult?.items ?? [];

  // Filter guilds
  const filteredGuilds = guilds.filter((guild) => {
    // Sub-tab filter
    if (guildTab === "verified" && !guild.isVerified) return false;
    if (guildTab === "newest") {
      // Show all, sorted by newest (handled below)
    }

    // Search
    if (searchQuery && activeSection === "guilds") {
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
    }

    return true;
  });

  // Sort guilds by newest if that tab is active
  const sortedGuilds =
    guildTab === "newest"
      ? [...filteredGuilds].sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        )
      : filteredGuilds;

  // Filter users
  const filteredUsers = users.filter((user) => {
    // Sub-tab filter
    if (peopleTab === "verified" && user.verificationLevel === "ANONYMOUS")
      return false;

    // Search
    if (searchQuery && activeSection === "people") {
      const query = searchQuery.toLowerCase();
      const matchesName = user.name?.toLowerCase().includes(query);
      const matchesEmail = user.email?.toLowerCase().includes(query);
      const matchesBio = user.bio?.toLowerCase().includes(query);
      if (!matchesName && !matchesEmail && !matchesBio) return false;
    }

    return true;
  });

  // Sort users by newest if that tab is active
  const sortedUsers =
    peopleTab === "newest"
      ? [...filteredUsers].sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
        )
      : filteredUsers;

  const hasActiveFilters =
    searchQuery || selectedSpecialty !== "all" || selectedRating !== "all";

  const clearFilters = () => {
    setGuildTab("all");
    setPeopleTab("all");
    setSearchQuery("");
    setSelectedSpecialty("all");
    setSelectedRating("all");
  };

  // Stats for sidebar
  const guildStats = {
    total: filteredGuilds.length,
    verified: filteredGuilds.filter((g) => g.isVerified).length,
  };

  const userStats = {
    total: filteredUsers.length,
    verified: filteredUsers.filter((u) => u.verificationLevel !== "ANONYMOUS")
      .length,
  };

  // Highest-rated guilds among the records returned by this query.
  const highestRatedGuildsInView = [...filteredGuilds]
    .sort((a, b) => (Number(b.rating) || 0) - (Number(a.rating) || 0))
    .slice(0, 3);

  const currentSubTabs =
    activeSection === "guilds" ? guildSubTabs : peopleSubTabs;
  const currentSubTab = activeSection === "guilds" ? guildTab : peopleTab;
  const setCurrentSubTab =
    activeSection === "guilds" ? setGuildTab : setPeopleTab;

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
                  People in this view
                </span>
                <span className="text-foreground font-medium">
                  {usersError ? "—" : userStats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Verified people in view
                </span>
                <span className="text-neon-green font-medium">
                  {usersError ? "—" : userStats.verified}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Guilds in this view
                </span>
                <span className="text-foreground font-medium">
                  {guildsError ? "—" : guildStats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground text-sm">
                  Verified guilds in view
                </span>
                <span className="text-neon-green font-medium">
                  {guildsError ? "—" : guildStats.verified}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Highest rated among visible results */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center gap-2 text-base">
                <Award className="text-neon-yellow size-4" />
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
                    <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
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
                        <CheckCircle className="text-neon-green size-3" />
                      )}
                    </div>
                  </div>
                </Link>
              ))}
            </CardContent>
          </Card>

          {/* Specialties */}
          {activeSection === "guilds" && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-base">
                  <Filter className="text-neon-pink size-4" />
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
          )}

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
            <Users className="text-neon-pink size-5" />
            People
          </h1>
          {activeSection === "guilds" && (
            <Button variant="neon" size="sm" asChild>
              <Link href="/guilds/create">
                <Plus className="mr-2 size-4" />
                Create Guild
              </Link>
            </Button>
          )}
        </div>

        {/* Primary Section Tabs */}
        <div
          className="border-border flex overflow-x-auto border-b-2"
          role="tablist"
          aria-label="People directory section"
          onKeyDown={handleTabListKeyDown}
        >
          {sectionTabs.map((tab) => {
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => {
                  setActiveSection(tab.id);
                  setSearchQuery("");
                }}
                role="tab"
                id={`people-section-tab-${tab.id}`}
                aria-selected={activeSection === tab.id}
                aria-controls={`people-section-panel-${tab.id}`}
                tabIndex={activeSection === tab.id ? 0 : -1}
                className={cn(
                  "relative flex min-h-11 min-w-28 flex-1 items-center justify-center gap-2 px-3 py-3 text-sm font-medium whitespace-nowrap transition-colors",
                  activeSection === tab.id
                    ? "text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-card",
                )}
              >
                {tab.label}
                {activeSection === tab.id && (
                  <span className="bg-primary absolute right-0 bottom-0 left-0 h-0.5" />
                )}
              </button>
            );
          })}
        </div>

        <div
          role="tabpanel"
          id={`people-section-panel-${activeSection}`}
          aria-labelledby={`people-section-tab-${activeSection}`}
        >
          {/* Search */}
          <div className="flex flex-col gap-2 px-4 py-3 sm:flex-row sm:items-center">
            <div className="relative flex-1">
              <label htmlFor="people-search" className="sr-only">
                {activeSection === "guilds" ? "Search guilds" : "Search people"}
              </label>
              <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
              <input
                type="text"
                id="people-search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={
                  activeSection === "guilds"
                    ? "Search guilds..."
                    : "Search people..."
                }
                className="bg-card border-border text-foreground placeholder:text-muted-foreground focus:border-primary w-full border-2 py-2 pr-4 pl-10 text-sm focus:outline-none"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => setSearchQuery("")}
                  aria-label={`Clear ${activeSection === "guilds" ? "guild" : "people"} search`}
                  className="text-muted-foreground hover:text-foreground absolute top-1/2 right-0 flex min-h-11 min-w-11 -translate-y-1/2 items-center justify-center"
                >
                  <X className="size-4" />
                </button>
              )}
            </div>
            {activeSection === "guilds" && (
              <Button
                variant={showFilters ? "neon" : "outline"}
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
                aria-expanded={showFilters}
                aria-controls="people-guild-filters"
                className="w-full gap-1.5 sm:w-auto"
              >
                <SlidersHorizontal className="size-4" />
                Filters
              </Button>
            )}
          </div>

          {/* Expanded Filters (guilds only) */}
          {showFilters && activeSection === "guilds" && (
            <div
              id="people-guild-filters"
              className="border-border space-y-3 border-t px-4 pt-3 pb-4"
            >
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="people-specialty-filter"
                    className="text-muted-foreground mb-1.5 block text-xs"
                  >
                    Specialty
                  </label>
                  <select
                    id="people-specialty-filter"
                    value={selectedSpecialty}
                    onChange={(e) => setSelectedSpecialty(e.target.value)}
                    className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                  >
                    <option value="all">All Specialties</option>
                    {Object.keys(specialtyVariants).map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label
                    htmlFor="people-rating-filter"
                    className="text-muted-foreground mb-1.5 block text-xs"
                  >
                    Minimum Rating
                  </label>
                  <select
                    id="people-rating-filter"
                    value={selectedRating}
                    onChange={(e) => setSelectedRating(e.target.value)}
                    className="bg-card border-border text-foreground focus:border-primary min-h-11 w-full cursor-pointer appearance-none border-2 px-3 py-2 text-sm focus:outline-none"
                  >
                    <option value="all">Any Rating</option>
                    <option value="4+">4+ Stars</option>
                    <option value="3+">3+ Stars</option>
                  </select>
                </div>
              </div>
              {hasActiveFilters && (
                <div className="flex items-center justify-end pt-2">
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

          {/* Sub-Tabs */}
          <div
            className="border-border flex overflow-x-auto border-b-2"
            role="tablist"
            aria-label={`${activeSection === "guilds" ? "Guild" : "People"} filters`}
            onKeyDown={handleTabListKeyDown}
          >
            {currentSubTabs.map((tab) => {
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setCurrentSubTab(tab.id)}
                  role="tab"
                  id={`people-${activeSection}-filter-tab-${tab.id}`}
                  aria-selected={currentSubTab === tab.id}
                  aria-controls={`people-${activeSection}-panel-${tab.id}`}
                  tabIndex={currentSubTab === tab.id ? 0 : -1}
                  aria-label={tab.label}
                  className={cn(
                    "relative flex min-h-11 min-w-24 flex-1 items-center justify-center gap-2 px-3 py-2.5 text-xs font-medium whitespace-nowrap transition-colors",
                    currentSubTab === tab.id
                      ? "text-primary"
                      : "text-muted-foreground hover:text-foreground hover:bg-card",
                  )}
                >
                  <span className="hidden sm:inline">{tab.label}</span>
                  {currentSubTab === tab.id && (
                    <span className="bg-primary absolute right-0 bottom-0 left-0 h-0.5" />
                  )}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div
        role="tabpanel"
        id={`people-${activeSection}-panel-${currentSubTab}`}
        aria-labelledby={`people-${activeSection}-filter-tab-${currentSubTab}`}
      >
        {activeSection === "guilds" ? (
          // Guilds Feed
          guildsLoading ? (
            <div className="flex items-center justify-center py-20">
              <div className="border-primary size-8 animate-spin rounded-full border-2 border-t-transparent" />
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
          ) : sortedGuilds.length === 0 ? (
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
                <Button
                  variant="outline"
                  className="mt-4"
                  onClick={clearFilters}
                >
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
            sortedGuilds.map((guild) => (
              <article
                key={guild.id}
                className="border-border bg-card hover:bg-card/80 border-b-2 transition-colors"
              >
                <div className="p-4">
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
                        <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
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
                          <CheckCircle className="text-neon-green size-4" />
                        )}
                        <Badge variant="neon-pink" size="sm">
                          Guild
                        </Badge>
                        <span className="text-muted-foreground text-sm">
                          &middot;
                        </span>
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

                  <Link
                    href={`/guilds/${guild.slug}`}
                    className="mt-3 block pl-15"
                  >
                    <p className="text-foreground line-clamp-3">
                      {guild.description}
                    </p>

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
          )
        ) : // People Feed
        usersLoading ? (
          <div className="flex items-center justify-center py-20">
            <div className="border-primary size-8 animate-spin rounded-full border-2 border-t-transparent" />
          </div>
        ) : usersError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 border-b-2 px-4 py-10 text-center"
          >
            <p className="text-destructive font-medium">
              People could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No people counts or profiles are shown.
            </p>
          </div>
        ) : sortedUsers.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-4 py-20">
            <UserIcon className="text-muted-foreground mb-4 size-12" />
            <p className="text-foreground text-lg font-medium">
              No people found
            </p>
            <p className="text-muted-foreground mt-1">
              {users.length > 0
                ? "No people match the current view."
                : "No people records were returned."}
            </p>
            {users.length > 0 && (
              <Button variant="outline" className="mt-4" onClick={clearFilters}>
                Clear filters
              </Button>
            )}
          </div>
        ) : (
          sortedUsers.map((user) => (
            <article
              key={user.id}
              className="border-border bg-card hover:bg-card/80 border-b-2 transition-colors"
            >
              <div className="p-4">
                <div className="flex items-start gap-3">
                  <Link
                    href={`/dashboard/profile/${user.id}`}
                    aria-label={`Open ${user.name ?? "Anonymous member"} profile`}
                    className="shrink-0"
                  >
                    <Avatar className="border-border hover:border-primary size-12 border-2 transition-colors">
                      {user.image ? (
                        <AvatarImage
                          src={user.image}
                          alt={user.name ?? "User"}
                        />
                      ) : null}
                      <AvatarFallback className="bg-primary/20 text-primary">
                        {user.name?.charAt(0) ?? "U"}
                      </AvatarFallback>
                    </Avatar>
                  </Link>

                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link
                        href={`/dashboard/profile/${user.id}`}
                        className="text-foreground hover:text-primary font-medium transition-colors"
                      >
                        {user.name ?? "Anonymous"}
                      </Link>
                      {user.verificationLevel !== "ANONYMOUS" && (
                        <Shield className="text-neon-green size-4" />
                      )}
                      <Badge
                        variant={userTypeVariants[user.userType] ?? "secondary"}
                        size="sm"
                      >
                        {user.userType.replace("_", " ")}
                      </Badge>
                      <span className="text-muted-foreground text-sm">
                        &middot;
                      </span>
                      <span className="text-muted-foreground text-sm">
                        {formatRelativeTime(new Date(user.createdAt))}
                      </span>
                    </div>
                    <div className="mt-1 flex items-center gap-3">
                      <span className="text-muted-foreground text-sm">
                        Level {user.level}
                      </span>
                      <Badge variant="secondary" size="sm">
                        {user.tier}
                      </Badge>
                      {user.location && (
                        <span className="text-muted-foreground text-sm">
                          {user.location}
                        </span>
                      )}
                    </div>
                  </div>

                  <FollowButton userId={user.id} />
                </div>

                {user.bio && (
                  <div className="mt-3 pl-15">
                    <p className="text-foreground line-clamp-2">{user.bio}</p>
                  </div>
                )}

                <div className="text-muted-foreground mt-3 flex items-center gap-4 pl-15 text-sm">
                  <div className="flex items-center gap-1">
                    <Sparkles className="size-4" />
                    <span>{user.totalXP} XP</span>
                  </div>
                  {user.verificationLevel !== "ANONYMOUS" && (
                    <div className="flex items-center gap-1">
                      <Shield className="text-neon-green size-4" />
                      <span className="text-neon-green">
                        {user.verificationLevel}
                      </span>
                    </div>
                  )}
                </div>

                <div className="mt-4 flex justify-end pl-15">
                  <Button variant="outline" size="sm" asChild>
                    <Link href={`/dashboard/profile/${user.id}`}>
                      View profile
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

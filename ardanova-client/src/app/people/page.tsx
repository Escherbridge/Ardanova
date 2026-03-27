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
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
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
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "~/components/ui/dropdown-menu";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";
import { FeedLayout } from "~/components/layouts/feed-layout";

// Primary section tabs
const sectionTabs = [
  { id: "guilds", label: "Guilds", icon: Users },
  { id: "people", label: "People", icon: UserIcon },
];

// Sub-tabs for guilds
const guildSubTabs = [
  { id: "all", label: "All Guilds", icon: Users },
  { id: "verified", label: "Verified", icon: CheckCircle },
  { id: "newest", label: "Newest", icon: Clock },
];

// Sub-tabs for people
const peopleSubTabs = [
  { id: "all", label: "All People", icon: UserIcon },
  { id: "verified", label: "Verified", icon: Shield },
  { id: "newest", label: "Newest", icon: Clock },
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
const userTypeVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
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

function renderRating(rating: number | null) {
  if (!rating) return null;
  return (
    <div className="flex items-center gap-1 text-sm">
      <Star className="size-4 fill-neon-yellow text-neon-yellow" />
      <span className="font-medium text-foreground">
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
  const { data: isFollowing, isLoading } = api.user.isFollowing.useQuery(
    { userId },
    { retry: false }
  );

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

  if (isLoading) return null;

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
  const { data: guildsResult, isLoading: guildsLoading } =
    api.guild.getAll.useQuery({
      limit: 50,
      page: 1,
    });

  // Fetch users from API
  const { data: usersResult, isLoading: usersLoading } =
    api.user.getAll.useQuery({
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
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
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
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        )
      : filteredUsers;

  const hasActiveFilters =
    searchQuery || selectedSpecialty !== "all" || selectedRating !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedSpecialty("all");
    setSelectedRating("all");
  };

  // Stats for sidebar
  const guildStats = {
    total: guilds.length,
    verified: guilds.filter((g) => g.isVerified).length,
  };

  const userStats = {
    total: users.length,
    verified: users.filter((u) => u.verificationLevel !== "ANONYMOUS").length,
  };

  // Top rated guilds for sidebar
  const topRatedGuilds = [...guilds]
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
              <CardTitle className="text-base flex items-center gap-2">
                <Sparkles className="size-4 text-neon-yellow" />
                Community Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">
                  Total People
                </span>
                <span className="font-medium text-foreground">
                  {userStats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">
                  Verified People
                </span>
                <span className="font-medium text-neon-green">
                  {userStats.verified}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">
                  Total Guilds
                </span>
                <span className="font-medium text-foreground">
                  {guildStats.total}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">
                  Verified Guilds
                </span>
                <span className="font-medium text-neon-green">
                  {guildStats.verified}
                </span>
              </div>
            </CardContent>
          </Card>

          {/* Top Rated Guilds */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Award className="size-4 text-neon-yellow" />
                Top Rated Guilds
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {topRatedGuilds.map((guild) => (
                <Link
                  key={guild.id}
                  href={`/guilds/${guild.slug}`}
                  className="flex items-center gap-3"
                >
                  <Avatar className="size-10 border-2 border-border">
                    {guild.logo ? (
                      <AvatarImage src={guild.logo} alt={guild.name} />
                    ) : null}
                    <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
                      <Briefcase className="size-4" />
                    </AvatarFallback>
                  </Avatar>
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-sm text-foreground hover:text-primary transition-colors truncate">
                      {guild.name}
                    </p>
                    <div className="flex items-center gap-2">
                      {renderRating(guild.rating)}
                      {guild.isVerified && (
                        <CheckCircle className="size-3 text-neon-green" />
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
                <CardTitle className="text-base flex items-center gap-2">
                  <Filter className="size-4 text-neon-pink" />
                  Specialties
                </CardTitle>
              </CardHeader>
              <CardContent className="flex flex-wrap gap-2">
                {Object.keys(specialtyVariants).map((specialty) => (
                  <Badge
                    key={specialty}
                    variant={specialtyVariants[specialty]}
                    size="sm"
                    className="cursor-pointer hover:opacity-80"
                    onClick={() =>
                      setSelectedSpecialty(
                        selectedSpecialty === specialty ? "all" : specialty
                      )
                    }
                  >
                    {specialty}
                  </Badge>
                ))}
              </CardContent>
            </Card>
          )}

          {/* Footer */}
          <div className="text-xs text-muted-foreground space-x-2 px-2">
            <Link href="/terms" className="hover:underline">
              Terms
            </Link>
            <span>&middot;</span>
            <Link href="/privacy" className="hover:underline">
              Privacy
            </Link>
            <span>&middot;</span>
            <Link href="/help" className="hover:underline">
              Help
            </Link>
            <p className="mt-2">&copy; 2024 ArdaNova</p>
          </div>
        </>
      }
    >
      {/* Header */}
      <div className="sticky top-0 z-10 bg-background/95 backdrop-blur border-b-2 border-border">
        <div className="p-4 flex items-center justify-between">
          <h1 className="text-xl font-bold text-foreground flex items-center gap-2">
            <Users className="size-5 text-neon-pink" />
            People
          </h1>
          {activeSection === "guilds" && (
            <Button variant="neon" size="sm" asChild>
              <Link href="/guilds/create">
                <Plus className="size-4 mr-2" />
                Create Guild
              </Link>
            </Button>
          )}
        </div>

        {/* Primary Section Tabs */}
        <div className="flex border-b-2 border-border">
          {sectionTabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => {
                  setActiveSection(tab.id);
                  setSearchQuery("");
                }}
                className={cn(
                  "flex-1 flex items-center justify-center gap-2 py-3 text-sm font-medium transition-colors relative",
                  activeSection === tab.id
                    ? "text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-card"
                )}
              >
                <Icon className="size-4" />
                {tab.label}
                {activeSection === tab.id && (
                  <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
                )}
              </button>
            );
          })}
        </div>

        {/* Search */}
        <div className="px-4 py-3 flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder={
                activeSection === "guilds"
                  ? "Search guilds..."
                  : "Search people..."
              }
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
          {activeSection === "guilds" && (
            <Button
              variant={showFilters ? "neon" : "outline"}
              size="sm"
              onClick={() => setShowFilters(!showFilters)}
              className="gap-1.5"
            >
              <SlidersHorizontal className="size-4" />
              Filters
            </Button>
          )}
        </div>

        {/* Expanded Filters (guilds only) */}
        {showFilters && activeSection === "guilds" && (
          <div className="px-4 pb-4 space-y-3 border-t border-border pt-3">
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="text-xs text-muted-foreground mb-1.5 block">
                  Specialty
                </label>
                <select
                  value={selectedSpecialty}
                  onChange={(e) => setSelectedSpecialty(e.target.value)}
                  className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                <label className="text-xs text-muted-foreground mb-1.5 block">
                  Minimum Rating
                </label>
                <select
                  value={selectedRating}
                  onChange={(e) => setSelectedRating(e.target.value)}
                  className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
        <div className="flex border-b-2 border-border">
          {currentSubTabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setCurrentSubTab(tab.id)}
                className={cn(
                  "flex-1 flex items-center justify-center gap-2 py-2.5 text-xs font-medium transition-colors relative",
                  currentSubTab === tab.id
                    ? "text-primary"
                    : "text-muted-foreground hover:text-foreground hover:bg-card"
                )}
              >
                <Icon className="size-3.5" />
                <span className="hidden sm:inline">{tab.label}</span>
                {currentSubTab === tab.id && (
                  <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-primary" />
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Content */}
      <div>
        {activeSection === "guilds" ? (
          // Guilds Feed
          guildsLoading ? (
            <div className="flex items-center justify-center py-20">
              <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
            </div>
          ) : sortedGuilds.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 px-4">
              <Users className="size-12 text-muted-foreground mb-4" />
              <p className="text-lg font-medium text-foreground">
                No guilds found
              </p>
              <p className="text-muted-foreground mt-1">
                Be the first to create one!
              </p>
              <Button variant="neon" className="mt-4" asChild>
                <Link href="/guilds/create">
                  <Plus className="size-4 mr-2" />
                  Create Guild
                </Link>
              </Button>
            </div>
          ) : (
            sortedGuilds.map((guild) => (
              <article
                key={guild.id}
                className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
              >
                <div className="p-4">
                  <div className="flex items-start gap-3">
                    <Link
                      href={`/guilds/${guild.slug}`}
                      className="shrink-0"
                    >
                      <Avatar className="size-12 border-2 border-border hover:border-primary transition-colors">
                        {guild.logo ? (
                          <AvatarImage src={guild.logo} alt={guild.name} />
                        ) : null}
                        <AvatarFallback className="bg-neon-pink/20 text-neon-pink">
                          <Briefcase className="size-6" />
                        </AvatarFallback>
                      </Avatar>
                    </Link>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <Link
                          href={`/guilds/${guild.slug}`}
                          className="font-medium text-foreground hover:text-primary transition-colors"
                        >
                          {guild.name}
                        </Link>
                        {guild.isVerified && (
                          <CheckCircle className="size-4 text-neon-green" />
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
                      <div className="flex items-center gap-3 mt-1">
                        {renderRating(guild.rating)}
                        <span className="text-sm text-muted-foreground">
                          {guild.projectsCount ?? 0} projects completed
                        </span>
                      </div>
                    </div>

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

                  <Link
                    href={`/guilds/${guild.slug}`}
                    className="block mt-3 pl-15"
                  >
                    <p className="text-foreground line-clamp-3">
                      {guild.description}
                    </p>

                    {guild.specialties && (
                      <div className="flex flex-wrap gap-2 mt-3">
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

                    <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground">
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

                  <div className="mt-4 pl-15 flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="sm"
                      className="gap-1.5 text-muted-foreground hover:text-neon-pink"
                    >
                      <Star className="size-4" />
                      <span className="text-xs">Rate</span>
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="gap-1.5 text-muted-foreground hover:text-primary"
                    >
                      <MessageCircle className="size-4" />
                      <span className="text-xs">Contact</span>
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
          )
        ) : // People Feed
        usersLoading ? (
          <div className="flex items-center justify-center py-20">
            <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          </div>
        ) : sortedUsers.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-20 px-4">
            <UserIcon className="size-12 text-muted-foreground mb-4" />
            <p className="text-lg font-medium text-foreground">
              No people found
            </p>
            <p className="text-muted-foreground mt-1">
              Try adjusting your search or filters.
            </p>
          </div>
        ) : (
          sortedUsers.map((user) => (
            <article
              key={user.id}
              className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
            >
              <div className="p-4">
                <div className="flex items-start gap-3">
                  <Link
                    href={`/dashboard/profile?id=${user.id}`}
                    className="shrink-0"
                  >
                    <Avatar className="size-12 border-2 border-border hover:border-primary transition-colors">
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

                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <Link
                        href={`/dashboard/profile?id=${user.id}`}
                        className="font-medium text-foreground hover:text-primary transition-colors"
                      >
                        {user.name ?? "Anonymous"}
                      </Link>
                      {user.verificationLevel !== "ANONYMOUS" && (
                        <Shield className="size-4 text-neon-green" />
                      )}
                      <Badge
                        variant={
                          userTypeVariants[user.userType] ?? "secondary"
                        }
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
                    <div className="flex items-center gap-3 mt-1">
                      <span className="text-sm text-muted-foreground">
                        Level {user.level}
                      </span>
                      <Badge variant="secondary" size="sm">
                        {user.tier}
                      </Badge>
                      {user.location && (
                        <span className="text-sm text-muted-foreground">
                          {user.location}
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <FollowButton userId={user.id} />
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon-sm">
                          <MoreHorizontal className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem>Copy profile link</DropdownMenuItem>
                        <DropdownMenuItem>Report</DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                </div>

                {user.bio && (
                  <div className="mt-3 pl-15">
                    <p className="text-foreground line-clamp-2">{user.bio}</p>
                  </div>
                )}

                <div className="mt-3 pl-15 flex items-center gap-4 text-sm text-muted-foreground">
                  <div className="flex items-center gap-1">
                    <Sparkles className="size-4" />
                    <span>{user.totalXP} XP</span>
                  </div>
                  {user.verificationLevel !== "ANONYMOUS" && (
                    <div className="flex items-center gap-1">
                      <Shield className="size-4 text-neon-green" />
                      <span className="text-neon-green">
                        {user.verificationLevel}
                      </span>
                    </div>
                  )}
                </div>

                <div className="mt-4 pl-15 flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="gap-1.5 text-muted-foreground hover:text-primary"
                  >
                    <MessageCircle className="size-4" />
                    <span className="text-xs">Message</span>
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
        {((activeSection === "guilds" && sortedGuilds.length > 0) ||
          (activeSection === "people" && sortedUsers.length > 0)) && (
          <div className="flex justify-center py-6">
            <Button variant="outline">
              Load more {activeSection === "guilds" ? "guilds" : "people"}
            </Button>
          </div>
        )}
      </div>
    </FeedLayout>
  );
}

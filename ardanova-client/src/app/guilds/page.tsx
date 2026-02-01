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
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
  Award,
  SlidersHorizontal,
  X,
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

// Feed tabs for guilds
const guildTabs = [
  { id: "all", label: "All Guilds", icon: Users },
  { id: "verified", label: "Verified", icon: CheckCircle },
  { id: "top-rated", label: "Top Rated", icon: Star },
  { id: "newest", label: "Newest", icon: Clock },
];

// Specialty badge variants
const specialtyVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  "Web Development": "neon",
  "Mobile Development": "neon-purple",
  "UI/UX Design": "neon-pink",
  "Data Science": "neon-green",
  "Marketing": "warning",
  "Finance": "neon-green",
  "Legal": "secondary",
  "Strategy": "neon-purple",
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

function renderRating(rating: number | null) {
  if (!rating) return null;
  return (
    <div className="flex items-center gap-1 text-sm">
      <Star className="size-4 fill-neon-yellow text-neon-yellow" />
      <span className="font-medium text-foreground">{Number(rating).toFixed(1)}</span>
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
  const { data: guildsResult, isLoading } = api.guild.getAll.useQuery({
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
      const matchesDescription = guild.description?.toLowerCase().includes(query);
      const matchesSpecialties = guild.specialties?.toLowerCase().includes(query);
      if (!matchesName && !matchesDescription && !matchesSpecialties) return false;
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
      const diffDays = (now.getTime() - guildDate.getTime()) / (1000 * 60 * 60 * 24);

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
    total: guilds.length,
    verified: guilds.filter((g) => g.isVerified).length,
    totalProjects: guilds.reduce((sum, g) => sum + (g.projectsCount ?? 0), 0),
    totalReviews: guilds.reduce((sum, g) => sum + (g.reviewsCount ?? 0), 0),
  };

  // Top rated guilds for sidebar
  const topRatedGuilds = [...guilds]
    .sort((a, b) => (Number(b.rating) || 0) - (Number(a.rating) || 0))
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
                Guild Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Guilds</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Verified Guilds</span>
                <span className="font-medium text-neon-green">{stats.verified}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Projects Completed</span>
                <span className="font-medium text-foreground">{stats.totalProjects}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Reviews</span>
                <span className="font-medium text-foreground">{stats.totalReviews}</span>
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
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/guilds?tab=top-rated">View all top rated</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Specialties */}
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
                >
                  {specialty}
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
                <Users className="size-5 text-neon-pink" />
                Guilds
              </h1>
              <Button variant="neon" size="sm" asChild>
                <Link href="/guilds/create">
                  <Plus className="size-4 mr-2" />
                  Create Guild
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
                  placeholder="Search guilds..."
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
                  {/* Specialty Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Specialty
                    </label>
                    <select
                      value={selectedSpecialty}
                      onChange={(e) => setSelectedSpecialty(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Minimum Rating
                    </label>
                    <select
                      value={selectedRating}
                      onChange={(e) => setSelectedRating(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Projects Completed
                    </label>
                    <select
                      value={selectedProjects}
                      onChange={(e) => setSelectedProjects(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
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
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Created
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
                      {selectedSpecialty !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {specialtyFilters.find((f) => f.id === selectedSpecialty)?.label}
                          <button onClick={() => setSelectedSpecialty("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedRating !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {ratingFilters.find((f) => f.id === selectedRating)?.label}
                          <button onClick={() => setSelectedRating("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedProjects !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {projectsFilters.find((f) => f.id === selectedProjects)?.label}
                          <button onClick={() => setSelectedProjects("all")}>
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
              {guildTabs.map((tab) => {
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

          {/* Guilds Feed */}
          <div>
            {isLoading ? (
              <div className="flex items-center justify-center py-20">
                <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              </div>
            ) : filteredGuilds.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <Users className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No guilds found</p>
                <p className="text-muted-foreground mt-1">Be the first to create one!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/guilds/create">
                    <Plus className="size-4 mr-2" />
                    Create Guild
                  </Link>
                </Button>
              </div>
            ) : (
              filteredGuilds.map((guild) => (
                <article
                  key={guild.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
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
                          <Badge variant="neon-pink" size="sm">Guild</Badge>
                          <span className="text-muted-foreground text-sm">·</span>
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

                    {/* Guild Content */}
                    <Link href={`/guilds/${guild.slug}`} className="block mt-3 pl-15">
                      <p className="text-foreground line-clamp-3">
                        {guild.description}
                      </p>

                      {/* Specialties */}
                      {guild.specialties && (
                        <div className="flex flex-wrap gap-2 mt-3">
                          {guild.specialties.split(",").slice(0, 4).map((specialty, i) => {
                            const trimmed = specialty.trim();
                            return (
                              <Badge
                                key={i}
                                variant={specialtyVariants[trimmed] ?? "secondary"}
                                size="sm"
                              >
                                {trimmed}
                              </Badge>
                            );
                          })}
                        </div>
                      )}

                      {/* Stats */}
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

                    {/* Actions */}
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
            )}

            {/* Load More */}
            {filteredGuilds.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more guilds</Button>
              </div>
            )}
          </div>
    </FeedLayout>
  );
}

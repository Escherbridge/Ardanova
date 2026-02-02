"use client";

import { useState } from "react";
import Link from "next/link";
import {
  Search,
  Filter,
  Heart,
  Users,
  TrendingUp,
  FolderKanban,
  Plus,
  MessageCircle,
  Share2,
  Bookmark,
  MoreHorizontal,
  Sparkles,
  Clock,
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
import { useEnumOptions } from "~/hooks/use-enum";
import { FeedLayout } from "~/components/layouts/feed-layout";

// Feed tabs for projects
const projectTabs = [
  { id: "all", label: "All Projects", icon: FolderKanban },
  { id: "trending", label: "Trending", icon: TrendingUp },
  { id: "newest", label: "Newest", icon: Clock },
  { id: "funded", label: "Funded", icon: Sparkles },
];

// Category badge variants
const categoryVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary"> = {
  TECHNOLOGY: "neon",
  HEALTHCARE: "neon-pink",
  EDUCATION: "neon-purple",
  ENVIRONMENT: "neon-green",
  SOCIAL_IMPACT: "neon-pink",
  BUSINESS: "secondary",
  ARTS_CULTURE: "neon-purple",
  AGRICULTURE: "neon-green",
  FINANCE: "warning",
  OTHER: "secondary",
};

// Status badge variants
const statusVariants: Record<string, "neon" | "neon-pink" | "neon-green" | "neon-purple" | "warning" | "secondary" | "destructive"> = {
  DRAFT: "secondary",
  PUBLISHED: "neon",
  SEEKING_SUPPORT: "warning",
  FUNDED: "neon-green",
  IN_PROGRESS: "neon-purple",
  COMPLETED: "neon-green",
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


const fundingFilters = [
  { id: "all", label: "Any Funding" },
  { id: "0-1000", label: "Under $1,000" },
  { id: "1000-10000", label: "$1,000 - $10,000" },
  { id: "10000-50000", label: "$10,000 - $50,000" },
  { id: "50000+", label: "Over $50,000" },
];

const timeFilters = [
  { id: "all", label: "All Time" },
  { id: "today", label: "Today" },
  { id: "week", label: "This Week" },
  { id: "month", label: "This Month" },
  { id: "year", label: "This Year" },
];

export default function ProjectsPage() {
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState("all");
  const [selectedProjectType, setSelectedProjectType] = useState("all");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [selectedFunding, setSelectedFunding] = useState("all");
  const [selectedTime, setSelectedTime] = useState("all");

  // API-driven enum filters
  const { options: categoryOptions } = useEnumOptions("ProjectCategory");
  const categoryFilters = [{ id: "all", label: "All Categories" }, ...categoryOptions];
  const { options: projectTypeOptions } = useEnumOptions("ProjectType");
  const projectTypeFilters = [{ id: "all", label: "All Types" }, ...projectTypeOptions];
  const { options: statusOptions } = useEnumOptions("ProjectStatus");
  const statusFilters = [{ id: "all", label: "All Statuses" }, ...statusOptions];

  // Fetch projects from API
  const { data: projectsResult, isLoading } = api.project.getAll.useQuery({
    limit: 50,
  });

  const projects = projectsResult?.items || [];

  // Filter projects based on all criteria
  const filteredProjects = projects.filter((project) => {
    // Tab filter
    if (activeTab === "trending" && (project.votesCount ?? 0) === 0) return false;
    if (activeTab === "funded" && project.status !== "FUNDED" && project.status !== "COMPLETED") return false;

    // Search query
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesTitle = project.title.toLowerCase().includes(query);
      const matchesDescription = project.description?.toLowerCase().includes(query);
      const matchesTags = project.tags?.toLowerCase().includes(query);
      if (!matchesTitle && !matchesDescription && !matchesTags) return false;
    }

    // Category filter
    if (selectedCategory !== "all" && !((project as any).categories as string[] ?? []).includes(selectedCategory)) return false;

    // Project type filter
    if (selectedProjectType !== "all" && (project as any).projectType !== selectedProjectType) return false;

    // Status filter
    if (selectedStatus !== "all" && project.status !== selectedStatus) return false;

    // Funding filter
    if (selectedFunding !== "all") {
      const funding = Number(project.currentFunding || 0);
      if (selectedFunding === "0-1000" && funding >= 1000) return false;
      if (selectedFunding === "1000-10000" && (funding < 1000 || funding >= 10000)) return false;
      if (selectedFunding === "10000-50000" && (funding < 10000 || funding >= 50000)) return false;
      if (selectedFunding === "50000+" && funding < 50000) return false;
    }

    // Time filter
    if (selectedTime !== "all") {
      const now = new Date();
      const projectDate = new Date(project.createdAt);
      const diffDays = (now.getTime() - projectDate.getTime()) / (1000 * 60 * 60 * 24);

      if (selectedTime === "today" && diffDays > 1) return false;
      if (selectedTime === "week" && diffDays > 7) return false;
      if (selectedTime === "month" && diffDays > 30) return false;
      if (selectedTime === "year" && diffDays > 365) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery ||
    selectedCategory !== "all" ||
    selectedProjectType !== "all" ||
    selectedStatus !== "all" ||
    selectedFunding !== "all" ||
    selectedTime !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedCategory("all");
    setSelectedProjectType("all");
    setSelectedStatus("all");
    setSelectedFunding("all");
    setSelectedTime("all");
  };

  const activeFilterCount =
    (selectedCategory !== "all" ? 1 : 0) +
    (selectedProjectType !== "all" ? 1 : 0) +
    (selectedStatus !== "all" ? 1 : 0) +
    (selectedFunding !== "all" ? 1 : 0) +
    (selectedTime !== "all" ? 1 : 0);

  // Stats for sidebar
  const stats = {
    total: projects.length,
    funded: projects.filter((p) => p.status === "FUNDED" || p.status === "COMPLETED").length,
    totalFunding: projects.reduce((sum, p) => sum + Number(p.currentFunding || 0), 0),
    totalSupporters: projects.reduce((sum, p) => sum + (p.supportersCount || 0), 0),
  };

  // Trending projects for sidebar
  const trendingProjects = [...projects]
    .sort((a, b) => (b.votesCount || 0) - (a.votesCount || 0))
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
                Platform Stats
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Projects</span>
                <span className="font-medium text-foreground">{stats.total}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Funded Projects</span>
                <span className="font-medium text-neon-green">{stats.funded}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Raised</span>
                <span className="font-medium text-foreground">${stats.totalFunding.toLocaleString()}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-sm text-muted-foreground">Total Supporters</span>
                <span className="font-medium text-foreground">{stats.totalSupporters}</span>
              </div>
            </CardContent>
          </Card>

          {/* Trending Projects */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <TrendingUp className="size-4 text-primary" />
                Trending Projects
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {trendingProjects.map((project) => (
                <Link
                  key={project.id}
                  href={`/projects/${project.id}`}
                  className="block"
                >
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-sm text-foreground hover:text-primary transition-colors line-clamp-1">
                        {project.title}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {project.supportersCount || 0} supporters
                      </p>
                    </div>
                    {((project as any).categories as string[] ?? []).slice(0, 1).map((cat: string) => (
                      <Badge
                        key={cat}
                        variant={categoryVariants[cat] || "secondary"}
                        size="sm"
                      >
                        {cat.replace("_", " ")}
                      </Badge>
                    ))}
                  </div>
                  {project.fundingGoal && Number(project.fundingGoal) > 0 && (
                    <Progress
                      value={Math.min((Number(project.currentFunding || 0) / Number(project.fundingGoal)) * 100, 100)}
                      variant="neon"
                      className="h-1"
                    />
                  )}
                </Link>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/projects?tab=trending">View all trending</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Categories */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Filter className="size-4 text-neon-pink" />
                Categories
              </CardTitle>
            </CardHeader>
            <CardContent className="flex flex-wrap gap-2">
              {Object.keys(categoryVariants).map((category) => (
                <Badge
                  key={category}
                  variant={categoryVariants[category]}
                  size="sm"
                  className="cursor-pointer hover:opacity-80"
                >
                  {category.replace("_", " ")}
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
                <FolderKanban className="size-5 text-primary" />
                Projects
              </h1>
              <Button variant="neon" size="sm" asChild>
                <Link href="/projects/create">
                  <Plus className="size-4 mr-2" />
                  New Project
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
                  placeholder="Search projects..."
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
                  {/* Category Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Category
                    </label>
                    <select
                      value={selectedCategory}
                      onChange={(e) => setSelectedCategory(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {categoryFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Project Type Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Project Type
                    </label>
                    <select
                      value={selectedProjectType}
                      onChange={(e) => setSelectedProjectType(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {projectTypeFilters.map((filter) => (
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
                      {statusFilters.map((filter) => (
                        <option key={filter.id} value={filter.id}>
                          {filter.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  {/* Funding Filter */}
                  <div>
                    <label className="text-xs text-muted-foreground mb-1.5 block">
                      Funding Range
                    </label>
                    <select
                      value={selectedFunding}
                      onChange={(e) => setSelectedFunding(e.target.value)}
                      className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                    >
                      {fundingFilters.map((filter) => (
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
                      {selectedCategory !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {categoryFilters.find((f) => f.id === selectedCategory)?.label}
                          <button onClick={() => setSelectedCategory("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedProjectType !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {projectTypeFilters.find((f) => f.id === selectedProjectType)?.label}
                          <button onClick={() => setSelectedProjectType("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedStatus !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {statusFilters.find((f) => f.id === selectedStatus)?.label}
                          <button onClick={() => setSelectedStatus("all")}>
                            <X className="size-3" />
                          </button>
                        </Badge>
                      )}
                      {selectedFunding !== "all" && (
                        <Badge variant="secondary" size="sm" className="gap-1">
                          {fundingFilters.find((f) => f.id === selectedFunding)?.label}
                          <button onClick={() => setSelectedFunding("all")}>
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
              {projectTabs.map((tab) => {
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

          {/* Projects Feed */}
          <div>
            {isLoading ? (
              <div className="flex items-center justify-center py-20">
                <div className="size-8 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              </div>
            ) : filteredProjects.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4">
                <FolderKanban className="size-12 text-muted-foreground mb-4" />
                <p className="text-lg font-medium text-foreground">No projects found</p>
                <p className="text-muted-foreground mt-1">Be the first to create one!</p>
                <Button variant="neon" className="mt-4" asChild>
                  <Link href="/projects/create">
                    <Plus className="size-4 mr-2" />
                    Create Project
                  </Link>
                </Button>
              </div>
            ) : (
              filteredProjects.map((project) => (
                <article
                  key={project.id}
                  className="border-b-2 border-border bg-card hover:bg-card/80 transition-colors"
                >
                  <div className="p-4">
                    {/* Header */}
                    <div className="flex items-start gap-3">
                      <Link
                        href={`/dashboard/profile/${project.createdById}`}
                        className="shrink-0"
                      >
                        <Avatar className="size-10 border-2 border-border hover:border-primary transition-colors">
                          <AvatarImage src={(project as any).createdBy?.image || undefined} />
                          <AvatarFallback>
                            {(project as any).createdBy?.name?.charAt(0) || "U"}
                          </AvatarFallback>
                        </Avatar>
                      </Link>

                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <Link
                            href={`/dashboard/profile/${project.createdById}`}
                            className="font-medium text-foreground hover:text-primary transition-colors"
                          >
                            {(project as any).createdBy?.name || "Unknown User"}
                          </Link>
                          <Badge variant="secondary" size="sm">Founder</Badge>
                          <span className="text-muted-foreground text-sm">·</span>
                          <span className="text-muted-foreground text-sm">
                            {formatRelativeTime(new Date(project.createdAt))}
                          </span>
                        </div>
                        <p className="text-sm text-muted-foreground mt-0.5">
                          created a new project
                        </p>
                      </div>

                      <div className="flex items-center gap-2">
                        <Badge
                          variant={statusVariants[project.status] || "secondary"}
                          size="sm"
                        >
                          {project.status.replace("_", " ")}
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

                    {/* Project Content */}
                    <Link href={`/projects/${project.id}`} className="block mt-3 pl-13">
                      <h3 className="font-semibold text-lg text-foreground hover:text-primary transition-colors">
                        {project.title}
                      </h3>
                      <p className="text-foreground mt-2 line-clamp-3">
                        {project.description}
                      </p>

                      {/* Category & Tags */}
                      <div className="flex flex-wrap gap-2 mt-3">
                        {((project as any).categories as string[] ?? []).map((cat: string) => (
                          <Badge
                            key={cat}
                            variant={categoryVariants[cat] || "secondary"}
                            size="sm"
                          >
                            {cat.replace("_", " ")}
                          </Badge>
                        ))}
                        {project.tags?.split(",").slice(0, 2).map((tag, i) => (
                          <Badge key={i} variant="secondary" size="sm">
                            {tag.trim()}
                          </Badge>
                        ))}
                      </div>

                      {/* Funding Progress */}
                      {project.fundingGoal && Number(project.fundingGoal) > 0 && (
                        <div className="mt-4">
                          <div className="flex justify-between text-sm mb-1">
                            <span className="text-muted-foreground">Funding Progress</span>
                            <span className="font-medium text-foreground">
                              ${Number(project.currentFunding || 0).toLocaleString()} / ${Number(project.fundingGoal).toLocaleString()}
                            </span>
                          </div>
                          <Progress
                            value={Math.min((Number(project.currentFunding || 0) / Number(project.fundingGoal)) * 100, 100)}
                            variant="neon"
                            className="h-2"
                          />
                        </div>
                      )}

                      {/* Stats */}
                      <div className="flex items-center gap-4 mt-3 text-sm text-muted-foreground">
                        <div className="flex items-center gap-1">
                          <Users className="size-4" />
                          <span>{project.supportersCount || 0} supporters</span>
                        </div>
                        <div className="flex items-center gap-1">
                          <Heart className="size-4" />
                          <span>{project.votesCount || 0} votes</span>
                        </div>
                      </div>
                    </Link>

                    {/* Actions */}
                    <div className="mt-4 pl-13 flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="gap-1.5 text-muted-foreground hover:text-neon-pink"
                      >
                        <Heart className="size-4" />
                        <span className="text-xs">{project.votesCount || ""}</span>
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
              ))
            )}

            {/* Load More */}
            {filteredProjects.length > 0 && (
              <div className="flex justify-center py-6">
                <Button variant="outline">Load more projects</Button>
              </div>
            )}
          </div>
    </FeedLayout>
  );
}

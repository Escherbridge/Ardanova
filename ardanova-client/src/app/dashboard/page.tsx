"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import {
  TrendingUp,
  Users,
  Calendar,
  Home,
  Search,
  SlidersHorizontal,
  X,
  ChevronDown,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Progress } from "~/components/ui/progress";
import {
  Feed,
  ComposeBox,
  EntityPreview,
  type FeedCardProps,
  type EntityData,
  type FeedTab,
} from "~/components/feed";
import { useSession } from "next-auth/react";
import { api } from "~/trpc/react";
import { cn } from "~/lib/utils";
import { FeedLayout } from "~/components/layouts/feed-layout";
import type { Event as CalendarEvent } from "~/lib/api/ardanova/endpoints/events";

// Sample feed data - in production this would come from API
const sampleFeedItems: FeedCardProps[] = [
  {
    id: "1",
    type: "project_update",
    author: {
      id: "u1",
      name: "Sarah Chen",
      avatar: "https://i.pravatar.cc/150?u=sarah",
      badge: "Founder",
    },
    entity: {
      id: "p1",
      type: "project",
      name: "EcoWaste Solutions",
      slug: "ecowaste-solutions",
    },
    content: {
      title: "Milestone Reached: 100 Co-owners!",
      text: "We just hit 100 co-owners on our sustainable waste management platform. Thank you to everyone who believes in building a cleaner future together. Next up: launching our pilot program in three cities.",
    },
    engagement: { likes: 47, comments: 12, shares: 8 },
    timestamp: new Date(Date.now() - 1000 * 60 * 30),
  },
  {
    id: "2",
    type: "task_completed",
    author: {
      id: "u2",
      name: "Marcus Rodriguez",
      avatar: "https://i.pravatar.cc/150?u=marcus",
    },
    entity: {
      id: "p2",
      type: "project",
      name: "HealthTrack",
      slug: "healthtrack",
    },
    content: {
      text: "Just completed the mobile app redesign. The new interface is much more accessible and works better on older devices. Ready for community review!",
      metadata: {
        "Task": "Mobile App Redesign",
        "Reward": "250 shares",
      },
    },
    engagement: { likes: 23, comments: 5, shares: 2 },
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 2),
  },
  {
    id: "3",
    type: "guild_activity",
    author: {
      id: "u3",
      name: "Design Guild",
      avatar: "https://i.pravatar.cc/150?u=guild",
      badge: "Guild",
    },
    entity: {
      id: "g1",
      type: "guild",
      name: "Design Guild",
      slug: "design-guild",
    },
    content: {
      title: "Weekly Design Critique Session",
      text: "Join us this Friday at 3pm EST for our weekly design critique. Bring your work-in-progress and get feedback from fellow designers. All skill levels welcome!",
    },
    engagement: { likes: 15, comments: 8, shares: 4 },
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 5),
  },
  {
    id: "4",
    type: "proposal",
    author: {
      id: "u4",
      name: "Alex Kim",
      avatar: "https://i.pravatar.cc/150?u=alex",
    },
    entity: {
      id: "p1",
      type: "project",
      name: "EcoWaste Solutions",
      slug: "ecowaste-solutions",
    },
    content: {
      title: "Proposal: Expand to European Markets",
      text: "I'm proposing we allocate 15% of our next funding round to expand operations to Germany and Netherlands. Both countries have strong environmental policies and receptive markets.",
      metadata: {
        "Voting ends": "in 5 days",
        "Current votes": "67% in favor",
      },
    },
    engagement: { likes: 34, comments: 28, shares: 12 },
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 12),
  },
  {
    id: "5",
    type: "milestone",
    author: {
      id: "u5",
      name: "Jordan Lee",
      avatar: "https://i.pravatar.cc/150?u=jordan",
    },
    entity: {
      id: "p3",
      type: "project",
      name: "EduConnect",
      slug: "educonnect",
    },
    content: {
      title: "1,000 Students Matched with Mentors!",
      text: "Our mentorship platform just crossed a major milestone. Over 1,000 students have now been connected with experienced mentors in their fields. The impact stories coming in are incredible.",
    },
    engagement: { likes: 89, comments: 24, shares: 31 },
    timestamp: new Date(Date.now() - 1000 * 60 * 60 * 24),
  },
];

// Dashboard-specific tabs (simplified)
const dashboardTabs: FeedTab[] = [
  { id: "feed", label: "Feed", icon: Home },
  { id: "trending", label: "Trending", icon: TrendingUp },
];

// Search filter options
const postTypeFilters = [
  { id: "all", label: "All Types" },
  { id: "project_update", label: "Project Updates" },
  { id: "task_completed", label: "Task Completions" },
  { id: "guild_activity", label: "Guild Activity" },
  { id: "proposal", label: "Proposals" },
  { id: "milestone", label: "Milestones" },
];

const timeRangeFilters = [
  { id: "all", label: "All Time" },
  { id: "today", label: "Today" },
  { id: "week", label: "This Week" },
  { id: "month", label: "This Month" },
];

export default function DashboardPage() {
  const { data: session } = useSession();
  const router = useRouter();
  const searchParams = useSearchParams();
  const [feedItems, setFeedItems] = useState(sampleFeedItems);
  const [selectedEntity, setSelectedEntity] = useState<EntityData | null>(null);
  const [isEntityPreviewOpen, setIsEntityPreviewOpen] = useState(false);
  const [showFilters, setShowFilters] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedType, setSelectedType] = useState("all");
  const [selectedTimeRange, setSelectedTimeRange] = useState("all");

  const user = session?.user;

  const { data: myProjectsData } = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: !!session?.user },
  );

  const { data: myGuilds } = api.guild.getMyGuilds.useQuery(undefined, {
    enabled: !!session?.user,
  });

  const composeScopes = useMemo(() => {
    const scopes: { type: string; id: string; name: string }[] = [];
    for (const p of myProjectsData?.items ?? []) {
      scopes.push({ type: "project", id: p.id, name: p.title });
    }
    for (const g of myGuilds ?? []) {
      scopes.push({ type: "guild", id: g.id, name: g.name });
    }
    return scopes;
  }, [myProjectsData?.items, myGuilds]);

  const { data: featuredProjects } = api.project.getFeatured.useQuery();
  const { data: discoverUsers } = api.user.getAll.useQuery({ limit: 12, page: 1 });
  const { data: upcomingEventsData } = api.event.getUpcoming.useQuery({ limit: 3 });

  const trendingProjects = useMemo(() => {
    const items = featuredProjects ?? [];
    return items.slice(0, 5).map((p) => ({
      id: p.id,
      slug: p.slug,
      name: p.title,
      category: p.categories?.[0] ?? "Project",
      coOwners: p.supportersCount ?? 0,
      progress:
        p.fundingGoal && p.fundingGoal > 0
          ? Math.min(100, Math.round((p.currentFunding / p.fundingGoal) * 100))
          : 0,
    }));
  }, [featuredProjects]);

  const suggestedUsers = useMemo(() => {
    const items = discoverUsers?.items ?? [];
    return items
      .filter((u) => u.id !== user?.id)
      .slice(0, 5)
      .map((u) => ({
        id: u.id,
        name: u.name ?? "Member",
        avatar: u.image ?? "",
        bio: u.bio ?? "",
      }));
  }, [discoverUsers?.items, user?.id]);

  const handleAuthorClick = (author: { id: string; name: string }) => {
    // Navigate to user's own profile if clicking on themselves, otherwise to the author's profile
    if (user?.id === author.id) {
      router.push("/dashboard/profile");
    } else {
      router.push(`/dashboard/profile/${author.id}`);
    }
  };

  const handleEntityClick = (entity: FeedCardProps["entity"]) => {
    if (entity) {
      setSelectedEntity({
        id: entity.id,
        type: entity.type,
        name: entity.name,
        slug: entity.slug,
        description:
          "A community-owned project building sustainable solutions for the future.",
        stats: {
          members: 124,
          tasks: 15,
          completed: 89,
          funding: 45000,
          fundingGoal: 100000,
        },
        tags: ["Sustainability", "Community", "Impact"],
        recentMembers: [
          { id: "u1", name: "Sarah Chen", avatar: "https://i.pravatar.cc/150?u=sarah" },
          { id: "u2", name: "Marcus Rodriguez", avatar: "https://i.pravatar.cc/150?u=marcus" },
          { id: "u4", name: "Alex Kim", avatar: "https://i.pravatar.cc/150?u=alex" },
        ],
        createdAt: new Date(2024, 0, 15),
      });
      setIsEntityPreviewOpen(true);
    }
  };

  const handlePostSubmit = (post: {
    text: string;
    scope: { type: string; id?: string; name?: string };
  }) => {
    const newPost: FeedCardProps = {
      id: `new-${Date.now()}`,
      type: "post",
      author: {
        id: user?.id || "current-user",
        name: user?.name || "You",
        avatar: user?.image || undefined,
      },
      content: {
        text: post.text,
      },
      engagement: { likes: 0, comments: 0, shares: 0 },
      timestamp: new Date(),
    };
    setFeedItems([newPost, ...feedItems]);
  };

  // Filter feed items based on search and filters
  const filteredFeedItems = feedItems.filter((item) => {
    // Search query filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesContent =
        item.content.text?.toLowerCase().includes(query) ||
        item.content.title?.toLowerCase().includes(query);
      const matchesAuthor = item.author.name.toLowerCase().includes(query);
      const matchesEntity = item.entity?.name.toLowerCase().includes(query);
      if (!matchesContent && !matchesAuthor && !matchesEntity) return false;
    }

    // Type filter
    if (selectedType !== "all" && item.type !== selectedType) return false;

    // Time range filter
    if (selectedTimeRange !== "all") {
      const now = new Date();
      const itemDate = new Date(item.timestamp);
      const diffMs = now.getTime() - itemDate.getTime();
      const diffDays = diffMs / (1000 * 60 * 60 * 24);

      if (selectedTimeRange === "today" && diffDays > 1) return false;
      if (selectedTimeRange === "week" && diffDays > 7) return false;
      if (selectedTimeRange === "month" && diffDays > 30) return false;
    }

    return true;
  });

  const hasActiveFilters =
    searchQuery || selectedType !== "all" || selectedTimeRange !== "all";

  const clearFilters = () => {
    setSearchQuery("");
    setSelectedType("all");
    setSelectedTimeRange("all");
  };

  return (
    <FeedLayout
      sidebar={
        <>
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
                <button
                  key={project.id}
                  onClick={() =>
                    handleEntityClick({
                      id: project.id,
                      type: "project",
                      name: project.name,
                      slug: project.slug,
                    })
                  }
                  className="w-full text-left"
                >
                  <div className="flex items-start justify-between mb-2">
                    <div>
                      <p className="font-medium text-sm text-foreground hover:text-primary transition-colors">
                        {project.name}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {project.coOwners} co-owners
                      </p>
                    </div>
                    <Badge variant="secondary" size="sm">
                      {project.category}
                    </Badge>
                  </div>
                  <Progress value={project.progress} variant="neon" className="h-1" />
                </button>
              ))}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/projects">View all projects</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Who to Follow */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Users className="size-4 text-neon-pink" />
                Who to Follow
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {suggestedUsers.map((suggestedUser) => (
                <div
                  key={suggestedUser.id}
                  className="flex items-center gap-3"
                >
                  <button
                    onClick={() => router.push(`/dashboard/profile/${suggestedUser.id}`)}
                    className="shrink-0"
                  >
                    <Avatar className="size-9 border-2 border-border hover:border-primary transition-colors">
                      <AvatarImage src={suggestedUser.avatar} />
                      <AvatarFallback>{suggestedUser.name.charAt(0)}</AvatarFallback>
                    </Avatar>
                  </button>
                  <button
                    onClick={() => router.push(`/dashboard/profile/${suggestedUser.id}`)}
                    className="flex-1 min-w-0 text-left"
                  >
                    <p className="font-medium text-sm text-foreground truncate hover:text-primary transition-colors">
                      {suggestedUser.name}
                    </p>
                    <p className="text-xs text-muted-foreground truncate">
                      {suggestedUser.bio}
                    </p>
                  </button>
                  <Button variant="outline" size="sm">
                    Follow
                  </Button>
                </div>
              ))}
              <Button variant="ghost" className="w-full text-sm">
                Show more
              </Button>
            </CardContent>
          </Card>

          {/* Upcoming Events */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base flex items-center gap-2">
                <Calendar className="size-4 text-neon-green" />
                Upcoming Events
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {(upcomingEventsData ?? []).length === 0 ? (
                <p className="text-sm text-muted-foreground">No upcoming events.</p>
              ) : (
                (upcomingEventsData as CalendarEvent[]).map((ev) => {
                  const start = new Date(ev.startDate);
                  return (
                    <Link
                      key={ev.id}
                      href="/events"
                      className="block p-2 border-2 border-border hover:border-primary transition-colors cursor-pointer"
                    >
                      <p className="font-medium text-sm text-foreground">{ev.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {start.toLocaleString(undefined, {
                          weekday: "short",
                          month: "short",
                          day: "numeric",
                          hour: "numeric",
                          minute: "2-digit",
                        })}{" "}
                        {ev.timezone}
                      </p>
                    </Link>
                  );
                })
              )}
              <Button variant="ghost" className="w-full text-sm" asChild>
                <Link href="/events">View calendar</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Footer Links */}
          <div className="text-xs text-muted-foreground space-x-2 px-2">
            <Link href="/terms" className="hover:underline">
              Terms
            </Link>
            <span>·</span>
            <Link href="/privacy" className="hover:underline">
              Privacy
            </Link>
            <span>·</span>
            <Link href="/help" className="hover:underline">
              Help
            </Link>
            <p className="mt-2">&copy; 2024 ArdaNova</p>
          </div>
        </>
      }
    >
      {/* Search Parameters Section */}
      <div className="sticky top-0 z-20 bg-background border-b-2 border-border">
        {/* Search Bar */}
        <div className="p-4 flex items-center gap-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search posts, projects, people..."
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
            {hasActiveFilters && !showFilters && (
              <Badge variant="neon" size="sm" className="ml-1">
                {(selectedType !== "all" ? 1 : 0) +
                  (selectedTimeRange !== "all" ? 1 : 0)}
              </Badge>
            )}
          </Button>
        </div>

        {/* Expanded Filters */}
        {showFilters && (
          <div className="px-4 pb-4 space-y-3 border-t border-border pt-3">
            <div className="flex flex-wrap gap-4">
              {/* Post Type Filter */}
              <div className="flex-1 min-w-[150px]">
                <label className="text-xs text-muted-foreground mb-1.5 block">
                  Post Type
                </label>
                <select
                  value={selectedType}
                  onChange={(e) => setSelectedType(e.target.value)}
                  className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                >
                  {postTypeFilters.map((filter) => (
                    <option key={filter.id} value={filter.id}>
                      {filter.label}
                    </option>
                  ))}
                </select>
              </div>

              {/* Time Range Filter */}
              <div className="flex-1 min-w-[150px]">
                <label className="text-xs text-muted-foreground mb-1.5 block">
                  Time Range
                </label>
                <select
                  value={selectedTimeRange}
                  onChange={(e) => setSelectedTimeRange(e.target.value)}
                  className="w-full px-3 py-2 bg-card border-2 border-border text-foreground text-sm focus:border-primary focus:outline-none appearance-none cursor-pointer"
                >
                  {timeRangeFilters.map((filter) => (
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
                      {postTypeFilters.find((f) => f.id === selectedType)?.label}
                      <button onClick={() => setSelectedType("all")}>
                        <X className="size-3" />
                      </button>
                    </Badge>
                  )}
                  {selectedTimeRange !== "all" && (
                    <Badge variant="secondary" size="sm" className="gap-1">
                      {timeRangeFilters.find((f) => f.id === selectedTimeRange)?.label}
                      <button onClick={() => setSelectedTimeRange("all")}>
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
      </div>

      <Feed
        tabs={dashboardTabs}
        initialTab="feed"
        items={filteredFeedItems}
        onEntityClick={handleEntityClick}
        onAuthorClick={handleAuthorClick}
        header={
          <ComposeBox
            user={{
              name: user?.name || "You",
              avatar: user?.image || undefined,
            }}
            onSubmit={handlePostSubmit}
            placeholder="Share an update with the community..."
            scopes={composeScopes}
          />
        }
        hasMore
        onLoadMore={() => {
          // Load more items
        }}
      />

      {/* Entity Preview Panel */}
      <EntityPreview
        entity={selectedEntity}
        isOpen={isEntityPreviewOpen}
        onClose={() => setIsEntityPreviewOpen(false)}
      />
    </FeedLayout>
  );
}

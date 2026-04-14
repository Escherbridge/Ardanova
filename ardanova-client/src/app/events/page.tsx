"use client";

import { useMemo, useState } from "react";
import { useSession } from "next-auth/react";
import { api } from "~/trpc/react";
import type { Event as ApiEvent } from "~/lib/api/ardanova/endpoints/events";
import { Badge } from "~/components/ui/badge";
import { Card, CardContent } from "~/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { Button } from "~/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import Link from "next/link";
import {
  Calendar,
  Plus,
  Search,
  Filter,
  X,
  MapPin,
  Clock,
  Users,
  Video,
  Globe,
  Star,
  CalendarDays,
  Sparkles,
  TrendingUp,
} from "lucide-react";
import { FeedLayout } from "~/components/layouts/feed-layout";
import { toast } from "sonner";

interface FeedTab {
  id: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
}

const eventTabs: FeedTab[] = [
  { id: "all", label: "All Events", icon: Calendar },
  { id: "upcoming", label: "Upcoming", icon: CalendarDays },
  { id: "past", label: "Past", icon: Clock },
  { id: "my-events", label: "My Events", icon: Star },
];

const eventTypeFilters = [
  { id: "all", label: "All Types" },
  { id: "meetup", label: "Meetups" },
  { id: "workshop", label: "Workshops" },
  { id: "hackathon", label: "Hackathons" },
  { id: "conference", label: "Conferences" },
  { id: "webinar", label: "Webinars" },
  { id: "ama", label: "AMAs" },
];

const formatFilters = [
  { id: "all", label: "All Formats" },
  { id: "virtual", label: "Virtual" },
  { id: "in-person", label: "In-Person" },
  { id: "hybrid", label: "Hybrid" },
];

const dateFilters = [
  { id: "all", label: "Any Time" },
  { id: "today", label: "Today" },
  { id: "this-week", label: "This Week" },
  { id: "this-month", label: "This Month" },
  { id: "next-month", label: "Next Month" },
];

const locationFilters = [
  { id: "all", label: "All Locations" },
  { id: "global", label: "Global" },
  { id: "americas", label: "Americas" },
  { id: "europe", label: "Europe" },
  { id: "asia", label: "Asia" },
  { id: "remote", label: "Remote Only" },
];

type EventCard = {
  id: string;
  title: string;
  description: string;
  type: string;
  format: "virtual" | "in-person" | "hybrid";
  date: string;
  startTime: string;
  endTime: string;
  timezone: string;
  location: string;
  organizer: { name: string; avatar: string };
  attendees: number;
  maxAttendees: number | null;
  isRegistered: boolean;
  tags: string[];
  featured: boolean;
};

function mapApiEventToCard(e: ApiEvent, registeredIds: Set<string>): EventCard {
  const start = new Date(e.startDate);
  const end = new Date(e.endDate);
  const org = (e as ApiEvent & { organizer?: { name?: string | null; image?: string | null } }).organizer;
  const format: EventCard["format"] =
    e.isOnline && e.location ? "hybrid" : e.isOnline ? "virtual" : "in-person";
  const rawType = String(e.type ?? "meetup").toLowerCase();
  let type = "meetup";
  if (rawType.includes("workshop")) type = "workshop";
  else if (rawType.includes("hack")) type = "hackathon";
  else if (rawType.includes("conference")) type = "conference";
  else if (rawType.includes("webinar")) type = "webinar";
  else if (rawType.includes("ama")) type = "ama";

  return {
    id: e.id,
    title: e.title,
    description: e.description ?? "",
    type,
    format,
    date: start.toISOString().slice(0, 10),
    startTime: start.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
    endTime: end.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
    timezone: e.timezone,
    location: e.location ?? "—",
    organizer: {
      name: org?.name ?? "Organizer",
      avatar: org?.image ?? "",
    },
    attendees: (e as ApiEvent & { attendeesCount?: number }).attendeesCount ?? 0,
    maxAttendees: e.maxAttendees ?? null,
    isRegistered: registeredIds.has(e.id),
    tags: [],
    featured: false,
  };
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return date.toLocaleDateString("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
  });
}

function getEventTypeColor(type: string): string {
  const colors: Record<string, string> = {
    meetup: "bg-neon/20 text-neon border-neon/30",
    workshop: "bg-neon-purple/20 text-neon-purple border-neon-purple/30",
    hackathon: "bg-neon-pink/20 text-neon-pink border-neon-pink/30",
    conference: "bg-neon-green/20 text-neon-green border-neon-green/30",
    webinar: "bg-blue-500/20 text-blue-400 border-blue-500/30",
    ama: "bg-orange-500/20 text-orange-400 border-orange-500/30",
  };
  return colors[type] ?? "bg-muted text-muted-foreground";
}

function getFormatIcon(format: string) {
  switch (format) {
    case "virtual":
      return <Video className="h-3 w-3" />;
    case "in-person":
      return <MapPin className="h-3 w-3" />;
    case "hybrid":
      return <Globe className="h-3 w-3" />;
    default:
      return <Globe className="h-3 w-3" />;
  }
}

export default function EventsPage() {
  const { data: session } = useSession();
  const utils = api.useUtils();
  const [activeTab, setActiveTab] = useState("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);

  // Filter states
  const [eventTypeFilter, setEventTypeFilter] = useState("all");
  const [formatFilter, setFormatFilter] = useState("all");
  const [dateFilter, setDateFilter] = useState("all");
  const [locationFilter, setLocationFilter] = useState("all");

  const activeFilterCount = [
    eventTypeFilter,
    formatFilter,
    dateFilter,
    locationFilter,
  ].filter((f) => f !== "all").length;

  const hasActiveFilters = activeFilterCount > 0 || searchQuery.length > 0;

  const clearFilters = () => {
    setEventTypeFilter("all");
    setFormatFilter("all");
    setDateFilter("all");
    setLocationFilter("all");
    setSearchQuery("");
  };

  const { data: eventsResult, isLoading: eventsLoading } = api.event.getAll.useQuery({
    limit: 50,
    page: 1,
    search: searchQuery || undefined,
  });

  const { data: registeredList } = api.event.getRegisteredEvents.useQuery(undefined, {
    enabled: !!session?.user,
  });

  const registerMutation = api.event.register.useMutation({
    onSuccess: () => {
      void utils.event.getAll.invalidate();
      void utils.event.getRegisteredEvents.invalidate();
      toast.success("Registered");
    },
    onError: (e) => toast.error(e.message),
  });

  const registeredIds = useMemo(() => {
    const ids = (registeredList ?? []).map((x) => String((x as { id: string }).id));
    return new Set(ids);
  }, [registeredList]);

  const sampleEvents = useMemo(
    () =>
      (eventsResult?.items ?? []).map((e) => mapApiEventToCard(e as ApiEvent, registeredIds)),
    [eventsResult?.items, registeredIds],
  );

  // Filter events
  const filteredEvents = sampleEvents.filter((event) => {
    if (searchQuery) {
      const query = searchQuery.toLowerCase();
      const matchesSearch =
        event.title.toLowerCase().includes(query) ||
        event.description.toLowerCase().includes(query) ||
        event.tags.some((tag) => tag.toLowerCase().includes(query));
      if (!matchesSearch) return false;
    }

    if (eventTypeFilter !== "all" && event.type !== eventTypeFilter)
      return false;
    if (formatFilter !== "all" && event.format !== formatFilter) return false;

    // Tab filtering
    if (activeTab === "my-events" && !event.isRegistered) return false;

    return true;
  });

  return (
    <FeedLayout
      sidebar={
        <>
          <div className="space-y-6">
            {/* Events Stats */}
            {/* <Card className="bg-muted/30 border-border/50">
              <CardContent className="p-4">
                <h3 className="font-semibold mb-4 flex items-center gap-2">
                  <TrendingUp className="h-4 w-4 text-neon" />
                  Event Stats
                </h3>
                <div className="space-y-3">
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">
                      Upcoming Events
                    </span>
                    <span className="font-semibold text-neon">24</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">
                      Your Registrations
                    </span>
                    <span className="font-semibold">3</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">
                      Events This Month
                    </span>
                    <span className="font-semibold">12</span>
                  </div>
                  <div className="flex justify-between items-center">
                    <span className="text-sm text-muted-foreground">
                      Total Attendees
                    </span>
                    <span className="font-semibold">2.4K</span>
                  </div>
                </div>
              </CardContent>
            </Card> */}

            {/* Featured Events */}
            <Card className="bg-muted/30 border-border/50">
              <CardContent className="p-4">
                <h3 className="font-semibold mb-4 flex items-center gap-2">
                  <Sparkles className="h-4 w-4 text-neon-pink" />
                  Featured Events
                </h3>
                <div className="space-y-3">
                  {sampleEvents
                    .slice(0, 3)
                    .map((event) => (
                      <div
                        key={event.id}
                        className="p-2 rounded-lg hover:bg-muted/50 transition-colors cursor-pointer"
                      >
                        <div className="flex items-center gap-2 mb-1">
                          <Badge
                            variant="outline"
                            className={`text-xs ${getEventTypeColor(event.type)}`}
                          >
                            {event.type}
                          </Badge>
                          <span className="text-xs text-muted-foreground">
                            {formatDate(event.date)}
                          </span>
                        </div>
                        <p className="text-sm font-medium truncate">{event.title}</p>
                        <p className="text-xs text-muted-foreground">
                          {event.attendees} attending
                        </p>
                      </div>
                    ))}
                </div>
              </CardContent>
            </Card>

            {/* Event Categories */}
            <Card className="bg-muted/30 border-border/50">
              <CardContent className="p-4">
                <h3 className="font-semibold mb-4">Popular Categories</h3>
                <div className="flex flex-wrap gap-2">
                  {eventTypeFilters.slice(1).map((type) => (
                    <Badge
                      key={type.id}
                      variant="outline"
                      className="cursor-pointer hover:bg-neon/20 hover:border-neon transition-colors"
                      onClick={() => {
                        setEventTypeFilter(type.id);
                        setShowFilters(true);
                      }}
                    >
                      {type.label}
                    </Badge>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </>
      }
    >
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 border-b border-border/50">
          <div className="p-4">
            <div className="flex items-center justify-between mb-4">
              <h1 className="text-2xl font-bold">Events</h1>
              <Button className="bg-neon hover:bg-neon/90 text-black" asChild>
                <Link href="/events/create">
                  <Plus className="h-4 w-4 mr-2" />
                  Create Event
                </Link>
              </Button>
            </div>

            {/* Search Bar */}
            <div className="flex gap-2 mb-4">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Search events..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-10 pr-10 py-2 bg-muted/50 border border-border/50 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 focus:border-neon/50"
                />
                {searchQuery && (
                  <button
                    onClick={() => setSearchQuery("")}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                  >
                    <X className="h-4 w-4" />
                  </button>
                )}
              </div>
              <Button
                variant="outline"
                size="icon"
                onClick={() => setShowFilters(!showFilters)}
                className={showFilters ? "bg-neon/20 border-neon" : ""}
              >
                <Filter className="h-4 w-4" />
                {activeFilterCount > 0 && (
                  <span className="absolute -top-1 -right-1 h-4 w-4 bg-neon text-black text-xs rounded-full flex items-center justify-center">
                    {activeFilterCount}
                  </span>
                )}
              </Button>
            </div>

            {/* Expandable Filters */}
            {showFilters && (
              <div className="mb-4 p-4 bg-muted/30 rounded-lg border border-border/50">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">
                      Event Type
                    </label>
                    <Select
                      value={eventTypeFilter}
                      onValueChange={setEventTypeFilter}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {eventTypeFilters.map((filter) => (
                          <SelectItem key={filter.id} value={filter.id}>
                            {filter.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">
                      Format
                    </label>
                    <Select value={formatFilter} onValueChange={setFormatFilter}>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {formatFilters.map((filter) => (
                          <SelectItem key={filter.id} value={filter.id}>
                            {filter.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">
                      Date
                    </label>
                    <Select value={dateFilter} onValueChange={setDateFilter}>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {dateFilters.map((filter) => (
                          <SelectItem key={filter.id} value={filter.id}>
                            {filter.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground mb-1 block">
                      Location
                    </label>
                    <Select
                      value={locationFilter}
                      onValueChange={setLocationFilter}
                    >
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {locationFilters.map((filter) => (
                          <SelectItem key={filter.id} value={filter.id}>
                            {filter.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>
              </div>
            )}

            {/* Active Filters Display */}
            {hasActiveFilters && (
              <div className="flex flex-wrap gap-2 mb-4">
                {searchQuery && (
                  <Badge
                    variant="secondary"
                    className="gap-1 cursor-pointer hover:bg-destructive/20"
                    onClick={() => setSearchQuery("")}
                  >
                    Search: {searchQuery}
                    <X className="h-3 w-3" />
                  </Badge>
                )}
                {eventTypeFilter !== "all" && (
                  <Badge
                    variant="secondary"
                    className="gap-1 cursor-pointer hover:bg-destructive/20"
                    onClick={() => setEventTypeFilter("all")}
                  >
                    Type:{" "}
                    {eventTypeFilters.find((f) => f.id === eventTypeFilter)?.label}
                    <X className="h-3 w-3" />
                  </Badge>
                )}
                {formatFilter !== "all" && (
                  <Badge
                    variant="secondary"
                    className="gap-1 cursor-pointer hover:bg-destructive/20"
                    onClick={() => setFormatFilter("all")}
                  >
                    Format:{" "}
                    {formatFilters.find((f) => f.id === formatFilter)?.label}
                    <X className="h-3 w-3" />
                  </Badge>
                )}
                {dateFilter !== "all" && (
                  <Badge
                    variant="secondary"
                    className="gap-1 cursor-pointer hover:bg-destructive/20"
                    onClick={() => setDateFilter("all")}
                  >
                    Date: {dateFilters.find((f) => f.id === dateFilter)?.label}
                    <X className="h-3 w-3" />
                  </Badge>
                )}
                {locationFilter !== "all" && (
                  <Badge
                    variant="secondary"
                    className="gap-1 cursor-pointer hover:bg-destructive/20"
                    onClick={() => setLocationFilter("all")}
                  >
                    Location:{" "}
                    {locationFilters.find((f) => f.id === locationFilter)?.label}
                    <X className="h-3 w-3" />
                  </Badge>
                )}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={clearFilters}
                  className="h-6 text-xs text-muted-foreground hover:text-foreground"
                >
                  Clear all
                </Button>
              </div>
            )}

            {/* Tabs */}
            <div className="flex gap-1 overflow-x-auto">
              {eventTabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-colors whitespace-nowrap ${
                    activeTab === tab.id
                      ? "bg-neon/20 text-neon"
                      : "text-muted-foreground hover:text-foreground hover:bg-muted/50"
                  }`}
                >
                  <tab.icon className="h-4 w-4" />
                  {tab.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Events Feed */}
        <div className="divide-y divide-border/50">
          {eventsLoading ? (
            <div className="p-8 text-center text-muted-foreground">Loading events…</div>
          ) : filteredEvents.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground">
              <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No events found matching your criteria.</p>
              {hasActiveFilters && (
                <Button
                  variant="link"
                  onClick={clearFilters}
                  className="mt-2 text-neon"
                >
                  Clear filters
                </Button>
              )}
            </div>
          ) : (
            filteredEvents.map((event) => (
              <div
                key={event.id}
                className="p-4 hover:bg-muted/30 transition-colors cursor-pointer"
              >
                <div className="flex gap-4">
                  {/* Date Block */}
                  <div className="flex-shrink-0 w-16 text-center">
                    <div className="bg-neon/20 rounded-lg p-2 border border-neon/30">
                      <div className="text-xs text-neon font-medium uppercase">
                        {new Date(event.date).toLocaleDateString("en-US", {
                          month: "short",
                        })}
                      </div>
                      <div className="text-2xl font-bold text-foreground">
                        {new Date(event.date).getDate()}
                      </div>
                    </div>
                  </div>

                  {/* Event Details */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2 mb-2">
                      <div className="flex items-center gap-2 flex-wrap">
                        <Badge
                          variant="outline"
                          className={getEventTypeColor(event.type)}
                        >
                          {event.type}
                        </Badge>
                        <Badge
                          variant="outline"
                          className="gap-1 text-muted-foreground"
                        >
                          {getFormatIcon(event.format)}
                          {event.format}
                        </Badge>
                        {event.featured && (
                          <Badge className="bg-neon-pink/20 text-neon-pink border-neon-pink/30 gap-1">
                            <Sparkles className="h-3 w-3" />
                            Featured
                          </Badge>
                        )}
                      </div>
                      {event.isRegistered && (
                        <Badge className="bg-neon-green/20 text-neon-green border-neon-green/30">
                          Registered
                        </Badge>
                      )}
                    </div>

                    <h3 className="font-semibold text-lg mb-1 truncate">
                      {event.title}
                    </h3>
                    <p className="text-sm text-muted-foreground mb-3 line-clamp-2">
                      {event.description}
                    </p>

                    <div className="flex flex-wrap gap-x-4 gap-y-2 text-sm text-muted-foreground mb-3">
                      <div className="flex items-center gap-1">
                        <Clock className="h-4 w-4" />
                        {event.startTime} - {event.endTime} {event.timezone}
                      </div>
                      <div className="flex items-center gap-1">
                        <MapPin className="h-4 w-4" />
                        {event.location}
                      </div>
                      <div className="flex items-center gap-1">
                        <Users className="h-4 w-4" />
                        {event.attendees}
                        {event.maxAttendees && ` / ${event.maxAttendees}`} attending
                      </div>
                    </div>

                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <Avatar className="h-6 w-6">
                          <AvatarImage src={event.organizer.avatar} />
                          <AvatarFallback className="text-xs">
                            {event.organizer.name.charAt(0)}
                          </AvatarFallback>
                        </Avatar>
                        <span className="text-sm text-muted-foreground">
                          Hosted by{" "}
                          <span className="text-foreground">
                            {event.organizer.name}
                          </span>
                        </span>
                      </div>
                      {!event.isRegistered ? (
                        <Button
                          size="sm"
                          className="bg-neon hover:bg-neon/90 text-black"
                          disabled={!session?.user || registerMutation.isPending}
                          onClick={(evClick) => {
                            evClick.stopPropagation();
                            if (!session?.user) {
                              toast.error("Sign in to register");
                              return;
                            }
                            registerMutation.mutate({ eventId: event.id });
                          }}
                        >
                          Register
                        </Button>
                      ) : (
                        <Button size="sm" variant="outline">
                          View Details
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
    </FeedLayout>
  );
}

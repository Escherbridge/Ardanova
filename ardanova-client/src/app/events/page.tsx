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
  CalendarDays,
} from "lucide-react";
import { FeedLayout } from "~/components/layouts/feed-layout";
import { handleTabListKeyDown } from "~/lib/accessibility";
import { toast } from "sonner";

interface FeedTab {
  id: string;
  label: string;
}

const eventTabs: FeedTab[] = [
  { id: "all", label: "All Events" },
  { id: "upcoming", label: "Upcoming" },
  { id: "past", label: "Past" },
  { id: "my-events", label: "My Events" },
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
  attendees: number | null;
  maxAttendees: number | null;
  isRegistered: boolean;
  tags: string[];
};

function mapApiEventToCard(e: ApiEvent, registeredIds: Set<string>): EventCard {
  const start = new Date(e.startDate);
  const end = new Date(e.endDate);
  const org = (
    e as ApiEvent & {
      organizer?: { name?: string | null; image?: string | null };
    }
  ).organizer;
  const attendeesCount = (e as ApiEvent & { attendeesCount?: number })
    .attendeesCount;
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
    startTime: start.toLocaleTimeString([], {
      hour: "2-digit",
      minute: "2-digit",
    }),
    endTime: end.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
    timezone: e.timezone,
    location: e.location ?? "—",
    organizer: {
      name: org?.name ?? "Organizer",
      avatar: org?.image ?? "",
    },
    attendees: typeof attendeesCount === "number" ? attendeesCount : null,
    maxAttendees: e.maxAttendees ?? null,
    isRegistered: registeredIds.has(e.id),
    tags: [],
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

  const {
    data: eventsResult,
    isLoading: eventsLoading,
    error: eventsError,
  } = api.event.getAll.useQuery({
    limit: 50,
    page: 1,
    search: searchQuery || undefined,
  });

  const { data: registeredList, error: registeredEventsError } =
    api.event.getRegisteredEvents.useQuery(undefined, {
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

  const unregisterMutation = api.event.unregister.useMutation({
    onSuccess: () => {
      void utils.event.getAll.invalidate();
      void utils.event.getRegisteredEvents.invalidate();
      toast.success("Unregistered");
    },
    onError: (e) => toast.error(e.message),
  });

  const registeredIds = useMemo(() => {
    const ids = (registeredList ?? []).map((event) => event.id);
    return new Set(ids);
  }, [registeredList]);

  const eventCards = useMemo(
    () =>
      (eventsResult?.items ?? []).map((event) =>
        mapApiEventToCard(event, registeredIds),
      ),
    [eventsResult?.items, registeredIds],
  );

  // Filter events
  const filteredEvents = eventCards.filter((event) => {
    const eventDate = new Date(`${event.date}T00:00:00`);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

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
    if (
      locationFilter === "remote" &&
      event.format !== "virtual" &&
      !event.location.toLowerCase().includes("remote") &&
      !event.location.toLowerCase().includes("online")
    ) {
      return false;
    }

    if (dateFilter !== "all") {
      const tomorrow = new Date(today);
      tomorrow.setDate(today.getDate() + 1);
      const weekEnd = new Date(today);
      weekEnd.setDate(today.getDate() + 7);
      const monthEnd = new Date(today.getFullYear(), today.getMonth() + 1, 1);
      const nextMonthStart = monthEnd;
      const nextMonthEnd = new Date(
        today.getFullYear(),
        today.getMonth() + 2,
        1,
      );

      if (
        dateFilter === "today" &&
        (eventDate < today || eventDate >= tomorrow)
      )
        return false;
      if (
        dateFilter === "this-week" &&
        (eventDate < today || eventDate >= weekEnd)
      )
        return false;
      if (
        dateFilter === "this-month" &&
        (eventDate < today || eventDate >= monthEnd)
      )
        return false;
      if (
        dateFilter === "next-month" &&
        (eventDate < nextMonthStart || eventDate >= nextMonthEnd)
      )
        return false;
    }

    // Tab filtering
    if (activeTab === "upcoming" && eventDate < today) return false;
    if (activeTab === "past" && eventDate >= today) return false;
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

            {/* A finite preview of the records loaded for this page. */}
            <Card className="bg-muted/30 border-border/50">
              <CardContent className="p-4">
                <h3 className="mb-4 flex items-center gap-2 font-semibold">
                  <CalendarDays className="text-neon-pink h-4 w-4" />
                  First events in this page
                </h3>
                <div className="space-y-3">
                  {eventCards.slice(0, 3).map((event) => (
                    <div
                      key={event.id}
                      className="border-border rounded-lg border p-2"
                    >
                      <div className="mb-1 flex items-center gap-2">
                        <Badge
                          variant="outline"
                          className={`text-xs ${getEventTypeColor(event.type)}`}
                        >
                          {event.type}
                        </Badge>
                        <span className="text-muted-foreground text-xs">
                          {formatDate(event.date)}
                        </span>
                      </div>
                      <p className="truncate text-sm font-medium">
                        {event.title}
                      </p>
                      <p className="text-muted-foreground text-xs">
                        {event.attendees === null
                          ? "Attendance unavailable"
                          : `${event.attendees} attending`}
                      </p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Event Categories */}
            <Card className="bg-muted/30 border-border/50">
              <CardContent className="p-4">
                <h3 className="mb-4 font-semibold">Popular Categories</h3>
                <div className="flex flex-wrap gap-2">
                  {eventTypeFilters.slice(1).map((type) => (
                    <button
                      key={type.id}
                      type="button"
                      aria-pressed={eventTypeFilter === type.id}
                      className="focus-visible:ring-ring min-h-11 focus-visible:ring-2 focus-visible:outline-none"
                      onClick={() => {
                        setEventTypeFilter(type.id);
                        setShowFilters(true);
                      }}
                    >
                      <Badge
                        variant="outline"
                        className="hover:bg-neon/20 hover:border-neon transition-colors"
                      >
                        {type.label}
                      </Badge>
                    </button>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </>
      }
    >
      {/* Sticky Header */}
      <div className="border-border bg-background relative z-10 border-b">
        <div className="p-4">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <h1 className="text-2xl font-bold">Events</h1>
            <Button className="bg-neon hover:bg-neon/90 text-black" asChild>
              <Link href="/events/create">
                <Plus className="mr-2 h-4 w-4" />
                Create Event
              </Link>
            </Button>
          </div>

          {/* Search Bar */}
          <div className="mb-4 flex flex-col gap-2 sm:flex-row">
            <div className="relative flex-1">
              <label htmlFor="events-search" className="sr-only">
                Search events
              </label>
              <Search className="text-muted-foreground absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2" />
              <input
                type="text"
                id="events-search"
                placeholder="Search events..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="bg-muted/50 border-border/50 focus:ring-neon/50 focus:border-neon/50 w-full rounded-lg border py-2 pr-10 pl-10 focus:ring-2 focus:outline-none"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => setSearchQuery("")}
                  aria-label="Clear event search"
                  className="text-muted-foreground hover:text-foreground absolute top-1/2 right-0 flex min-h-11 min-w-11 -translate-y-1/2 items-center justify-center"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
            <Button
              variant="outline"
              size="icon"
              onClick={() => setShowFilters(!showFilters)}
              aria-label="Event filters"
              aria-expanded={showFilters}
              aria-controls="event-filters"
              className={
                showFilters ? "bg-neon/20 border-neon" : "self-end sm:self-auto"
              }
            >
              <Filter className="h-4 w-4" />
              {activeFilterCount > 0 && (
                <span className="bg-neon absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-none text-xs text-black">
                  {activeFilterCount}
                </span>
              )}
            </Button>
          </div>

          {/* Expandable Filters */}
          {showFilters && (
            <div
              id="event-filters"
              className="bg-muted/30 border-border/50 mb-4 rounded-lg border p-4"
            >
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label
                    htmlFor="event-type-filter"
                    className="text-muted-foreground mb-1 block text-xs"
                  >
                    Event Type
                  </label>
                  <Select
                    value={eventTypeFilter}
                    onValueChange={setEventTypeFilter}
                  >
                    <SelectTrigger id="event-type-filter" className="w-full">
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
                  <label
                    htmlFor="event-format-filter"
                    className="text-muted-foreground mb-1 block text-xs"
                  >
                    Format
                  </label>
                  <Select value={formatFilter} onValueChange={setFormatFilter}>
                    <SelectTrigger id="event-format-filter" className="w-full">
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
                  <label
                    htmlFor="event-date-filter"
                    className="text-muted-foreground mb-1 block text-xs"
                  >
                    Date
                  </label>
                  <Select value={dateFilter} onValueChange={setDateFilter}>
                    <SelectTrigger id="event-date-filter" className="w-full">
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
                  <label
                    htmlFor="event-location-filter"
                    className="text-muted-foreground mb-1 block text-xs"
                  >
                    Location
                  </label>
                  <Select
                    value={locationFilter}
                    onValueChange={setLocationFilter}
                  >
                    <SelectTrigger
                      id="event-location-filter"
                      className="w-full"
                    >
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
            <div className="mb-4 flex flex-wrap gap-2">
              {searchQuery && (
                <Badge variant="secondary" className="gap-1 pr-0">
                  Search: {searchQuery}
                  <button
                    type="button"
                    aria-label="Remove search filter"
                    className="hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center"
                    onClick={() => setSearchQuery("")}
                  >
                    <X className="h-3 w-3" aria-hidden="true" />
                  </button>
                </Badge>
              )}
              {eventTypeFilter !== "all" && (
                <Badge variant="secondary" className="gap-1 pr-0">
                  Type:{" "}
                  {
                    eventTypeFilters.find((f) => f.id === eventTypeFilter)
                      ?.label
                  }
                  <button
                    type="button"
                    aria-label="Remove event type filter"
                    className="hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center"
                    onClick={() => setEventTypeFilter("all")}
                  >
                    <X className="h-3 w-3" aria-hidden="true" />
                  </button>
                </Badge>
              )}
              {formatFilter !== "all" && (
                <Badge variant="secondary" className="gap-1 pr-0">
                  Format:{" "}
                  {formatFilters.find((f) => f.id === formatFilter)?.label}
                  <button
                    type="button"
                    aria-label="Remove event format filter"
                    className="hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center"
                    onClick={() => setFormatFilter("all")}
                  >
                    <X className="h-3 w-3" aria-hidden="true" />
                  </button>
                </Badge>
              )}
              {dateFilter !== "all" && (
                <Badge variant="secondary" className="gap-1 pr-0">
                  Date: {dateFilters.find((f) => f.id === dateFilter)?.label}
                  <button
                    type="button"
                    aria-label="Remove event date filter"
                    className="hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center"
                    onClick={() => setDateFilter("all")}
                  >
                    <X className="h-3 w-3" aria-hidden="true" />
                  </button>
                </Badge>
              )}
              {locationFilter !== "all" && (
                <Badge variant="secondary" className="gap-1 pr-0">
                  Location:{" "}
                  {locationFilters.find((f) => f.id === locationFilter)?.label}
                  <button
                    type="button"
                    aria-label="Remove event location filter"
                    className="hover:bg-destructive/20 flex min-h-11 min-w-11 items-center justify-center"
                    onClick={() => setLocationFilter("all")}
                  >
                    <X className="h-3 w-3" aria-hidden="true" />
                  </button>
                </Badge>
              )}
              <Button
                variant="ghost"
                size="sm"
                onClick={clearFilters}
                className="text-muted-foreground hover:text-foreground text-xs"
              >
                Clear all
              </Button>
            </div>
          )}

          {/* Tabs */}
          <div
            className="flex gap-1 overflow-x-auto"
            role="tablist"
            aria-label="Event scope"
            onKeyDown={handleTabListKeyDown}
          >
            {eventTabs.map((tab) => (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                role="tab"
                id={`events-tab-${tab.id}`}
                aria-selected={activeTab === tab.id}
                aria-controls={`events-panel-${tab.id}`}
                tabIndex={activeTab === tab.id ? 0 : -1}
                className={`flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium whitespace-nowrap transition-colors ${
                  activeTab === tab.id
                    ? "bg-neon/20 text-neon"
                    : "text-muted-foreground hover:text-foreground hover:bg-muted/50"
                }`}
              >
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Events Feed */}
      <div
        className="divide-border/50 divide-y"
        role="tabpanel"
        id={`events-panel-${activeTab}`}
        aria-labelledby={`events-tab-${activeTab}`}
      >
        {eventsLoading ? (
          <div className="text-muted-foreground p-8 text-center">
            Loading events…
          </div>
        ) : eventsError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 p-8 text-center"
          >
            <p className="text-destructive font-medium">
              Events could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No event records or registration actions are shown.
            </p>
          </div>
        ) : activeTab === "my-events" && registeredEventsError ? (
          <div
            role="alert"
            className="border-destructive bg-destructive/10 p-8 text-center"
          >
            <p className="text-destructive font-medium">
              Your event registrations could not be loaded.
            </p>
            <p className="text-muted-foreground mt-1 text-sm">
              No registration state is inferred from missing data.
            </p>
          </div>
        ) : filteredEvents.length === 0 ? (
          <div className="text-muted-foreground p-8 text-center">
            <Calendar className="mx-auto mb-4 h-12 w-12 opacity-50" />
            <p>No events found matching your criteria.</p>
            {hasActiveFilters && (
              <Button
                variant="link"
                onClick={clearFilters}
                className="text-neon mt-2"
              >
                Clear filters
              </Button>
            )}
          </div>
        ) : (
          filteredEvents.map((event) => (
            <article key={event.id} className="p-4">
              <div className="flex gap-4">
                {/* Date Block */}
                <div className="w-16 flex-shrink-0 text-center">
                  <div className="bg-neon/20 border-neon/30 rounded-lg border p-2">
                    <div className="text-neon text-xs font-medium uppercase">
                      {new Date(event.date).toLocaleDateString("en-US", {
                        month: "short",
                      })}
                    </div>
                    <div className="text-foreground text-2xl font-bold">
                      {new Date(event.date).getDate()}
                    </div>
                  </div>
                </div>

                {/* Event Details */}
                <div className="min-w-0 flex-1">
                  <div className="mb-2 flex items-start justify-between gap-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge
                        variant="outline"
                        className={getEventTypeColor(event.type)}
                      >
                        {event.type}
                      </Badge>
                      <Badge
                        variant="outline"
                        className="text-muted-foreground gap-1"
                      >
                        {getFormatIcon(event.format)}
                        {event.format}
                      </Badge>
                    </div>
                    {event.isRegistered && (
                      <Badge className="bg-neon-green/20 text-neon-green border-neon-green/30">
                        Registered
                      </Badge>
                    )}
                  </div>

                  <h3 className="mb-1 truncate text-lg font-semibold">
                    {event.title}
                  </h3>
                  <p className="text-muted-foreground mb-3 line-clamp-2 text-sm">
                    {event.description}
                  </p>

                  <div className="text-muted-foreground mb-3 flex flex-wrap gap-x-4 gap-y-2 text-sm">
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
                      {event.attendees === null
                        ? "Attendance unavailable"
                        : `${event.attendees}${event.maxAttendees ? ` / ${event.maxAttendees}` : ""} attending`}
                    </div>
                  </div>

                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-2">
                      <Avatar className="h-6 w-6">
                        <AvatarImage src={event.organizer.avatar} alt="" />
                        <AvatarFallback className="text-xs">
                          {event.organizer.name.charAt(0)}
                        </AvatarFallback>
                      </Avatar>
                      <span className="text-muted-foreground text-sm">
                        Hosted by{" "}
                        <span className="text-foreground">
                          {event.organizer.name}
                        </span>
                      </span>
                    </div>
                    {!session?.user ? (
                      <Button size="sm" variant="outline" asChild>
                        <Link href="/auth/signin?callbackUrl=%2Fevents">
                          Sign in to register
                        </Link>
                      </Button>
                    ) : registeredEventsError ? (
                      <span
                        role="status"
                        className="text-muted-foreground text-sm"
                      >
                        Registration status unavailable
                      </span>
                    ) : !event.isRegistered ? (
                      <Button
                        size="sm"
                        className="bg-neon hover:bg-neon/90 text-black"
                        disabled={registerMutation.isPending}
                        onClick={(evClick) => {
                          evClick.stopPropagation();
                          registerMutation.mutate({ eventId: event.id });
                        }}
                      >
                        Register
                      </Button>
                    ) : (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={unregisterMutation.isPending}
                        onClick={(evClick) => {
                          evClick.stopPropagation();
                          unregisterMutation.mutate({ eventId: event.id });
                        }}
                      >
                        Unregister
                      </Button>
                    )}
                  </div>
                </div>
              </div>
            </article>
          ))
        )}
        {!eventsLoading && !eventsError && filteredEvents.length > 0 && (
          <div
            className="text-muted-foreground border-border border-t p-4 text-center text-sm"
            role="status"
          >
            End of current results · {filteredEvents.length} shown from{" "}
            {eventCards.length} loaded
            {typeof eventsResult?.totalCount === "number"
              ? ` · ${eventsResult.totalCount} total reported`
              : ""}
            .
          </div>
        )}
      </div>
    </FeedLayout>
  );
}

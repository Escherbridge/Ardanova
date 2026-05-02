import { type BaseApiClient, type ApiResponse, type PagedResult } from "../../base-client";

export interface Event {
  id: string;
  title: string;
  slug?: string | null;
  description?: string | null;
  type?: string | null;
  status?: string | null;
  /** ISO datetime (backend StartDate) */
  startDate: string;
  /** ISO datetime (backend EndDate) */
  endDate: string;
  timezone: string;
  location?: string | null;
  meetingUrl?: string | null;
  isOnline?: boolean;
  maxAttendees?: number | null;
  organizerId: string;
  projectId?: string | null;
  guildId?: string | null;
  createdAt: string;
  updatedAt: string;
  [key: string]: unknown;
}

export interface EventOrganizer {
  id: string;
  name?: string | null;
  email: string;
  image?: string | null;
}

export interface EventAttendee {
  id: string;
  userId: string;
  eventId: string;
  status?: string | null;
  user?: EventOrganizer;
  [key: string]: unknown;
}

export interface CreateEventDto {
  organizerId: string;
  title: string;
  description?: string;
  type?: string;
  visibility?: string;
  location?: string;
  locationUrl?: string;
  isOnline?: boolean;
  meetingUrl?: string;
  timezone: string;
  startDate: string;
  endDate: string;
  maxAttendees?: number;
  coverImage?: string;
  projectId?: string;
  guildId?: string;
  [key: string]: unknown;
}

export interface UpdateEventDto {
  title?: string;
  description?: string;
  type?: string;
  status?: string;
  visibility?: string;
  startDate?: string;
  endDate?: string;
  location?: string;
  locationUrl?: string;
  isOnline?: boolean;
  meetingUrl?: string;
  timezone?: string;
  maxAttendees?: number;
  coverImage?: string;
  [key: string]: unknown;
}

export interface RegisterEventDto {
  userId: string;
  [key: string]: unknown;
}

export interface SearchEventsParams {
  searchTerm?: string;
  type?: string;
  status?: string;
  upcoming?: boolean;
  page?: number;
  pageSize?: number;
}

export class EventsEndpoint {
  constructor(private client: BaseApiClient) {}

  getById(id: string): Promise<ApiResponse<Event>> {
    return this.client.get<Event>(`/api/events/${id}`);
  }

  getBySlug(slug: string): Promise<ApiResponse<Event>> {
    return this.client.get<Event>(`/api/events/slug/${encodeURIComponent(slug)}`);
  }

  getUpcoming(limit = 10): Promise<ApiResponse<Event[]>> {
    return this.client.get<Event[]>(`/api/events/upcoming?limit=${limit}`);
  }

  getByOrganizerId(organizerId: string): Promise<ApiResponse<Event[]>> {
    return this.client.get<Event[]>(`/api/events/organizer/${organizerId}`);
  }

  getRegisteredEvents(userId: string): Promise<ApiResponse<Event[]>> {
    return this.client.get<Event[]>(`/api/events/user/${userId}/registered`);
  }

  getAttendees(eventId: string): Promise<ApiResponse<EventAttendee[]>> {
    return this.client.get<EventAttendee[]>(`/api/events/${eventId}/attendees`);
  }

  search(params: SearchEventsParams = {}): Promise<ApiResponse<PagedResult<Event>>> {
    const sp = new URLSearchParams();
    if (params.searchTerm) sp.set("searchTerm", params.searchTerm);
    if (params.type) sp.set("type", params.type);
    if (params.status) sp.set("status", params.status);
    if (params.upcoming != null) sp.set("upcoming", String(params.upcoming));
    sp.set("page", String(params.page ?? 1));
    sp.set("pageSize", String(params.pageSize ?? 10));
    return this.client.get<PagedResult<Event>>(`/api/events/search?${sp.toString()}`);
  }

  create(data: CreateEventDto): Promise<ApiResponse<Event>> {
    return this.client.post<Event>("/api/events", data);
  }

  update(id: string, data: UpdateEventDto): Promise<ApiResponse<Event>> {
    return this.client.put<Event>(`/api/events/${id}`, data);
  }

  delete(id: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/events/${id}`);
  }

  register(eventId: string, dto: RegisterEventDto): Promise<ApiResponse<unknown>> {
    return this.client.post(`/api/events/${eventId}/register`, dto);
  }

  unregister(eventId: string, userId: string): Promise<ApiResponse<void>> {
    return this.client.delete(`/api/events/${eventId}/register?userId=${encodeURIComponent(userId)}`);
  }
}

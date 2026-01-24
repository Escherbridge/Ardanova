/**
 * WebSocket/Realtime Types for ArdaNova
 * Matches the event types from the backend EventBus
 */

// Base event interface matching backend IDomainEvent
export interface DomainEvent {
  eventId: string;
  eventType: string;
  occurredAt: string;
}

// User Events
export interface UserCreatedEvent extends DomainEvent {
  eventType: "user.created";
  userId: string;
  email: string;
  name?: string;
}

export interface UserUpdatedEvent extends DomainEvent {
  eventType: "user.updated";
  userId: string;
  name?: string;
  email?: string;
}

export interface UserVerifiedEvent extends DomainEvent {
  eventType: "user.verified";
  userId: string;
}

export interface UserDeletedEvent extends DomainEvent {
  eventType: "user.deleted";
  userId: string;
}

// Project Events
export interface ProjectCreatedEvent extends DomainEvent {
  eventType: "project.created";
  projectId: string;
  ownerId: string;
  title: string;
  slug?: string;
}

export interface ProjectUpdatedEvent extends DomainEvent {
  eventType: "project.updated";
  projectId: string;
  title: string;
}

export interface ProjectStatusChangedEvent extends DomainEvent {
  eventType: "project.status_changed";
  projectId: string;
  oldStatus: string;
  newStatus: string;
}

export interface ProjectDeletedEvent extends DomainEvent {
  eventType: "project.deleted";
  projectId: string;
}

export interface ProjectTaskCompletedEvent extends DomainEvent {
  eventType: "project.task_completed";
  projectId: string;
  taskId: string;
  assigneeId?: string;
  taskTitle: string;
}

export interface ProjectMemberAddedEvent extends DomainEvent {
  eventType: "project.member_added";
  projectId: string;
  userId: string;
  role: string;
}

export interface ProjectMemberRemovedEvent extends DomainEvent {
  eventType: "project.member_removed";
  projectId: string;
  userId: string;
}

// Notification Events
export interface NotificationCreatedEvent extends DomainEvent {
  eventType: "notification.created";
  notificationId: string;
  userId: string;
  type: string;
  title: string;
  message: string;
  actionUrl?: string;
}

export interface NotificationReadEvent extends DomainEvent {
  eventType: "notification.read";
  notificationId: string;
  userId: string;
}

export interface NotificationsMarkedAllReadEvent extends DomainEvent {
  eventType: "notification.all_read";
  userId: string;
}

// Activity Events
export interface ActivityLoggedEvent extends DomainEvent {
  eventType: "activity.logged";
  activityId: string;
  userId?: string;
  activityType: string;
  description: string;
  entityId?: string;
  entityType?: string;
}

// Union type of all events
export type ArdaNovaEvent =
  | UserCreatedEvent
  | UserUpdatedEvent
  | UserVerifiedEvent
  | UserDeletedEvent
  | ProjectCreatedEvent
  | ProjectUpdatedEvent
  | ProjectStatusChangedEvent
  | ProjectDeletedEvent
  | ProjectTaskCompletedEvent
  | ProjectMemberAddedEvent
  | ProjectMemberRemovedEvent
  | NotificationCreatedEvent
  | NotificationReadEvent
  | NotificationsMarkedAllReadEvent
  | ActivityLoggedEvent;

// Event type string literals
export type ArdaNovaEventType = ArdaNovaEvent["eventType"];

// Connection states
export type ConnectionState =
  | "disconnected"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "error";

// WebSocket options
export interface WebSocketOptions {
  autoConnect?: boolean;
  reconnectInterval?: number;
  maxReconnectAttempts?: number;
  onConnect?: () => void;
  onDisconnect?: () => void;
  onError?: (error: Error) => void;
}

// Subscription action types
export type SubscriptionAction =
  | { action: "subscribeToProject"; payload: { projectId: string } }
  | { action: "unsubscribeFromProject"; payload: { projectId: string } }
  | { action: "subscribeToAgency"; payload: { agencyId: string } }
  | { action: "unsubscribeFromAgency"; payload: { agencyId: string } }
  | { action: "subscribeToUser"; payload: { userId: string } }
  | { action: "unsubscribeFromUser"; payload: { userId: string } }
  | { action: "subscribeToAll"; payload: Record<string, never> }
  | { action: "unsubscribeFromAll"; payload: Record<string, never> };

// Event callback type
export type EventCallback<T extends ArdaNovaEvent = ArdaNovaEvent> = (event: T) => void;

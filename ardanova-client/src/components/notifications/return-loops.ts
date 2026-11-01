import type { Notification } from "~/lib/api/ardanova/endpoints/notifications";
import { normalizeInternalCallbackUrl } from "~/lib/auth-navigation";

/** The only two come-back events the bell shows (plan §4); see components/notifications/AGENTS.md. */
export const RETURN_LOOP_TYPES = {
  COMMENT_REPLY: "reply",
  TASK_ASSIGNED: "picked-up",
} as const;

export type ReturnLoopKind =
  (typeof RETURN_LOOP_TYPES)[keyof typeof RETURN_LOOP_TYPES];

export interface ReturnLoopNotification {
  id: string;
  kind: ReturnLoopKind;
  title: string;
  message: string;
  /** An in-app path; never an absolute or protocol-relative URL. */
  href: string;
  createdAt: string;
}

/** Where a notification with no usable link lands: Home, where the post itself is. */
export const RETURN_LOOP_FALLBACK_HREF = "/dashboard";

function kindOf(type: string): ReturnLoopKind | null {
  return Object.hasOwn(RETURN_LOOP_TYPES, type)
    ? RETURN_LOOP_TYPES[type as keyof typeof RETURN_LOOP_TYPES]
    : null;
}

/** Unread notifications narrowed to replies and picked-up asks, in the API's order (newest first). */
export function returnLoopsOf(
  notifications: readonly Notification[],
): ReturnLoopNotification[] {
  return notifications.flatMap((notification) => {
    const kind = kindOf(notification.type);
    if (!kind || notification.isRead) return [];
    return [
      {
        id: notification.id,
        kind,
        title: notification.title,
        message: notification.message,
        href: normalizeInternalCallbackUrl(
          notification.actionUrl ?? undefined,
          RETURN_LOOP_FALLBACK_HREF,
        ),
        createdAt: notification.createdAt,
      },
    ];
  });
}

/** True for the two types the bell owns; "Mark all read" clears only these from view. */
export function isReturnLoop(notification: Pick<Notification, "type">) {
  return kindOf(notification.type) !== null;
}

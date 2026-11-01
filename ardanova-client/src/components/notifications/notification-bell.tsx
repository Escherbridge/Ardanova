"use client";

import Link from "next/link";
import { useEffect, useId, useRef, useState } from "react";
import { Bell, HandHelping, Reply } from "lucide-react";
import { toast } from "sonner";

import { formatNoteAge } from "~/components/feed/field-note-audience";
import { describeMutationError } from "~/components/ui/query-error-copy";
import { cn } from "~/lib/utils";
import { api } from "~/trpc/react";

import { returnLoopsOf, type ReturnLoopNotification } from "./return-loops";

/** In-app only, no push: a quiet poll while the tab is open. See ./AGENTS.md §Data flow. */
const POLL_INTERVAL_MS = 60_000;
const BADGE_CAP = 9;

/** The topbar bell: replies to your words and your asks picked up, each a deep link that marks it read. */
export function NotificationBell() {
  const utils = api.useUtils();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelId = useId();
  const headingId = useId();

  const unread = api.notification.getUnread.useQuery(undefined, {
    refetchInterval: POLL_INTERVAL_MS,
  });
  const items = returnLoopsOf(unread.data ?? []);
  const count = items.length;

  // Optimistic: the item leaves the list at once; a refusal puts it back. The server read wins after.
  const markAsRead = api.notification.markAsRead.useMutation({
    onMutate: async ({ id }) => {
      await utils.notification.getUnread.cancel();
      const previous = utils.notification.getUnread.getData();
      utils.notification.getUnread.setData(undefined, (current) =>
        current?.filter((notification) => notification.id !== id),
      );
      return { previous };
    },
    onError: (error, _input, context) => {
      utils.notification.getUnread.setData(undefined, context?.previous);
      toast.error(describeMutationError(error, "mark that read"));
    },
    onSettled: () => void utils.notification.getUnread.invalidate(),
  });

  // Only the rows the bell shows: escrow, KYC or governance notices no screen lists stay unread.
  const markShownAsRead = api.notification.markManyAsRead.useMutation({
    onMutate: async ({ ids }) => {
      await utils.notification.getUnread.cancel();
      const previous = utils.notification.getUnread.getData();
      const marked = new Set(ids);
      utils.notification.getUnread.setData(undefined, (current) =>
        current?.filter((notification) => !marked.has(notification.id)),
      );
      return { previous };
    },
    onError: (error, _input, context) => {
      utils.notification.getUnread.setData(undefined, context?.previous);
      toast.error(describeMutationError(error, "mark these read"));
    },
    onSettled: () => void utils.notification.getUnread.invalidate(),
  });

  // Disclosure pattern: Escape returns focus to the bell; a press outside closes without moving it.
  useEffect(() => {
    if (!open) return;
    const closeOnOutsidePress = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key !== "Escape") return;
      setOpen(false);
      triggerRef.current?.focus();
    };
    document.addEventListener("pointerdown", closeOnOutsidePress);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOnOutsidePress);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [open]);

  return (
    <div ref={rootRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        aria-label={
          count > 0 ? `Notifications, ${count} unread` : "Notifications"
        }
        onClick={() => setOpen((current) => !current)}
        className={cn(
          "border-input bg-background hover:bg-secondary focus-visible:outline-ring relative inline-flex size-10 items-center justify-center border-2 transition-colors focus-visible:outline-2 focus-visible:outline-offset-2",
          open && "bg-foreground text-background hover:bg-foreground/90",
        )}
      >
        <Bell aria-hidden className="size-[1.125rem]" />
        {count > 0 ? (
          <span
            aria-hidden
            className="bg-primary text-primary-foreground border-background motion-safe:animate-in motion-safe:zoom-in-50 absolute -top-2 -right-2 flex h-5 min-w-5 items-center justify-center border-2 px-1 font-mono text-[10px] leading-none font-bold tabular-nums"
          >
            {count > BADGE_CAP ? `${BADGE_CAP}+` : count}
          </span>
        ) : null}
      </button>

      {open ? (
        <div
          id={panelId}
          role="region"
          aria-labelledby={headingId}
          className="bg-popover text-popover-foreground motion-safe:animate-in motion-safe:fade-in-0 motion-safe:slide-in-from-top-1 absolute top-[calc(100%+0.5rem)] right-0 z-50 w-[min(22rem,calc(100vw-1.5rem))] border-2 shadow-[6px_6px_0_0_var(--foreground)]"
        >
          <div className="flex min-h-11 items-center justify-between gap-3 border-b-2 px-3">
            <h2
              id={headingId}
              className="font-mono text-[11px] font-semibold tracking-[0.08em] uppercase"
            >
              Notifications
            </h2>
            {count > 0 ? (
              <button
                type="button"
                disabled={markShownAsRead.isPending}
                onClick={() =>
                  markShownAsRead.mutate({
                    // The procedure's bound; a longer list clears in the next press.
                    ids: items.slice(0, 100).map((item) => item.id),
                  })
                }
                className="hover:text-foreground text-muted-foreground min-h-8 px-1 text-xs font-semibold underline-offset-4 hover:underline disabled:opacity-60"
              >
                Mark all read
              </button>
            ) : null}
          </div>

          {unread.isError && !unread.data ? (
            <div className="flex items-center justify-between gap-3 px-3 py-4 text-sm">
              <p role="alert">Notifications didn&apos;t load.</p>
              <button
                type="button"
                onClick={() => void unread.refetch()}
                className="min-h-8 px-1 text-xs font-semibold underline underline-offset-4"
              >
                Try again
              </button>
            </div>
          ) : unread.isPending ? (
            <p
              role="status"
              className="text-muted-foreground px-3 py-4 text-sm"
            >
              Loading
            </p>
          ) : count === 0 ? (
            <p className="text-muted-foreground px-3 py-4 text-sm">
              Nothing new.
            </p>
          ) : (
            <ul className="max-h-[min(26rem,70vh)] divide-y overflow-y-auto">
              {items.map((item) => (
                <li key={item.id}>
                  <ReturnLoopLink
                    item={item}
                    onOpen={() => {
                      setOpen(false);
                      markAsRead.mutate({ id: item.id });
                    }}
                  />
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}

function ReturnLoopLink({
  item,
  onOpen,
}: {
  item: ReturnLoopNotification;
  onOpen: () => void;
}) {
  const Icon = item.kind === "reply" ? Reply : HandHelping;
  return (
    <Link
      href={item.href}
      onClick={onOpen}
      className="hover:bg-secondary focus-visible:bg-secondary focus-visible:outline-ring grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 px-3 py-3 transition-colors focus-visible:outline-2 focus-visible:-outline-offset-2"
    >
      <span
        aria-hidden
        className={cn(
          "mt-0.5 flex size-7 items-center justify-center border-2",
          item.kind === "reply"
            ? "border-foreground"
            : "border-primary bg-primary text-primary-foreground",
        )}
      >
        <Icon className="size-3.5" />
      </span>
      <span className="min-w-0">
        <span className="flex items-baseline justify-between gap-3">
          <span className="truncate text-sm font-semibold">{item.title}</span>
          <time
            dateTime={item.createdAt}
            suppressHydrationWarning
            className="text-muted-foreground shrink-0 font-mono text-[11px]"
          >
            {formatNoteAge(item.createdAt)}
          </time>
        </span>
        <span className="text-muted-foreground mt-0.5 line-clamp-2 block text-sm leading-snug">
          {item.message}
        </span>
      </span>
    </Link>
  );
}

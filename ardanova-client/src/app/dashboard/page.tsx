"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { matchMutation, useQueryClient } from "@tanstack/react-query";
import { getMutationKey } from "@trpc/react-query";
import { ArrowUpRight } from "lucide-react";

import {
  FeedStream,
  FieldNoteComposer,
  feedCardFromPost,
  useAttachableSubjects,
  type ComposerAuthor,
  type PublishedPost,
} from "~/components/feed";
import { SetupPrompt } from "~/components/onboarding/setup-prompt";
import { feedRoutes } from "~/lib/feed/routes";
import type { FeedKitCard } from "~/lib/feed/types";
import { useSubjectFollow } from "~/lib/feed/use-subject-follow";
import { api, type RouterOutputs } from "~/trpc/react";

type MyTask = RouterOutputs["task"]["getMyTasks"]["items"][number];

const RAIL_TASK_LIMIT = 3;

/** Home is the feed: composer, ranked stream, cold-start row; "Your work" and the record notes in the rail. See ./AGENTS.md. */
export default function HomePage() {
  const { data: session } = useSession();
  const viewerId = session?.user?.id;
  const viewerName = session?.user?.name;
  const viewerImage = session?.user?.image;
  const viewer = useMemo<ComposerAuthor | null>(
    () =>
      viewerId ? { id: viewerId, name: viewerName, image: viewerImage } : null,
    [viewerId, viewerName, viewerImage],
  );

  const published = usePublishedCards(viewer);
  const onToggleFollow = useSubjectFollow();

  return (
    <div className="grid gap-x-12 gap-y-10 py-6 sm:py-10 lg:grid-cols-[minmax(0,40rem)_16rem] lg:justify-center">
      <h1 className="sr-only">Home</h1>

      <div className="min-w-0 space-y-6">
        <SetupPrompt />
        {viewer ? (
          <section aria-label="Post an update">
            <FieldNoteComposer author={viewer} />
          </section>
        ) : null}
        <FeedStream
          scope="home"
          viewerId={viewer?.id ?? null}
          onToggleFollow={onToggleFollow}
          leadingCards={published}
        />
      </div>

      <aside
        aria-label="Your work and records"
        className="space-y-8 lg:sticky lg:top-24 lg:self-start"
      >
        <YourWork enabled={viewer !== null} />
        <RecordNotes />
      </aside>
    </div>
  );
}

/**
 * Every post the person publishes (or, as an ask, edits) while Home is open, newest first, as leading cards: they
 * sit above the stream because they are newer than its pinned asOf, and an edit replaces the stream's copy. Observer
 * on the mutation cache, so it follows any composer that writes through `post.create`/`post.update` without depending
 * on that composer's callback shape. The card's subject comes from the composer's own attach lists (same cache).
 */
function usePublishedCards(viewer: ComposerAuthor | null): FeedKitCard[] {
  const queryClient = useQueryClient();
  const [posts, setPosts] = useState<PublishedPost[]>([]);
  const subjects = useAttachableSubjects(viewer !== null);

  useEffect(() => {
    const writes = [api.post.create, api.post.update].map((procedure) => ({
      mutationKey: getMutationKey(procedure),
    }));
    return queryClient.getMutationCache().subscribe((event) => {
      if (event.type !== "updated" || event.action.type !== "success") return;
      const mutation = event.mutation;
      if (!writes.some((write) => matchMutation(write, mutation))) return;
      const post = event.action.data as PublishedPost | undefined;
      if (!post?.id) return;
      setPosts((current) => [
        post,
        ...current.filter((earlier) => earlier.id !== post.id),
      ]);
    });
  }, [queryClient]);

  return useMemo(
    () =>
      viewer
        ? posts.map((post) =>
            feedCardFromPost(post, {
              author: viewer,
              project: subjects.projects.find(
                (project) => project.id === post.projectId,
              )?.card,
              guild: subjects.guilds.find((guild) => guild.id === post.guildId)
                ?.card,
            }),
          )
        : [],
    [posts, viewer, subjects.projects, subjects.guilds],
  );
}

/** Rail (lg+): up to three open tasks assigned to the person. */
function YourWork({ enabled }: { enabled: boolean }) {
  const myTasksQuery = api.task.getMyTasks.useQuery(
    { limit: 100 },
    { enabled },
  );
  const openTasks: MyTask[] = (myTasksQuery.data?.items ?? [])
    .filter((task) => task.status !== "COMPLETED")
    .slice(0, RAIL_TASK_LIMIT);

  return (
    <section aria-labelledby="your-work-heading" className="hidden lg:block">
      <h2
        id="your-work-heading"
        className="border-foreground text-muted-foreground border-b-2 pb-2 font-mono text-[11px] font-semibold tracking-[0.08em] uppercase"
      >
        Your work
      </h2>
      {myTasksQuery.isPending && enabled ? (
        <p role="status" className="text-muted-foreground py-3 text-sm">
          Loading
        </p>
      ) : myTasksQuery.isError ? (
        <p role="alert" className="py-3 text-sm">
          Tasks didn&apos;t load.
        </p>
      ) : openTasks.length === 0 ? (
        <div className="space-y-2 py-3">
          <p className="text-muted-foreground text-sm">
            Nothing assigned to you.
          </p>
          <Link
            href="/opportunities"
            className="inline-flex min-h-8 items-center gap-1 text-sm font-semibold underline-offset-4 hover:underline"
          >
            Find open work
            <ArrowUpRight aria-hidden className="size-3.5" />
          </Link>
        </div>
      ) : (
        <ol>
          {openTasks.map((task) => (
            <li key={task.id} className="border-border border-b">
              <Link
                href={feedRoutes.task(task.id)}
                className="hover:bg-secondary focus-visible:bg-secondary block py-3 transition-colors"
              >
                <span className="text-muted-foreground block font-mono text-[11px] tracking-[0.06em] uppercase">
                  {task.status.replaceAll("_", " ")}
                </span>
                <span className="mt-0.5 line-clamp-2 block text-sm leading-snug font-semibold">
                  {task.title}
                </span>
              </Link>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

/** Rights and consent notes for the proposals, events and funding the stream carries; kept verbatim. */
function RecordNotes() {
  return (
    <div className="border-border text-muted-foreground space-y-3 border-t-[3px] border-double pt-3 text-xs leading-relaxed">
      <p>
        Proposals remain proposals until the recorded process resolves them.
        Events are shown as scheduled, not as attendance or consent.
      </p>
      <p>
        Participation is not proof of ownership. Governance credentials, project
        utility, and economic rights remain separate, reviewable records.
      </p>
    </div>
  );
}

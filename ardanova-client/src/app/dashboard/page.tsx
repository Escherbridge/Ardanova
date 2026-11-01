"use client";

import { useEffect, useId, useMemo, useState } from "react";
import Link from "next/link";
import { useSession } from "next-auth/react";
import { matchMutation, useQueryClient } from "@tanstack/react-query";
import { getMutationKey } from "@trpc/react-query";
import { ArrowUpRight } from "lucide-react";

import { useSubjectFollow } from "~/app/tag/[tag]/use-subject-follow";
import { FeedStream, FieldNoteComposer } from "~/components/feed";
import { SetupPrompt } from "~/components/onboarding/setup-prompt";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { feedRoutes } from "~/lib/feed/routes";
import type { FeedKitCard } from "~/lib/feed/types";
import { api, type RouterOutputs } from "~/trpc/react";

type MyProject = RouterOutputs["project"]["getMyProjects"]["items"][number];
type MyTask = RouterOutputs["task"]["getMyTasks"]["items"][number];
type PublishedPost = RouterOutputs["post"]["create"];

interface HomeViewer {
  id: string;
  name?: string | null;
  image?: string | null;
}

const RAIL_TASK_LIMIT = 3;
const NO_PROJECTS: readonly MyProject[] = [];

/** Home is the feed: composer, ranked stream, cold-start row; "Your work" and the record notes in the rail. See ./AGENTS.md. */
export default function HomePage() {
  const { data: session } = useSession();
  const viewerId = session?.user?.id;
  const viewerName = session?.user?.name;
  const viewerImage = session?.user?.image;
  const viewer = useMemo<HomeViewer | null>(
    () =>
      viewerId ? { id: viewerId, name: viewerName, image: viewerImage } : null,
    [viewerId, viewerName, viewerImage],
  );

  const myProjectsQuery = api.project.getMyProjects.useQuery(
    { limit: 100, page: 1 },
    { enabled: viewer !== null },
  );
  const projects = myProjectsQuery.data?.items ?? NO_PROJECTS;
  const published = usePublishedCards(viewer, projects);
  const onToggleFollow = useSubjectFollow();

  return (
    <div className="grid gap-x-12 gap-y-10 py-6 sm:py-10 lg:grid-cols-[minmax(0,40rem)_16rem] lg:justify-center">
      <h1 className="sr-only">Home</h1>

      <div className="min-w-0 space-y-6">
        <SetupPrompt />
        {projects.length > 0 ? <HomeComposer projects={projects} /> : null}
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

/** The field-note composer over one of the person's projects (its current props need a project). */
function HomeComposer({ projects }: { projects: readonly MyProject[] }) {
  const [chosenId, setChosenId] = useState<string | null>(null);
  const pickerId = useId();
  const project =
    projects.find((candidate) => candidate.id === chosenId) ?? projects[0];
  if (!project) return null;

  return (
    <section aria-label="Post an update">
      <div className="text-muted-foreground mb-2 flex min-h-9 items-center gap-2 text-xs">
        {projects.length > 1 ? (
          <>
            <label htmlFor={pickerId}>Post to</label>
            <Select value={project.id} onValueChange={setChosenId}>
              <SelectTrigger
                id={pickerId}
                size="sm"
                className="text-foreground max-w-64"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {projects.map((candidate) => (
                  <SelectItem key={candidate.id} value={candidate.id}>
                    {candidate.title}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </>
        ) : (
          <p>
            Post to{" "}
            <span className="text-foreground font-semibold">
              {project.title}
            </span>
          </p>
        )}
      </div>
      {/* Not keyed by project: switching the target keeps the draft. */}
      <FieldNoteComposer projectId={project.id} />
    </section>
  );
}

/**
 * Every post the person publishes while Home is open, newest first, as leading cards: they sit above the
 * stream because they are newer than its pinned asOf. Observer on the mutation cache, so it follows any
 * composer that writes through `post.create` without depending on that composer's callback shape.
 */
function usePublishedCards(
  viewer: HomeViewer | null,
  projects: readonly MyProject[],
): FeedKitCard[] {
  const queryClient = useQueryClient();
  const [posts, setPosts] = useState<PublishedPost[]>([]);

  useEffect(() => {
    const createPost = { mutationKey: getMutationKey(api.post.create) };
    return queryClient.getMutationCache().subscribe((event) => {
      if (event.type !== "updated" || event.action.type !== "success") return;
      if (!matchMutation(createPost, event.mutation)) return;
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
      viewer ? posts.map((post) => publishedCard(post, viewer, projects)) : [],
    [posts, viewer, projects],
  );
}

/** A just-published post as the feed card the API will later serve for it (author's own view). */
function publishedCard(
  post: PublishedPost,
  author: HomeViewer,
  projects: readonly MyProject[],
): FeedKitCard {
  const project = projects.find((candidate) => candidate.id === post.projectId);
  const hashtags = Array.isArray(post.hashtags)
    ? post.hashtags.filter((tag): tag is string => typeof tag === "string")
    : [];
  return {
    id: post.id,
    postType: post.type,
    sourceType: null,
    occurredAt: post.createdAt,
    author: {
      id: author.id,
      name: author.name ?? null,
      image: author.image ?? null,
    },
    title: post.title ?? null,
    body: post.content,
    bodyIsUserWritten: true,
    hashtags,
    project: project
      ? {
          id: project.id,
          title: project.title,
          slug: project.slug,
          summary: project.description ?? null,
          sectors: project.categories ?? [],
        }
      : null,
    guild: null,
    opportunity: null,
    event: null,
    funding: null,
    thread: null,
    counters: { likes: 0, replies: 0, shares: 0 },
    viewer: { isMember: true, isFollowing: false, canClaim: false },
    isDiscovery: false,
    reasons: [],
    task: post.taskId ? { id: post.taskId, state: "OPEN" } : null,
  };
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

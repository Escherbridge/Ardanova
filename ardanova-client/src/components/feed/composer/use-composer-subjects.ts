"use client";

import { useMemo } from "react";

import { api } from "~/trpc/react";

import {
  cardGuildOf,
  cardProjectOf,
  type CardGuild,
  type CardProject,
} from "./published-card";

export interface AttachableProject {
  id: string;
  name: string;
  card: CardProject;
}

export interface AttachableGuild {
  id: string;
  name: string;
  /** Null when the guild has no slug, so a card cannot link it. */
  card: CardGuild | null;
}

const listOptions = { staleTime: 60_000, refetchOnWindowFocus: false } as const;

/** Projects and guilds the author belongs to (created/owns or a member of). Not read when the page fixed the attachment. */
export function useAttachableSubjects(enabled: boolean) {
  const projects = api.project.getMyMemberships.useQuery(undefined, {
    ...listOptions,
    enabled,
  });
  const guilds = api.guild.getMyMemberships.useQuery(undefined, {
    ...listOptions,
    enabled,
  });
  const projectRows = projects.data;
  const guildRows = guilds.data;
  // Stable between reads, so a caller can derive memoised cards from these lists.
  return useMemo(
    () => ({
      projects: (projectRows ?? []).map(
        (project): AttachableProject => ({
          id: project.id,
          name: project.title,
          card: cardProjectOf(project),
        }),
      ),
      guilds: (guildRows ?? []).map(
        (guild): AttachableGuild => ({
          id: guild.id,
          name: guild.name,
          card: cardGuildOf(guild),
        }),
      ),
    }),
    [projectRows, guildRows],
  );
}

/** A task an ask may name: nobody holds it and it is not done (the API refuses a COMPLETED task). */
function isAskable(task: { assignedToId?: string | null; status: string }) {
  return !task.assignedToId && task.status !== "COMPLETED";
}

/** The project's tasks for the help picker; read only in help mode, once a project is known. */
export function useAskableTasks(projectId: string | null, enabled: boolean) {
  const tasks = api.task.getAll.useQuery(
    { projectId: projectId ?? "", limit: 100 },
    { ...listOptions, enabled: enabled && Boolean(projectId) },
  );
  const all = tasks.data?.items ?? [];
  return {
    all,
    askable: all.filter(isAskable),
    isLoading: tasks.isLoading && tasks.fetchStatus !== "idle",
    isError: tasks.isError,
  };
}

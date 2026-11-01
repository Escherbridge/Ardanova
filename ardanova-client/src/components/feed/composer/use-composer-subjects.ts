"use client";

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

/** Projects and guilds the author may attach a note to. Not read when the mounting page fixed the attachment. */
export function useAttachableSubjects(enabled: boolean) {
  const projects = api.project.getMyProjects.useQuery(
    {},
    { ...listOptions, enabled },
  );
  const guilds = api.guild.getMyGuilds.useQuery(undefined, {
    ...listOptions,
    enabled,
  });
  return {
    projects: (projects.data?.items ?? []).map(
      (project): AttachableProject => ({
        id: project.id,
        name: project.title,
        card: cardProjectOf(project),
      }),
    ),
    guilds: (guilds.data ?? []).map(
      (guild): AttachableGuild => ({
        id: guild.id,
        name: guild.name,
        card: cardGuildOf(guild),
      }),
    ),
  };
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

"use client";

import { useCallback } from "react";

import type { FeedSubject } from "~/lib/feed/types";
import { api } from "~/trpc/react";

/**
 * FeedStream's `onToggleFollow` for Home and tag pages: a project subject goes through the project follow
 * procedures, a guild subject through guild follow. The promise rejects on failure so FeedStream reverts.
 * Lives here only because of lane ownership; see ./AGENTS.md §Shared follow hook.
 */
export function useSubjectFollow() {
  const { mutateAsync: followProject } =
    api.project.followProject.useMutation();
  const { mutateAsync: unfollowProject } =
    api.project.unfollowProject.useMutation();
  const { mutateAsync: followGuild } = api.guild.followGuild.useMutation();
  const { mutateAsync: unfollowGuild } = api.guild.unfollowGuild.useMutation();

  return useCallback(
    (subject: FeedSubject, follow: boolean): Promise<unknown> => {
      if (subject.type === "project") {
        const input = { projectId: subject.id };
        return follow ? followProject(input) : unfollowProject(input);
      }
      const input = { guildId: subject.id };
      return follow ? followGuild(input) : unfollowGuild(input);
    },
    [followProject, unfollowProject, followGuild, unfollowGuild],
  );
}

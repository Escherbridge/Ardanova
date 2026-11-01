/** Every href the feed kit links to, in one place so a route move is one edit. */

const segment = (value: string) => encodeURIComponent(value);

export const feedRoutes = {
  project: (slug: string) => `/projects/${segment(slug)}`,
  projectBoard: (slug: string) => `/projects/${segment(slug)}/board`,
  projectInvest: (slug: string) => `/projects/${segment(slug)}/invest`,
  projectComments: (slug: string) => `/projects/${segment(slug)}?tab=comments`,
  guild: (slug: string) => `/guilds/${segment(slug)}`,
  /** The opportunity page resolves by slug; an id-only link 404s until the card carries one. */
  opportunity: (opportunity: { id: string; slug?: string | null }) =>
    `/opportunities/${segment(opportunity.slug || opportunity.id)}`,
  task: (taskId: string) => `/tasks/${segment(taskId)}`,
  person: (userId: string) => `/dashboard/profile/${segment(userId)}`,
  tag: (tag: string) => `/tag/${segment(tag)}`,
  /** Opens a draft; the conversation is created on first send, never on load. */
  chatWith: (userId: string) => `/chats?with=${segment(userId)}`,
  events: "/events",
  startProject: "/projects/create",
} as const;

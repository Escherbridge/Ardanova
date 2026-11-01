import { canonicalHashtag } from "~/components/hashtags/hashtag-tokens";

/** The project page's tabs, in order. Feed is first and the default; see ./AGENTS.md §Project page. */
export const PROJECT_TABS = [
  { id: "feed", label: "Feed" },
  { id: "overview", label: "Overview" },
  { id: "team", label: "Team" },
  { id: "funding", label: "Funding" },
  { id: "equity", label: "Your stake" },
  { id: "proposals", label: "Proposals" },
  { id: "opportunities", label: "Work" },
  { id: "tasks", label: "Tasks" },
  { id: "roadmap", label: "Roadmap" },
  { id: "sprints", label: "Sprints" },
  { id: "comments", label: "Comments" },
] as const;

export type ProjectTab = (typeof PROJECT_TABS)[number]["id"];

export const DEFAULT_PROJECT_TAB: ProjectTab = "feed";

/** Tabs that were folded into another; an old `?tab=` link still lands somewhere real. */
const FOLDED_TABS: ReadonlyMap<string, ProjectTab> = new Map<
  string,
  ProjectTab
>([["updates", "feed"]]);

function isProjectTab(value: string): value is ProjectTab {
  return PROJECT_TABS.some((tab) => tab.id === value);
}

/** The tab a URL asks for: `?tab=`, a folded tab's successor, or Proposals for `?proposalId=`; null means the default. */
export function projectTabFrom(search: {
  get(name: string): string | null;
}): ProjectTab | null {
  const named = search.get("tab");
  if (named && isProjectTab(named)) return named;
  const folded = named ? FOLDED_TABS.get(named) : undefined;
  if (folded) return folded;
  return search.get("proposalId") ? "proposals" : null;
}

/** The header's tags: linkable `#tags` first-seen and once each; legacy free text that is not a tag stays plain. */
export function projectTagsOf(project: {
  hashtags?: readonly string[];
  tags?: string | null;
}): { linked: string[]; plain: string[] } {
  const linked = new Set<string>();
  const plain = new Set<string>();
  const freeTags = (project.tags ?? "").split(",");
  for (const raw of [...(project.hashtags ?? []), ...freeTags]) {
    const text = raw.trim();
    if (!text) continue;
    const tag = canonicalHashtag(text);
    if (tag) linked.add(tag);
    else plain.add(text);
  }
  return { linked: [...linked], plain: [...plain] };
}

/** The edit form's hashtag field: `editable` is what a `hashtags` write replaces, `kept` the legacy text it carries over. See ./AGENTS.md §Edit form. */
export function projectHashtagFieldOf(project: {
  hashtags?: readonly string[];
  tags?: string | null;
  categories?: readonly string[];
}): { editable: string[]; kept: string[] } {
  const { linked, plain } = projectTagsOf(project);
  const kept = new Set(plain);
  for (const category of project.categories ?? []) {
    const text = category.trim();
    // A category a tag can spell is already among `hashtags` as its "#slug".
    if (text && !canonicalHashtag(text)) kept.add(text);
  }
  return { editable: linked, kept: [...kept] };
}

/** The one-line ask: `summary`, else the older `description`, unless it only repeats the solution. */
export function projectLedeOf(project: {
  summary?: string | null;
  description?: string | null;
  solution?: string | null;
}): string | undefined {
  const lede = project.summary?.trim() || project.description?.trim();
  return lede && lede !== project.solution?.trim() ? lede : undefined;
}

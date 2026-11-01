import { feedRoutes } from "./routes";
import type { FeedKitCard, FeedSubject } from "./types";

/** The per-subject state key FeedStream's follow state is held under, e.g. "project:abc". */
export function feedSubjectKey(type: FeedSubject["type"], id: string): string {
  return `${type}:${id}`;
}

/** The card's subject: its project, else its guild; null for a card about neither. */
export function feedSubjectOf(card: FeedKitCard): FeedSubject | null {
  if (card.project) {
    return {
      type: "project",
      id: card.project.id,
      name: card.project.title,
      href: feedRoutes.project(card.project.slug),
      key: feedSubjectKey("project", card.project.id),
    };
  }
  if (card.guild) {
    return {
      type: "guild",
      id: card.guild.id,
      name: card.guild.name,
      href: feedRoutes.guild(card.guild.slug),
      key: feedSubjectKey("guild", card.guild.id),
    };
  }
  return null;
}

/**
 * Leading (locally inserted) cards first, then every page in order, each id once. A leading card the stream also
 * carries keeps its place but shows the stream's copy, the server's word on state and counters. The API never repeats
 * a card in one scroll; the id check is the defensive second check. See components/feed/AGENTS.md §FeedStream.
 */
export function mergeFeedPages<TCard extends { id: string }>(
  leading: readonly TCard[],
  pages: readonly { items: readonly TCard[] }[] | undefined,
): TCard[] {
  const streamCopies = new Map<string, TCard>();
  pages?.forEach((page) =>
    page.items.forEach((card) => {
      if (!streamCopies.has(card.id)) streamCopies.set(card.id, card);
    }),
  );
  const seen = new Set<string>();
  const merged: TCard[] = [];
  const take = (card: TCard) => {
    if (seen.has(card.id)) return;
    seen.add(card.id);
    merged.push(streamCopies.get(card.id) ?? card);
  };
  leading.forEach(take);
  pages?.forEach((page) => page.items.forEach(take));
  return merged;
}

/** Every cached copy of card `id` with `patch` applied, for `setQueriesData` over the feed's infinite caches. */
export function withCardPatched<
  TCard extends { id: string },
  TData extends { pages: readonly { items: readonly TCard[] }[] },
>(
  data: TData | undefined,
  id: string,
  patch: Partial<TCard>,
): TData | undefined {
  if (!data) return data;
  // Same shape as `data`, only the matching cards replaced; spreading loses the generic, hence the cast.
  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      items: page.items.map((card) =>
        card.id === id ? { ...card, ...patch } : card,
      ),
    })),
  } as TData;
}

/** Sector codes seen on loaded cards, most frequent first: the cold-start row's chips. No new API. */
export function sectorsSeen(
  cards: readonly Pick<FeedKitCard, "project">[],
  limit = 6,
): string[] {
  const counts = new Map<string, number>();
  for (const card of cards) {
    for (const code of card.project?.sectors ?? []) {
      const sector = code.trim().toUpperCase();
      if (!sector || sector === "OTHER") continue;
      counts.set(sector, (counts.get(sector) ?? 0) + 1);
    }
  }
  // Map keeps first-seen order and Array.sort is stable, so ties stay in the order they appeared.
  return [...counts.entries()]
    .sort((left, right) => right[1] - left[1])
    .slice(0, limit)
    .map(([sector]) => sector);
}

/** A sector code as a tag the API's canon resolves back to the sector: FOOD_BEVERAGE -> "food-beverage". */
export function sectorTag(sectorCode: string): string {
  return sectorCode.trim().toLowerCase().replace(/_/g, "-");
}

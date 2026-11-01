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
 * Leading (locally inserted) cards first, then every page in order, each id once.
 * The API never repeats a card in one scroll; this is the defensive second check.
 */
export function mergeFeedPages<TCard extends { id: string }>(
  leading: readonly TCard[],
  pages: readonly { items: readonly TCard[] }[] | undefined,
): TCard[] {
  const seen = new Set<string>();
  const merged: TCard[] = [];
  const take = (card: TCard) => {
    if (seen.has(card.id)) return;
    seen.add(card.id);
    merged.push(card);
  };
  leading.forEach(take);
  pages?.forEach((page) => page.items.forEach(take));
  return merged;
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

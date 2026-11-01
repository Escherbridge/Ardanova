import {
  FEED_REASON_KINDS,
  type FeedReason,
  type FeedReasonKind,
  type FeedScope,
} from "~/lib/contracts/feed-contract";
import { categoryLabel } from "~/lib/taxonomy/labels";

import type { FeedKitCard } from "./types";

const isKnownReasonKind = (kind: string): kind is FeedReasonKind =>
  (FEED_REASON_KINDS as readonly string[]).includes(kind);

/**
 * The card's one reason fragment ("Following Barn raising", "#carpentry", "New in Construction").
 * Known kinds render client-side from kind + aboutId, never from the server's `text`; only a kind this
 * client does not know falls back to that text. Reasons that restate the page's scope are dropped.
 * See components/feed/AGENTS.md §Reasons.
 */
export function reasonFragment(
  card: FeedKitCard,
  scope?: FeedScope,
): string | null {
  const relevant = card.reasons.filter(
    (reason) => !restatesScope(reason, scope),
  );
  for (const reason of relevant) {
    if (!isKnownReasonKind(reason.kind)) continue;
    const fragment = knownFragment(reason.kind, reason, card);
    if (fragment) return fragment;
  }
  const unknown = relevant.find((reason) => !isKnownReasonKind(reason.kind));
  return unknown?.text.trim() ? unknown.text.trim() : null;
}

function knownFragment(
  kind: FeedReasonKind,
  reason: FeedReason,
  card: FeedKitCard,
): string | null {
  switch (kind) {
    case "FollowedProject":
      return `Following ${projectName(reason, card)}`;
    case "ProjectTeam":
      return `You're on ${projectName(reason, card)}`;
    case "FollowedGuild":
      return `Following ${guildName(reason, card)}`;
    case "GuildMember":
      return `You're in ${guildName(reason, card)}`;
    case "FollowedPerson":
      return `Following ${personName(reason, card)}`;
    case "YourActivity":
      // The card already shows your name; saying "yours" would narrate it.
      return null;
    case "TagInterest":
      return tagFragment(reason);
    case "YourSector":
      return `In ${sectorName(reason)}`;
    case "NewInSector":
      return `New in ${sectorName(reason)}`;
    case "NewProject":
      return "New project";
    case "Discovery":
      if (reason.about === "SECTOR") return `In ${sectorName(reason)}`;
      if (reason.about === "TAG") return tagFragment(reason);
      return "New to you";
    case "InScope":
      if (reason.about === "PROJECT")
        return `From ${projectName(reason, card)}`;
      if (reason.about === "GUILD") return `From ${guildName(reason, card)}`;
      if (reason.about === "TAG") return tagFragment(reason);
      if (reason.about === "SECTOR") return `In ${sectorName(reason)}`;
      return null;
    default:
      return unreachable(kind);
  }
}

/** On a project, guild or tag page, a reason naming that same subject says what the page already says. */
function restatesScope(reason: FeedReason, scope?: FeedScope): boolean {
  if (!scope || scope === "home" || !reason.aboutId) return false;
  const separator = scope.indexOf(":");
  const scopeKind = scope.slice(0, separator);
  const scopeId = scope.slice(separator + 1);
  switch (scopeKind) {
    case "project":
      return reason.about === "PROJECT" && reason.aboutId === scopeId;
    case "guild":
      return reason.about === "GUILD" && reason.aboutId === scopeId;
    case "tag":
      return (
        reason.about === "TAG" &&
        reason.aboutId.toLowerCase() === scopeId.replace(/^#/, "").toLowerCase()
      );
    default:
      return false;
  }
}

function projectName(reason: FeedReason, card: FeedKitCard): string {
  const project = card.project;
  return project && (reason.aboutId === null || reason.aboutId === project.id)
    ? project.title
    : "a project";
}

function guildName(reason: FeedReason, card: FeedKitCard): string {
  const guild = card.guild;
  return guild && (reason.aboutId === null || reason.aboutId === guild.id)
    ? guild.name
    : "a guild";
}

function personName(reason: FeedReason, card: FeedKitCard): string {
  const name = card.author.name?.trim();
  return name && (reason.aboutId === null || reason.aboutId === card.author.id)
    ? name
    : "this person";
}

function sectorName(reason: FeedReason): string {
  return reason.aboutId ? categoryLabel(reason.aboutId) : "your sector";
}

function tagFragment(reason: FeedReason): string | null {
  return reason.aboutId ? `#${reason.aboutId}` : null;
}

function unreachable(value: never): never {
  throw new Error(`Unhandled feed reason kind: ${String(value)}`);
}

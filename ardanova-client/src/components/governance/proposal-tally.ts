/** One option a member can vote for, with the ballots it has received so far. */
export interface TallyOption {
  label: string;
  choice: number;
  votes: number;
  percentage: number;
}

/** Ballots per choice as the API serialises them (JSON object keys arrive as strings). */
export type VotesByChoice = Record<string, number | undefined> | undefined | null;

/**
 * Reads a proposal's declared options and joins them to the tally, mirroring the API's
 * `TryReadChoices`: a string entry is choice = its index; an object entry uses its integer
 * `choice` when present, else its index. Unreadable options yield [], never invented ones.
 */
export function readProposalOptions(
  optionsJson: string | null | undefined,
  votesByChoice?: VotesByChoice,
): TallyOption[] {
  let parsed: unknown;
  try {
    parsed = JSON.parse(optionsJson ?? "");
  } catch {
    return [];
  }
  if (!Array.isArray(parsed)) return [];

  const declared: Array<{ label: string; choice: number }> = [];
  for (const [index, entry] of parsed.entries()) {
    if (typeof entry === "string") {
      if (entry.trim() === "") return [];
      declared.push({ label: entry, choice: index });
    } else if (typeof entry === "object" && entry !== null) {
      const record = entry as Record<string, unknown>;
      const choice =
        typeof record.choice === "number" && Number.isInteger(record.choice)
          ? record.choice
          : index;
      const label =
        typeof record.label === "string" && record.label.trim() !== ""
          ? record.label
          : `Option ${index + 1}`;
      declared.push({ label, choice });
    } else {
      return [];
    }
  }

  const counts = votesByChoice ?? {};
  const votesCast = Object.values(counts).reduce<number>((sum, n) => sum + (n ?? 0), 0);
  return declared.map(({ label, choice }) => {
    const votes = counts[String(choice)] ?? 0;
    return {
      label,
      choice,
      votes,
      percentage: votesCast > 0 ? Math.round((votes * 100) / votesCast) : 0,
    };
  });
}

/** Turnout toward quorum in members, as the API counts it; 0 when there is no electorate. */
export function quorumProgressPercent(
  votesCast: number,
  votesNeededForQuorum: number,
): number {
  if (votesNeededForQuorum <= 0) return 0;
  return Math.min(Math.round((votesCast * 100) / votesNeededForQuorum), 100);
}

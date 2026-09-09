/**
 * Currency marking for the money figures the API sends bare.
 *
 * `ProjectLedgerService.FormatAmount` is `value.ToString("F2", InvariantCulture)`, so
 * `sponsorshipTotalUsdText` and `usdReferenceText` arrive as literally `"1250.00"` — a number
 * with no symbol, no grouping and nothing that says what it counts. That is correct for a
 * server: the figure is the record, and a server has no business guessing a locale. It is wrong
 * for a screen.
 *
 * Why this matters here and not everywhere. The project money tab stacks two large mono
 * numbers: the project's own balance, which is followed by its unit label (`ARDA`, or a
 * project's own unit name), and the sponsorship total, which is dollars. Printing the second
 * one unlabelled next to the first invites a sponsor to read a dollar total as project units —
 * on the one screen they act from. D52: the ask is entered in dollars and people think in
 * dollars, so the dollar figure is the one that must never be ambiguous.
 *
 * The digits are passed through UNCHANGED. Re-grouping or re-rounding here would mean the
 * screen shows a figure the ledger never produced, and the whole point of the server sending
 * text rather than a float is that the record is exact. This adds a marker, nothing else.
 */

/** A figure already carrying a currency marker must not get a second one. */
function alreadyMarked(text: string): boolean {
  return text.startsWith("$") || text.startsWith("US$");
}

/**
 * The dollar figure a person reads, or null when there is nothing to show.
 *
 * Null, blank and whitespace all return null rather than `"$"` — a lone symbol is worse than an
 * absent figure, because it reads as a value of nothing.
 */
export function asDollarFigure(
  text: string | null | undefined,
): string | null {
  const trimmed = text?.trim() ?? "";
  if (trimmed.length === 0) return null;
  return alreadyMarked(trimmed) ? trimmed : `$${trimmed}`;
}

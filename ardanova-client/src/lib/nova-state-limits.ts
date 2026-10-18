/** Nova continuation `state` size limits, shared by the tRPC input and tab storage. See `src/components/ai/AGENTS.md` §Session ticket size. */

/** Largest upstream (OGAI) state the BFF accepted before tickets existed; the .NET twin is `NovaSessionTickets.MaxUpstreamStateLength`. */
export const OGAI_STATE_MAX_CHARS = 512;

/** `nv1.` + 10-digit unix expiry + `.` + 64 hex HMAC-SHA256 + `.`; the .NET twin is `NovaSessionTickets.EnvelopeLength`. */
export const NOVA_TICKET_ENVELOPE_CHARS =
  "nv1.".length + 10 + ".".length + 64 + ".".length;

/** Longest `state` a member can legitimately carry back: a ticket around a maximal upstream state. */
export const NOVA_STATE_MAX_CHARS =
  NOVA_TICKET_ENVELOPE_CHARS + OGAI_STATE_MAX_CHARS;

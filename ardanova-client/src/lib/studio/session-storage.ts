import { z } from "zod";

export const STUDIO_MODES = [
  "Project brief",
  "Presentation",
  "Rehearsal",
] as const;
export const STUDIO_SESSION_KEY = "ardanova:nova-studio:v2";
export const STUDIO_HISTORY_LIMIT = 25;
export const STUDIO_SESSION_MAX_BYTES = 2_000_000;

const studioUpstreamSchema = z.object({
  mode: z.enum(["Project brief", "Presentation"]),
  contentVersion: z.number().int().positive(),
});

const studioSnapshotSchema = z.object({
  content: z.string().max(50_000),
  contentVersion: z.number().int().positive(),
  decision: z.enum(["pending", "accepted", "rejected"]),
  sourceLabel: z.string().max(1_000),
  changeLabel: z.string().max(1_000),
  upstream: studioUpstreamSchema.nullable(),
});

const studioHistorySchema = z.object({
  past: z.array(studioSnapshotSchema).max(STUDIO_HISTORY_LIMIT),
  present: studioSnapshotSchema.nullable(),
});

export const studioSessionSchema = z.object({
  version: z.literal(2),
  mode: z.enum(STUDIO_MODES),
  brief: z.object({
    title: z.string().max(500),
    purpose: z.string().max(10_000),
    people: z.string().max(10_000),
    evidence: z.string().max(10_000),
  }),
  artifacts: z.object({
    "Project brief": studioHistorySchema,
    Presentation: studioHistorySchema,
    Rehearsal: studioHistorySchema,
  }),
  edit: z
    .object({
      mode: z.enum(STUDIO_MODES),
      buffer: z.string().max(50_000),
    })
    .nullable()
    .optional(),
});

export type StudioMode = (typeof STUDIO_MODES)[number];
export type StudioUpstreamMode = Extract<
  StudioMode,
  "Project brief" | "Presentation"
>;
export type StudioSnapshot = z.infer<typeof studioSnapshotSchema>;
export type StudioHistory = z.infer<typeof studioHistorySchema>;
export type StudioArtifacts = Record<StudioMode, StudioHistory>;
export type StudioSession = z.infer<typeof studioSessionSchema>;

export function studioStorageBytes(value: string): number {
  return new TextEncoder().encode(value).byteLength;
}

export function compactStudioArtifacts(
  artifacts: StudioArtifacts,
): StudioArtifacts {
  return Object.fromEntries(
    STUDIO_MODES.map((mode) => [
      mode,
      {
        past: artifacts[mode].past.slice(-STUDIO_HISTORY_LIMIT),
        present: artifacts[mode].present,
      },
    ]),
  ) as StudioArtifacts;
}

export function parseStudioSession(
  serialized: string | null,
): StudioSession | null {
  if (
    !serialized ||
    studioStorageBytes(serialized) > STUDIO_SESSION_MAX_BYTES
  ) {
    return null;
  }

  try {
    const result = studioSessionSchema.safeParse(JSON.parse(serialized));
    return result.success ? result.data : null;
  } catch {
    return null;
  }
}

export function serializeStudioSession(value: unknown): string | null {
  const parsed = studioSessionSchema.safeParse(value);
  if (!parsed.success) return null;

  const session: StudioSession = {
    ...parsed.data,
    artifacts: compactStudioArtifacts(parsed.data.artifacts),
  };

  while (true) {
    const serialized = JSON.stringify(session);
    if (studioStorageBytes(serialized) <= STUDIO_SESSION_MAX_BYTES) {
      return serialized;
    }

    const modeToPrune = STUDIO_MODES.filter(
      (mode) => session.artifacts[mode].past.length > 0,
    ).sort(
      (left, right) =>
        session.artifacts[right].past.length -
        session.artifacts[left].past.length,
    )[0];
    if (!modeToPrune) return null;
    session.artifacts[modeToPrune].past.shift();
  }
}

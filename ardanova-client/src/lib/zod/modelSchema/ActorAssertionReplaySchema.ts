import { z } from 'zod';

/////////////////////////////////////////
// ACTOR ASSERTION REPLAY SCHEMA
/////////////////////////////////////////

export const ActorAssertionReplaySchema = z.object({
  jti: z.string(),
  expiresAt: z.coerce.date(),
  consumedAt: z.coerce.date(),
  subject: z.string(),
  requestTarget: z.string(),
  bodySha256: z.string(),
})

export type ActorAssertionReplay = z.infer<typeof ActorAssertionReplaySchema>

export default ActorAssertionReplaySchema;

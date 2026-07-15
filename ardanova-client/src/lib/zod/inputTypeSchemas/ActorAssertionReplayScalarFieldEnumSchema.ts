import { z } from 'zod';

export const ActorAssertionReplayScalarFieldEnumSchema = z.enum(['jti','expiresAt','consumedAt','subject','requestTarget','bodySha256']);

export default ActorAssertionReplayScalarFieldEnumSchema;

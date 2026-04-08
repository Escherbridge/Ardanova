import { z } from 'zod';

/////////////////////////////////////////
// GUILD UPDATE SCHEMA
/////////////////////////////////////////

export const GuildUpdateSchema = z.object({
  id: z.string().cuid(),
  guildId: z.string(),
  createdById: z.string(),
  title: z.string(),
  content: z.string(),
  images: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type GuildUpdate = z.infer<typeof GuildUpdateSchema>

export default GuildUpdateSchema;

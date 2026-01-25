import { z } from 'zod';

/////////////////////////////////////////
// USER EXPERIENCE SCHEMA
/////////////////////////////////////////

export const UserExperienceSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  title: z.string(),
  company: z.string(),
  description: z.string().nullable(),
  startDate: z.coerce.date(),
  endDate: z.coerce.date().nullable(),
  isCurrent: z.boolean(),
})

export type UserExperience = z.infer<typeof UserExperienceSchema>

export default UserExperienceSchema;

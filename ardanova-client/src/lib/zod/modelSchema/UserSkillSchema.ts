import { z } from 'zod';

/////////////////////////////////////////
// USER SKILL SCHEMA
/////////////////////////////////////////

export const UserSkillSchema = z.object({
  id: z.string().cuid(),
  userId: z.string(),
  skill: z.string(),
  level: z.number().int(),
})

export type UserSkill = z.infer<typeof UserSkillSchema>

export default UserSkillSchema;

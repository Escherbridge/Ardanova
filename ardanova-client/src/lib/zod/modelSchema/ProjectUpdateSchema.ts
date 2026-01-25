import { z } from 'zod';

/////////////////////////////////////////
// PROJECT UPDATE SCHEMA
/////////////////////////////////////////

export const ProjectUpdateSchema = z.object({
  id: z.string().cuid(),
  projectId: z.string(),
  userId: z.string(),
  title: z.string(),
  content: z.string(),
  images: z.string().nullable(),
  createdAt: z.coerce.date(),
})

export type ProjectUpdate = z.infer<typeof ProjectUpdateSchema>

export default ProjectUpdateSchema;

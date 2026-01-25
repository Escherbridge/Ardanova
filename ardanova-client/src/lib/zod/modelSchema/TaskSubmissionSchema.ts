import { z } from 'zod';
import { SubmissionStatusSchema } from '../inputTypeSchemas/SubmissionStatusSchema'

/////////////////////////////////////////
// TASK SUBMISSION SCHEMA
/////////////////////////////////////////

export const TaskSubmissionSchema = z.object({
  status: SubmissionStatusSchema,
  id: z.string().cuid(),
  taskId: z.string(),
  submittedById: z.string(),
  content: z.string(),
  attachments: z.string().nullable(),
  reviewedById: z.string().nullable(),
  feedback: z.string().nullable(),
  submittedAt: z.coerce.date(),
  reviewedAt: z.coerce.date().nullable(),
})

export type TaskSubmission = z.infer<typeof TaskSubmissionSchema>

export default TaskSubmissionSchema;

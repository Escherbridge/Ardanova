import { z } from 'zod';

export const TaskSubmissionScalarFieldEnumSchema = z.enum(['id','taskId','submittedById','content','attachments','status','reviewedById','feedback','submittedAt','reviewedAt']);

export default TaskSubmissionScalarFieldEnumSchema;

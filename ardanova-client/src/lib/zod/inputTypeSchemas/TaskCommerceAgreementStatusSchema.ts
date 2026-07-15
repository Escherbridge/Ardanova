import { z } from 'zod';

export const TaskCommerceAgreementStatusSchema = z.enum(['DRAFT','ACCEPTED','ESCROW_FUNDED','QUEST_LINKED','RELEASE_AUTHORIZED','SETTLEMENT_PENDING','SETTLED','CANCELLED','FAILED']);

export type TaskCommerceAgreementStatusType = `${z.infer<typeof TaskCommerceAgreementStatusSchema>}`

export default TaskCommerceAgreementStatusSchema;

import { z } from 'zod';

export const XPEventTypeSchema = z.enum(['TASK_COMPLETED','PROPOSAL_CREATED','PROPOSAL_PASSED','VOTE_CAST','PROJECT_FUNDED','MEMBER_REFERRED','ACHIEVEMENT_EARNED','STREAK_MAINTAINED','LEVEL_UP','REVIEW_GIVEN','CONTRIBUTION_MADE']);

export type XPEventTypeType = `${z.infer<typeof XPEventTypeSchema>}`

export default XPEventTypeSchema;

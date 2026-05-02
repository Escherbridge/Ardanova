import { z } from 'zod';

export const MembershipGrantTypeSchema = z.enum(['FOUNDER','DAO_VOTE','CONTRIBUTION_THRESHOLD','APPLICATION_APPROVED','GAME_SDK_THRESHOLD']);

export type MembershipGrantTypeType = `${z.infer<typeof MembershipGrantTypeSchema>}`

export default MembershipGrantTypeSchema;

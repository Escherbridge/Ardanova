import { z } from 'zod';

export const TransactionTypeSchema = z.enum(['DEPOSIT','WITHDRAWAL','TASK_PAYMENT','PROPOSAL_EXECUTION','DIVIDEND','FEE']);

export type TransactionTypeType = `${z.infer<typeof TransactionTypeSchema>}`

export default TransactionTypeSchema;

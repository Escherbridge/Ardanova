import { z } from 'zod';

export const TaskEscrowScalarFieldEnumSchema = z.enum(['id','taskId','funderId','shareId','amount','status','txHashFund','txHashRelease','txHashRefund','createdAt','fundedAt','releasedAt','refundedAt']);

export default TaskEscrowScalarFieldEnumSchema;

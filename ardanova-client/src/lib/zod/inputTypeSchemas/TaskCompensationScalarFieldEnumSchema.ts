import { z } from 'zod';

export const TaskCompensationScalarFieldEnumSchema = z.enum(['id','taskId','model','tokenAmount','hourlyRate','equityPercent','stableCoinAmount','vestingMonths','createdAt']);

export default TaskCompensationScalarFieldEnumSchema;

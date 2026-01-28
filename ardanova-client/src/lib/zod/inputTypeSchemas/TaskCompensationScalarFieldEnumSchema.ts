import { z } from 'zod';

export const TaskCompensationScalarFieldEnumSchema = z.enum(['id','taskId','compensationModel','shareAmount','hourlyRate','equityPercent','stableCoinAmount','vestingMonths','createdAt']);

export default TaskCompensationScalarFieldEnumSchema;

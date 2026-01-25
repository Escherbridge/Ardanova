import { z } from 'zod';

export const SprintItemScalarFieldEnumSchema = z.enum(['id','sprintId','taskId','order','addedAt']);

export default SprintItemScalarFieldEnumSchema;

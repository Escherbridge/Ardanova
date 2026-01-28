import { z } from 'zod';

export const EpicScalarFieldEnumSchema = z.enum(['id','phaseId','title','description','status','priority','equityBudget','progress','startDate','targetDate','createdAt','updatedAt','assigneeId']);

export default EpicScalarFieldEnumSchema;

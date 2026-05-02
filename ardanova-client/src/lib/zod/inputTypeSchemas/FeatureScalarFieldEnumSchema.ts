import { z } from 'zod';

export const FeatureScalarFieldEnumSchema = z.enum(['id','sprintId','title','description','status','priority','order','createdAt','updatedAt','assigneeId']);

export default FeatureScalarFieldEnumSchema;

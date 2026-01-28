import { z } from 'zod';

export const ProductBacklogItemScalarFieldEnumSchema = z.enum(['id','epicId','title','description','type','storyPoints','status','acceptanceCriteria','priority','createdAt','updatedAt','assigneeId']);

export default ProductBacklogItemScalarFieldEnumSchema;

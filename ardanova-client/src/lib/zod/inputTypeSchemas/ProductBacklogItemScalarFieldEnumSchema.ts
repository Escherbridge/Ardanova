import { z } from 'zod';

export const ProductBacklogItemScalarFieldEnumSchema = z.enum(['id','projectId','featureId','sprintId','epicId','milestoneId','guildId','title','description','type','storyPoints','status','acceptanceCriteria','priority','equityReward','createdAt','updatedAt','assigneeId']);

export default ProductBacklogItemScalarFieldEnumSchema;

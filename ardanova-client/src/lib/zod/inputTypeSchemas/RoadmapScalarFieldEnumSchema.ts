import { z } from 'zod';

export const RoadmapScalarFieldEnumSchema = z.enum(['id','projectId','title','vision','status','createdAt','updatedAt','assigneeId']);

export default RoadmapScalarFieldEnumSchema;

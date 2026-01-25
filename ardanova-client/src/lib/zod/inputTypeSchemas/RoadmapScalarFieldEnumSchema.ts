import { z } from 'zod';

export const RoadmapScalarFieldEnumSchema = z.enum(['id','projectId','title','vision','status','createdAt','updatedAt']);

export default RoadmapScalarFieldEnumSchema;

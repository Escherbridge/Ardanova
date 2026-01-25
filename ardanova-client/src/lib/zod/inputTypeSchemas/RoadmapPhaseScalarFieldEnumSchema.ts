import { z } from 'zod';

export const RoadmapPhaseScalarFieldEnumSchema = z.enum(['id','roadmapId','name','description','order','startDate','endDate','status','createdAt','updatedAt']);

export default RoadmapPhaseScalarFieldEnumSchema;

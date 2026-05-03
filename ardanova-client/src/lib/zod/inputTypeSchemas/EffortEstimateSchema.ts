import { z } from 'zod';

export const EffortEstimateSchema = z.enum(['XS','S','M','L','XL']);

export type EffortEstimateType = `${z.infer<typeof EffortEstimateSchema>}`

export default EffortEstimateSchema;

import { z } from 'zod';

export const TaxonomyKindSchema = z.enum(['SECTOR','SKILL','PROJECT_TYPE']);

export type TaxonomyKindType = `${z.infer<typeof TaxonomyKindSchema>}`

export default TaxonomyKindSchema;

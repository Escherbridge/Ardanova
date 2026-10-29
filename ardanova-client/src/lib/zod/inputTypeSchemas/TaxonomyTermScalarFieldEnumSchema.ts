import { z } from 'zod';

export const TaxonomyTermScalarFieldEnumSchema = z.enum(['id','kind','slug','label','description','parentId','sortOrder','isActive','createdAt','updatedAt']);

export default TaxonomyTermScalarFieldEnumSchema;

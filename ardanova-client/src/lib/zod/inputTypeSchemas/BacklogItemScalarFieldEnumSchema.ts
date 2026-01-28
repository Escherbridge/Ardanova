import { z } from 'zod';

export const BacklogItemScalarFieldEnumSchema = z.enum(['id','pbiId','title','description','type','status','estimate','createdAt','updatedAt','assigneeId']);

export default BacklogItemScalarFieldEnumSchema;

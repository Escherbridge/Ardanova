import { z } from 'zod';

export const ShopScalarFieldEnumSchema = z.enum(['id','name','description','address','phone','email','website','logo','isActive','createdAt','updatedAt','ownerId']);

export default ShopScalarFieldEnumSchema;

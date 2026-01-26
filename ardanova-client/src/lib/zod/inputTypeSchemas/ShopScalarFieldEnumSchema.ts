import { z } from 'zod';

export const ShopScalarFieldEnumSchema = z.enum(['id','name','slug','description','category','address','phone','email','website','logo','isActive','createdAt','updatedAt','ownerId']);

export default ShopScalarFieldEnumSchema;

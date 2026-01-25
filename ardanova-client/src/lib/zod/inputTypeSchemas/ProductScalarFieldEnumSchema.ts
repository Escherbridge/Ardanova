import { z } from 'zod';

export const ProductScalarFieldEnumSchema = z.enum(['id','shopId','name','description','sku','price','cost','category','isActive','createdAt','updatedAt','userId']);

export default ProductScalarFieldEnumSchema;

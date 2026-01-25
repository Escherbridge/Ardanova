import { z } from 'zod';

export const InventoryItemScalarFieldEnumSchema = z.enum(['id','shopId','productId','currentStock','minStock','maxStock','reorderPoint','lastRestocked','createdAt','updatedAt','userId']);

export default InventoryItemScalarFieldEnumSchema;

import { z } from 'zod';

/////////////////////////////////////////
// INVENTORY ITEM SCHEMA
/////////////////////////////////////////

export const InventoryItemSchema = z.object({
  id: z.string().cuid(),
  shopId: z.string(),
  productId: z.string(),
  currentStock: z.number().int(),
  minStock: z.number().int(),
  maxStock: z.number().int().nullable(),
  reorderPoint: z.number().int().nullable(),
  lastRestocked: z.coerce.date().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  userId: z.string(),
})

export type InventoryItem = z.infer<typeof InventoryItemSchema>

export default InventoryItemSchema;

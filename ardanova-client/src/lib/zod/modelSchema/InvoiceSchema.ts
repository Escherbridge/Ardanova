import { z } from 'zod';
import { Prisma } from '@prisma/client'
import { InvoiceStatusSchema } from '../inputTypeSchemas/InvoiceStatusSchema'

/////////////////////////////////////////
// INVOICE SCHEMA
/////////////////////////////////////////

export const InvoiceSchema = z.object({
  status: InvoiceStatusSchema,
  id: z.string().cuid(),
  shopId: z.string(),
  buyerId: z.string(),
  invoiceNumber: z.string(),
  amount: z.instanceof(Prisma.Decimal, { message: "Field 'amount' must be a Decimal. Location: ['Models', 'Invoice']"}),
  tax: z.instanceof(Prisma.Decimal, { message: "Field 'tax' must be a Decimal. Location: ['Models', 'Invoice']"}).nullable(),
  discount: z.instanceof(Prisma.Decimal, { message: "Field 'discount' must be a Decimal. Location: ['Models', 'Invoice']"}).nullable(),
  total: z.instanceof(Prisma.Decimal, { message: "Field 'total' must be a Decimal. Location: ['Models', 'Invoice']"}),
  dueDate: z.coerce.date(),
  paidAt: z.coerce.date().nullable(),
  notes: z.string().nullable(),
  createdAt: z.coerce.date(),
  updatedAt: z.coerce.date(),
  userId: z.string(),
})

export type Invoice = z.infer<typeof InvoiceSchema>

export default InvoiceSchema;

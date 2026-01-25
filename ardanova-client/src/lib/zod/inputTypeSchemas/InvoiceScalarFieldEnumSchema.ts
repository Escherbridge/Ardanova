import { z } from 'zod';

export const InvoiceScalarFieldEnumSchema = z.enum(['id','shopId','buyerId','invoiceNumber','amount','tax','discount','total','status','dueDate','paidAt','notes','createdAt','updatedAt','userId']);

export default InvoiceScalarFieldEnumSchema;

import { z } from "zod";

export const taskEscrowStatusSchema = z.enum([
  "NONE",
  "FUNDED",
  "RELEASED",
  "DISPUTED",
  "REFUNDED",
]);

export const taskEscrowDtoSchema = z.object({
  id: z.string().min(1),
  taskId: z.string().min(1),
  funderId: z.string().min(1),
  shareId: z.string().min(1),
  amount: z.number().nonnegative(),
  status: taskEscrowStatusSchema,
  txHashFund: z.string().nullable().optional(),
  txHashRelease: z.string().nullable().optional(),
  txHashRefund: z.string().nullable().optional(),
  disputeReason: z.string().nullable().optional(),
  disputeDescription: z.string().nullable().optional(),
  disputedByUserId: z.string().nullable().optional(),
  createdAt: z.string().min(1),
  fundedAt: z.string().nullable().optional(),
  releasedAt: z.string().nullable().optional(),
  refundedAt: z.string().nullable().optional(),
  disputedAt: z.string().nullable().optional(),
});

export type TaskEscrowStatus = z.infer<typeof taskEscrowStatusSchema>;
export type TaskEscrowDto = z.infer<typeof taskEscrowDtoSchema>;

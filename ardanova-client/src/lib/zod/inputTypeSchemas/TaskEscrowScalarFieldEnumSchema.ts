import { z } from "zod";

export const TaskEscrowScalarFieldEnumSchema = z.enum([
  "id",
  "taskId",
  "funderId",
  "shareId",
  "amount",
  "status",
  "txHashFund",
  "txHashRelease",
  "txHashRefund",
  "disputeReason",
  "disputeDescription",
  "disputedByUserId",
  "createdAt",
  "fundedAt",
  "releasedAt",
  "refundedAt",
  "disputedAt",
]);

export default TaskEscrowScalarFieldEnumSchema;

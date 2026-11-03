import { z } from "zod";
import { apiClient } from "~/lib/api";
import type { ApiResponse } from "~/lib/api/base-client";
import { apiPath } from "~/lib/api/path";
import * as contract from "~/lib/azoa-money/contracts";
import { toSponsorSafeMessage } from "~/lib/commerce/on-ramp-status";
import { moneyRefusalError } from "~/server/api/money-refusal-error";
import { createTRPCRouter, protectedProcedure } from "~/server/api/trpc";

async function read<T>(
  call: Promise<ApiResponse<unknown>>,
  schema: z.ZodType<T>,
): Promise<T> {
  const response = await call;
  if (response.error || response.data === undefined || response.data === null) {
    throw moneyRefusalError(
      response.status >= 200 && response.status < 300
        ? { status: 502, code: "unconfirmed" }
        : { status: response.status, code: response.code },
      toSponsorSafeMessage(
        response.error,
        "We could not confirm the result. Your saved request stays available.",
      ),
    );
  }
  const value = schema.safeParse(response.data);
  if (!value.success)
    throw moneyRefusalError(
      { status: 502, code: "unconfirmed" },
      "We could not confirm the result. Your saved request stays available.",
    );
  return value.data;
}

const subjectSchema = z.object({ expectedSubject: z.string().min(1).max(128) });
const subjectProcedure = protectedProcedure
  .input(subjectSchema)
  .use(({ ctx, input, next }) => {
    if (input.expectedSubject !== ctx.session.user.id)
      throw moneyRefusalError(
        { status: 401, code: "unauthenticated" },
        "The signed-in account changed. Your saved request stays with its original account.",
      );
    return next();
  });
const idInput = z
  .object({ id: contract.recordIdSchema, ...subjectSchema.shape })
  .strict();
const keyInput = z
  .object({ requestKey: contract.requestKeySchema, ...subjectSchema.shape })
  .strict();

export const moneyRouter = createTRPCRouter({
  accountCapabilities: subjectProcedure.query(() =>
    read(
      apiClient.azoaCustodialAccount.getCapabilities(),
      contract.accountCapabilitiesSchema,
    ),
  ),
  accountStatus: subjectProcedure.query(() =>
    read(apiClient.azoaCustodialAccount.getStatus(), contract.accountSchema),
  ),
  ensureAccount: subjectProcedure.mutation(() =>
    read(apiClient.azoaCustodialAccount.ensure(), contract.accountSchema),
  ),
  beginKyc: subjectProcedure.mutation(() =>
    read(apiClient.azoaCustodialAccount.beginKyc(), contract.kycSchema),
  ),
  revokeConsent: subjectProcedure.mutation(() =>
    read(apiClient.azoaCustodialAccount.revokeConsent(), contract.revokeSchema),
  ),
  checkoutCapabilities: subjectProcedure.query(() =>
    read(
      apiClient.get("/api/add-funds/capabilities"),
      contract.capabilitiesSchema,
    ),
  ),
  beginCheckout: subjectProcedure
    .input(contract.moneyRequestSchema.extend(subjectSchema.shape))
    .mutation(({ input }) =>
      read(
        apiClient.addFunds.begin(
          { amountMinorUnits: input.amountMinorUnits },
          input.requestKey,
        ),
        contract.checkoutSchema,
      ),
    ),
  getCheckout: subjectProcedure
    .input(idInput)
    .query(({ input }) =>
      read(apiClient.addFunds.getPayment(input.id), contract.checkoutSchema),
    ),
  voidCheckout: subjectProcedure
    .input(keyInput)
    .mutation(({ input }) =>
      read(
        apiClient.addFunds.voidRequest(input.requestKey),
        contract.checkoutVoidSchema,
      ),
    ),
  cashOutAvailability: subjectProcedure.query(() =>
    read(
      apiClient.get("/api/Payouts/withdraw/availability"),
      contract.availabilitySchema,
    ),
  ),
  cashOutDestination: subjectProcedure.query(() =>
    read(
      apiClient.get("/api/Payouts/withdraw/destination"),
      contract.destinationSchema,
    ),
  ),
  startCashOut: subjectProcedure
    .input(contract.moneyRequestSchema.extend(subjectSchema.shape))
    .mutation(({ input }) =>
      read(
        apiClient.post("/api/Payouts/withdraw", {
          requestKey: input.requestKey,
          amountMinorUnits: input.amountMinorUnits,
        }),
        contract.withdrawalSchema,
      ),
    ),
  voidCashOut: subjectProcedure
    .input(keyInput)
    .mutation(({ input }) =>
      read(
        apiClient.post(
          apiPath`/api/Payouts/withdraw/requests/${input.requestKey}/void`,
          {},
        ),
        contract.cashOutVoidSchema,
      ),
    ),
  voidSale: subjectProcedure
    .input(
      z
        .object({
          requestKey: contract.requestKeySchema,
          quoteId: contract.recordIdSchema,
          ...subjectSchema.shape,
        })
        .strict(),
    )
    .mutation(({ input }) =>
      read(
        apiClient.post(
          apiPath`/api/Payouts/sale/requests/${input.requestKey}/void`,
          { quoteId: input.quoteId },
        ),
        contract.saleVoidSchema,
      ),
    ),
  cashOutHistory: subjectProcedure.query(() =>
    read(
      apiClient.get("/api/Payouts/withdraw"),
      z.array(contract.withdrawalSchema),
    ),
  ),
  sellable: subjectProcedure.query(() =>
    read(
      apiClient.get("/api/Payouts/sellable"),
      z.array(contract.sellableSchema),
    ),
  ),
  quoteSale: subjectProcedure
    .input(
      contract.holdingSchema
        .extend({
          sourceUnitAmount: z.number().int().positive().max(2147483647),
          ...subjectSchema.shape,
        })
        .strict(),
    )
    .mutation(({ input }) =>
      read(
        apiClient.post("/api/Payouts/sale/quotes", {
          projectTokenConfigId: input.projectTokenConfigId,
          holderClass: input.holderClass,
          sourceUnitAmount: input.sourceUnitAmount,
        }),
        contract.saleQuoteSchema,
      ),
    ),
  sell: subjectProcedure
    .input(
      z
        .object({
          requestKey: contract.requestKeySchema,
          quoteId: contract.recordIdSchema,
          ...subjectSchema.shape,
        })
        .strict(),
    )
    .mutation(({ input }) =>
      read(
        apiClient.post("/api/Payouts/sale", {
          requestKey: input.requestKey,
          quoteId: input.quoteId,
        }),
        contract.saleSchema,
      ),
    ),
  getSale: subjectProcedure
    .input(idInput)
    .query(({ input }) =>
      read(
        apiClient.get(apiPath`/api/Payouts/sale/${input.id}`),
        contract.saleSchema,
      ),
    ),
  createHandoff: subjectProcedure.mutation(() =>
    read(
      apiClient.post("/api/azoa/custodial-account/handoff", {}),
      contract.handoffSchema,
    ),
  ),
  resendHandoff: subjectProcedure
    .input(idInput)
    .mutation(({ input }) =>
      read(
        apiClient.post(
          apiPath`/api/azoa/custodial-account/handoff/${input.id}/resend`,
          {},
        ),
        contract.handoffSchema,
      ),
    ),
  getHandoff: subjectProcedure
    .input(idInput)
    .query(({ input }) =>
      read(
        apiClient.get(apiPath`/api/azoa/custodial-account/handoff/${input.id}`),
        contract.handoffSchema,
      ),
    ),
});

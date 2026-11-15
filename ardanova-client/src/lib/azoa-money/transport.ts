import type {
  AzoaMoneyTransport,
  MoneyResult,
  MoneyRefusalCode,
  CheckoutView,
  SaleView,
} from "azoa-sdk/money";
import { z } from "zod";
import * as c from "./contracts";
import { moneyCodeOf } from "~/lib/commerce/money-refusal";

export interface MoneyHost {
  voidCashOut(requestKey: string): Promise<unknown>;
  voidSale(request: { requestKey: string; quoteId: string }): Promise<unknown>;
  accountCapabilities(): Promise<unknown>;
  accountStatus(): Promise<unknown>;
  ensureAccount(): Promise<unknown>;
  beginKyc(): Promise<unknown>;
  revokeConsent(): Promise<unknown>;
  checkoutCapabilities(): Promise<unknown>;
  beginCheckout(request: {
    requestKey: string;
    amountMinorUnits: number;
  }): Promise<unknown>;
  getCheckout(id: string): Promise<unknown>;
  voidCheckout(requestKey: string): Promise<unknown>;
  cashOutAvailability(): Promise<unknown>;
  cashOutDestination(): Promise<unknown>;
  startCashOut(request: {
    requestKey: string;
    amountMinorUnits: number;
  }): Promise<unknown>;
  cashOutHistory(): Promise<unknown>;
  sellable(): Promise<unknown>;
  quoteSale(
    request: c.MoneyHolding & { sourceUnitAmount: number },
  ): Promise<unknown>;
  sell(request: { requestKey: string; quoteId: string }): Promise<unknown>;
}

async function result<T, U>(
  call: () => Promise<unknown>,
  schema: z.ZodType<T>,
  map: (value: T) => U,
): Promise<MoneyResult<U>> {
  try {
    return { ok: true, value: map(schema.parse(await call())) };
  } catch (error) {
    const known = moneyCodeOf(error);
    const code: MoneyRefusalCode =
      known && known !== "finished" ? known : "unconfirmed";
    return {
      ok: false,
      refusal: {
        code,
        message:
          code === "unconfirmed"
            ? "We could not confirm the result. The request stays saved; check again before starting another."
            : refusalText[code],
      },
    };
  }
}

const refusalText: Record<MoneyRefusalCode, string> = {
  setup_required: "Finish account setup first.",
  under_review: "Your account is awaiting review.",
  consent_revoked: "Payment permission needs your approval.",
  account_holder_required:
    "Continue account access on the secure account page.",
  kyb_required: "Business verification is required first.",
  payments_not_configured: "Payments are not available here yet.",
  rail_busy: "The payment service is busy. Your request stays saved.",
  definite:
    "This request could not continue. Resume the saved request to check its outcome.",
  unconfirmed: "The result is not confirmed.",
  needs_review: "This request needs a manual check. Its reference stays saved.",
  unauthenticated: "Sign in again to continue the saved request.",
};

export function checkoutView(
  value: z.infer<typeof c.checkoutSchema>,
): CheckoutView {
  return {
    intentId: value.paymentId,
    status: value.status,
    settlementState: value.settlementState,
    paymentUrl: value.paymentUrl ?? null,
    resumed: value.resumed,
    amountPaidMinorUnits: value.amountPaidMinorUnits ?? null,
    currency: value.currencyCode ?? null,
    expiresAt: value.expiresAt ?? null,
  };
}

function saleView(value: z.infer<typeof c.saleSchema>): SaleView {
  const refused = value.status === "FAILED" || value.status === "CANCELLED";
  return {
    id: value.id,
    status: refused ? "Refused" : value.status,
    statusMessage: refused
      ? "This sale did not go through; no sale remains pending."
      : value.status === "COMPLETED"
        ? "The proceeds are in your balance."
        : "Your sale is being confirmed.",
    quantityBaseUnits: value.quantityBaseUnits,
    proceedsMinorUnits: Number(value.ardaMinorUnits),
    currencyCode: value.currencyCode,
    currencyDecimals: value.currencyDecimals,
    requestedAt: value.requestedAt,
  };
}

/** Browser transport; every privileged operation stays behind the signed-in host. */
export function createArdaMoneyTransport(
  host: MoneyHost,
  holding?: c.MoneyHolding,
): AzoaMoneyTransport {
  return {
    account: {
      capabilities: () =>
        result(
          () => host.accountCapabilities(),
          c.accountCapabilitiesSchema,
          (value) => ({
            ready: value.ready,
            kycAvailable: value.kycAvailable,
            unavailableReason: value.unavailableReason ?? null,
          }),
        ),
      status: () =>
        result(
          () => host.accountStatus(),
          c.accountSchema,
          (value) => ({
            ...value,
            walletId: null,
            walletAddress: null,
            unavailableReason: value.unavailableReason ?? null,
            reviewDueAt: value.reviewDueAt ?? null,
          }),
        ),
      ensure: () =>
        result(
          () => host.ensureAccount(),
          c.accountSchema,
          (value) => ({
            ...value,
            walletId: null,
            walletAddress: null,
            unavailableReason: value.unavailableReason ?? null,
            reviewDueAt: value.reviewDueAt ?? null,
          }),
        ),
      beginKyc: () =>
        result(
          () => host.beginKyc(),
          c.kycSchema,
          (value) => ({
            verificationUrl: value.verificationUrl ?? null,
            expiresAt: value.expiresAt ?? null,
            instructions: value.instructions ?? null,
            developmentSimulation: value.developmentSimulation,
          }),
        ),
      revokeConsent: () =>
        result(
          () => host.revokeConsent(),
          c.revokeSchema,
          (value) => ({
            revokedAt: value.revokedAt,
          }),
        ),
    },
    checkout: {
      capabilities: () =>
        result(
          () => host.checkoutCapabilities(),
          c.capabilitiesSchema,
          (value) => value,
        ),
      begin: (request) =>
        result(
          () => host.beginCheckout(request),
          c.checkoutSchema,
          checkoutView,
        ),
      get: (id) =>
        result(() => host.getCheckout(id), c.checkoutSchema, checkoutView),
      void: (requestKey) =>
        result(
          () => host.voidCheckout(requestKey),
          c.checkoutVoidSchema,
          (value) =>
            value.outcome === "voided"
              ? { outcome: "voided" as const, intentId: null }
              : {
                  outcome: "revealed" as const,
                  intent: checkoutView(value.payment),
                },
        ),
    },
    cashOut: {
      void: (requestKey) =>
        result(
          () => host.voidCashOut(requestKey),
          c.cashOutVoidSchema,
          (value) => value,
        ),
      availability: () =>
        result(
          () => host.cashOutAvailability(),
          c.availabilitySchema,
          (value) => ({
            ...value,
            accountReady: value.destinationReady,
          }),
        ),
      destination: () =>
        result(
          () => host.cashOutDestination(),
          c.destinationSchema,
          (value) => ({
            ...value,
            accountOpened: value.opened,
            setupUrl: null,
            setupUrlExpiresAt: null,
          }),
        ),
      beginDestinationSetup: () =>
        Promise.resolve({
          ok: false,
          refusal: {
            code: "account_holder_required",
            message: refusalText.account_holder_required,
          },
        }),
      start: (request) =>
        result(
          () => host.startCashOut(request),
          c.withdrawalSchema,
          (value) => value,
        ),
      history: () =>
        result(
          () => host.cashOutHistory(),
          z.array(c.withdrawalSchema),
          (value) => value,
        ),
    },
    balance: {
      get: () =>
        result(
          () => host.cashOutAvailability(),
          c.availabilitySchema,
          (value) => ({
            availableMinorUnits: value.cashableMinorUnits,
            onHoldMinorUnits: value.onHoldMinorUnits,
            currencyCode: value.currencyCode,
            availableFrom: value.availableFrom,
          }),
        ),
    },
    ...(holding
      ? {
          sale: {
            void: (request) =>
              result(
                () => host.voidSale(request),
                c.saleVoidSchema,
                (value) =>
                  value.outcome === "voided"
                    ? { outcome: "voided" as const }
                    : {
                        outcome: "revealed" as const,
                        sale: saleView(value.sale),
                      },
              ),
            sellable: () =>
              result(
                () => host.sellable(),
                z.array(c.sellableSchema),
                (rows) => {
                  const row = rows.find(
                    (value) =>
                      value.projectTokenConfigId ===
                        holding.projectTokenConfigId &&
                      value.holderClass === holding.holderClass,
                  );
                  if (!row) throw new Error("Holding could not be read.");
                  return {
                    availableBaseUnits: String(
                      row.canSell ? row.sellableUnitCount : 0,
                    ),
                    decimals: 0,
                    unitName: row.unitName,
                    currencyCode: "ARDA",
                    currencyDecimals: 2,
                    statusMessage: row.statusMessage,
                  };
                },
              ),
            quote: (request: { quantityBaseUnits: string }) =>
              result(
                () => {
                  if (!/^[1-9]\d*$/u.test(request.quantityBaseUnits))
                    throw new Error("Whole units required.");
                  const sourceUnitAmount = Number(request.quantityBaseUnits);
                  if (
                    !Number.isSafeInteger(sourceUnitAmount) ||
                    sourceUnitAmount > 2147483647
                  )
                    throw new Error("Quantity outside the supported range.");
                  return host.quoteSale({ ...holding, sourceUnitAmount });
                },
                c.saleQuoteSchema,
                (value) => {
                  if (!value.canSell || !value.quoteId)
                    throw new Error("No sale was quoted.");
                  return {
                    quoteId: value.quoteId,
                    quantityBaseUnits: value.quantityBaseUnits,
                    proceedsMinorUnits: Number(value.platformMinorUnits),
                    currencyCode: value.currencyCode,
                    currencyDecimals: value.currencyDecimals,
                    expiresAt: value.expiresAt,
                  };
                },
              ),
            sell: (request: { requestKey: string; quoteId: string }) =>
              result(() => host.sell(request), c.saleSchema, saleView),
          },
        }
      : {}),
  };
}

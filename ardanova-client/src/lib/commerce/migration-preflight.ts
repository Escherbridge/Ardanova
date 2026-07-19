import { createHash } from "node:crypto";

export const GATED_COMMERCE_TABLES = [
  "ActorAssertionReplay",
  "EconomicOutbox",
  "EconomicSettlement",
  "FundingIntent",
  "StripeWebhookEvent",
  "TaskCommerceAgreement",
  "WalletVerificationChallenge",
] as const;

export const BASELINE_TABLES = [
  "ProjectInvestment",
  "ProjectTokenConfig",
  "Wallet",
] as const;

export type CommerceMigrationPhase =
  | "baseline"
  | "additive-source"
  | "additive";

export type CatalogColumn = {
  tableName: string;
  columnName: string;
  dataType: string;
  isNullable: string;
  udtName: string;
  columnDefault: string | null;
};

export type CatalogIndex = {
  tableName: string;
  columns: readonly string[];
  isUnique: boolean;
  isValid: boolean;
  isReady: boolean;
  predicate: string | null;
};

export type ProjectTokenConfigBackfillRow = {
  id: string;
  projectId: string;
  assetId: string | null;
  assetScale: number | null;
};

export type DuplicatePaymentIntent = {
  paymentIntentId: string;
  count: number;
};

export type PreflightObservation = {
  fingerprint: string;
  presentTables: readonly string[];
  columns: readonly CatalogColumn[];
  indexes: readonly CatalogIndex[];
  duplicatePaymentIntents: readonly DuplicatePaymentIntent[];
  projectTokenConfigBackfills: readonly ProjectTokenConfigBackfillRow[] | null;
};

export type PreflightFinding = {
  code: string;
  message: string;
};

export type PreflightReport = {
  blocked: readonly PreflightFinding[];
  warnings: readonly PreflightFinding[];
};

type DefaultRequirement =
  | { kind: "absent" }
  | { kind: "present"; tokens?: readonly string[] }
  | { kind: "ignore" };

type ColumnExpectation = {
  tableName: string;
  columnName: string;
  dataType: string;
  udtName: string;
  nullable: readonly ("YES" | "NO")[];
  defaultRequirement: DefaultRequirement;
};

type IndexExpectation = {
  tableName: string;
  columns: readonly string[];
  unique: boolean;
};

const absentDefault: DefaultRequirement = { kind: "absent" };
const nowDefault: DefaultRequirement = {
  kind: "present",
  tokens: ["now()", "current_timestamp"],
};
const numericDefault = (value: string): DefaultRequirement => ({
  kind: "present",
  tokens: [value],
});
const enumDefault = (value: string): DefaultRequirement => ({
  kind: "present",
  tokens: [value.toLowerCase()],
});

const column = (
  tableName: string,
  columnName: string,
  dataType: string,
  udtName: string,
  nullable: readonly ("YES" | "NO")[],
  defaultRequirement: DefaultRequirement = absentDefault,
): ColumnExpectation => ({
  tableName,
  columnName,
  dataType,
  udtName,
  nullable,
  defaultRequirement,
});

// Prisma String fields without a native type annotation map to PostgreSQL text.
const prismaString = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
  defaultRequirement: DefaultRequirement = absentDefault,
) =>
  column(tableName, columnName, "text", "text", nullable, defaultRequirement);
const timestamp = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
  defaultRequirement: DefaultRequirement = absentDefault,
) =>
  column(
    tableName,
    columnName,
    "timestamp without time zone",
    "timestamp",
    nullable,
    defaultRequirement,
  );
const integer = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
  defaultRequirement: DefaultRequirement = absentDefault,
) =>
  column(
    tableName,
    columnName,
    "integer",
    "int4",
    nullable,
    defaultRequirement,
  );
const boolean = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
) => column(tableName, columnName, "boolean", "bool", nullable);
const json = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
) => column(tableName, columnName, "jsonb", "jsonb", nullable);
const decimal = (
  tableName: string,
  columnName: string,
  nullable: readonly ("YES" | "NO")[],
) => column(tableName, columnName, "numeric", "numeric", nullable);
const enumColumn = (
  tableName: string,
  columnName: string,
  enumName: string,
  defaultRequirement: DefaultRequirement,
) =>
  column(
    tableName,
    columnName,
    "USER-DEFINED",
    enumName,
    ["NO"],
    defaultRequirement,
  );

// These columns existed before gated commerce. A unique payment-intent index is additive.
export const REQUIRED_BASELINE_COLUMNS: readonly ColumnExpectation[] = [
  prismaString("ProjectInvestment", "stripePaymentIntentId", ["YES"]),
  prismaString("ProjectTokenConfig", "assetId", ["YES"]),
];

// `assetScale` may be nullable while authoritative values are backfilled, or non-null only
// after that reviewed backfill. It must never have a default: a default is not evidence.
export const REQUIRED_ADDITIVE_COLUMNS: readonly ColumnExpectation[] = [
  timestamp("Wallet", "verifiedAt", ["YES"]),
  prismaString("Wallet", "verificationChain", ["YES"]),
  prismaString("Wallet", "verificationNetwork", ["YES"]),
  prismaString("Wallet", "verificationChallengeId", ["YES"]),
  integer("ProjectTokenConfig", "assetScale", ["YES", "NO"]),

  prismaString("WalletVerificationChallenge", "id", ["NO"]),
  prismaString("WalletVerificationChallenge", "userId", ["NO"]),
  prismaString("WalletVerificationChallenge", "walletId", ["NO"]),
  prismaString("WalletVerificationChallenge", "address", ["NO"]),
  prismaString("WalletVerificationChallenge", "chain", ["NO"]),
  prismaString("WalletVerificationChallenge", "network", ["NO"]),
  prismaString("WalletVerificationChallenge", "nonceHash", ["NO"]),
  timestamp("WalletVerificationChallenge", "issuedAt", ["NO"], nowDefault),
  timestamp("WalletVerificationChallenge", "expiresAt", ["NO"]),
  timestamp("WalletVerificationChallenge", "consumedAt", ["YES"]),
  boolean("WalletVerificationChallenge", "proofVerified", ["YES"]),
  prismaString("WalletVerificationChallenge", "signatureHash", ["YES"]),
  prismaString("WalletVerificationChallenge", "failureCode", ["YES"]),

  prismaString("ActorAssertionReplay", "jti", ["NO"]),
  timestamp("ActorAssertionReplay", "expiresAt", ["NO"]),
  timestamp("ActorAssertionReplay", "consumedAt", ["NO"], nowDefault),
  prismaString("ActorAssertionReplay", "subject", ["NO"]),
  prismaString("ActorAssertionReplay", "requestTarget", ["NO"]),
  prismaString("ActorAssertionReplay", "bodySha256", ["NO"]),

  prismaString("StripeWebhookEvent", "id", ["NO"]),
  prismaString("StripeWebhookEvent", "eventType", ["NO"]),
  enumColumn(
    "StripeWebhookEvent",
    "status",
    "StripeWebhookEventStatus",
    enumDefault("PROCESSING"),
  ),
  integer("StripeWebhookEvent", "attemptCount", ["NO"], numericDefault("1")),
  timestamp("StripeWebhookEvent", "receivedAt", ["NO"], nowDefault),
  timestamp("StripeWebhookEvent", "processingLeaseExpiresAt", ["NO"]),
  timestamp("StripeWebhookEvent", "completedAt", ["YES"]),
  timestamp("StripeWebhookEvent", "lastFailedAt", ["YES"]),

  prismaString("EconomicSettlement", "id", ["NO"]),
  enumColumn(
    "EconomicSettlement",
    "kind",
    "EconomicSettlementKind",
    absentDefault,
  ),
  enumColumn(
    "EconomicSettlement",
    "status",
    "EconomicSettlementStatus",
    enumDefault("DRAFT"),
  ),
  prismaString("EconomicSettlement", "idempotencyKey", ["NO"]),
  prismaString("EconomicSettlement", "externalEventId", ["YES"]),
  prismaString("EconomicSettlement", "beneficiaryUserId", ["NO"]),
  prismaString("EconomicSettlement", "authorizedByUserId", ["YES"]),
  prismaString("EconomicSettlement", "projectId", ["YES"]),
  prismaString("EconomicSettlement", "taskId", ["YES"]),
  prismaString("EconomicSettlement", "escrowId", ["YES"]),
  prismaString("EconomicSettlement", "assetCode", ["NO"]),
  decimal("EconomicSettlement", "amount", ["NO"]),
  integer("EconomicSettlement", "scale", ["NO"], numericDefault("18")),
  json("EconomicSettlement", "termsSnapshot", ["YES"]),
  prismaString("EconomicSettlement", "azoaOperationId", ["YES"]),
  json("EconomicSettlement", "azoaReceipt", ["YES"]),
  boolean("EconomicSettlement", "azoaReplayed", ["YES"]),
  prismaString("EconomicSettlement", "failureCode", ["YES"]),
  prismaString("EconomicSettlement", "failureDetail", ["YES"]),
  integer("EconomicSettlement", "version", ["NO"], numericDefault("0")),
  timestamp("EconomicSettlement", "createdAt", ["NO"], nowDefault),
  timestamp("EconomicSettlement", "authorizedAt", ["YES"]),
  timestamp("EconomicSettlement", "submittedAt", ["YES"]),
  timestamp("EconomicSettlement", "confirmedAt", ["YES"]),
  timestamp("EconomicSettlement", "updatedAt", ["NO"]),

  prismaString("EconomicOutbox", "id", ["NO"]),
  prismaString("EconomicOutbox", "settlementId", ["NO"]),
  enumColumn(
    "EconomicOutbox",
    "status",
    "EconomicOutboxStatus",
    enumDefault("PENDING"),
  ),
  integer("EconomicOutbox", "payloadVersion", ["NO"], numericDefault("1")),
  integer("EconomicOutbox", "attemptCount", ["NO"], numericDefault("0")),
  timestamp("EconomicOutbox", "availableAt", ["NO"], nowDefault),
  prismaString("EconomicOutbox", "leaseToken", ["YES"]),
  timestamp("EconomicOutbox", "leaseExpiresAt", ["YES"]),
  timestamp("EconomicOutbox", "lastAttemptAt", ["YES"]),
  timestamp("EconomicOutbox", "dispatchedAt", ["YES"]),
  timestamp("EconomicOutbox", "reconciliationRequiredAt", ["YES"]),
  prismaString("EconomicOutbox", "failureCode", ["YES"]),
  prismaString("EconomicOutbox", "failureDetail", ["YES"]),
  timestamp("EconomicOutbox", "createdAt", ["NO"], nowDefault),
  timestamp("EconomicOutbox", "updatedAt", ["NO"]),

  prismaString("FundingIntent", "id", ["NO"]),
  prismaString("FundingIntent", "semanticKey", ["NO"]),
  prismaString("FundingIntent", "idempotencyKey", ["NO"]),
  enumColumn(
    "FundingIntent",
    "status",
    "FundingIntentStatus",
    enumDefault("DRAFT"),
  ),
  prismaString("FundingIntent", "funderUserId", ["NO"]),
  prismaString("FundingIntent", "projectId", ["NO"]),
  prismaString("FundingIntent", "projectTokenConfigId", ["NO"]),
  prismaString("FundingIntent", "currencyCode", ["NO"]),
  decimal("FundingIntent", "amount", ["NO"]),
  integer("FundingIntent", "scale", ["NO"], numericDefault("2")),
  prismaString("FundingIntent", "disclosureVersion", ["NO"]),
  json("FundingIntent", "eligibilitySnapshot", ["NO"]),
  json("FundingIntent", "termsSnapshot", ["NO"]),
  prismaString("FundingIntent", "termsHash", ["NO"]),
  prismaString("FundingIntent", "paymentProvider", ["YES"]),
  prismaString("FundingIntent", "providerCheckoutSessionId", ["YES"]),
  prismaString("FundingIntent", "providerPaymentIntentId", ["YES"]),
  prismaString("FundingIntent", "verifiedProviderEventId", ["YES"]),
  prismaString("FundingIntent", "settlementId", ["YES"]),
  timestamp("FundingIntent", "expiresAt", ["YES"]),
  timestamp("FundingIntent", "paymentVerifiedAt", ["YES"]),
  timestamp("FundingIntent", "settledAt", ["YES"]),
  timestamp("FundingIntent", "cancelledAt", ["YES"]),
  timestamp("FundingIntent", "createdAt", ["NO"], nowDefault),
  timestamp("FundingIntent", "updatedAt", ["NO"]),

  prismaString("TaskCommerceAgreement", "id", ["NO"]),
  prismaString("TaskCommerceAgreement", "semanticKey", ["NO"]),
  enumColumn(
    "TaskCommerceAgreement",
    "status",
    "TaskCommerceAgreementStatus",
    enumDefault("DRAFT"),
  ),
  prismaString("TaskCommerceAgreement", "projectId", ["NO"]),
  prismaString("TaskCommerceAgreement", "taskId", ["NO"]),
  prismaString("TaskCommerceAgreement", "bidId", ["NO"]),
  prismaString("TaskCommerceAgreement", "contributorUserId", ["NO"]),
  prismaString("TaskCommerceAgreement", "projectTokenConfigId", ["YES"]),
  prismaString("TaskCommerceAgreement", "assetCode", ["NO"]),
  decimal("TaskCommerceAgreement", "awardAmount", ["NO"]),
  integer("TaskCommerceAgreement", "scale", ["NO"], numericDefault("18")),
  json("TaskCommerceAgreement", "acceptedTermsSnapshot", ["NO"]),
  prismaString("TaskCommerceAgreement", "termsHash", ["NO"]),
  prismaString("TaskCommerceAgreement", "escrowId", ["YES"]),
  prismaString("TaskCommerceAgreement", "questRunId", ["YES"]),
  prismaString("TaskCommerceAgreement", "settlementId", ["YES"]),
  timestamp("TaskCommerceAgreement", "acceptedAt", ["YES"]),
  timestamp("TaskCommerceAgreement", "releaseAuthorizedAt", ["YES"]),
  timestamp("TaskCommerceAgreement", "settledAt", ["YES"]),
  timestamp("TaskCommerceAgreement", "cancelledAt", ["YES"]),
  timestamp("TaskCommerceAgreement", "createdAt", ["NO"], nowDefault),
  timestamp("TaskCommerceAgreement", "updatedAt", ["NO"]),
];

export const REQUIRED_ADDITIVE_INDEXES: readonly IndexExpectation[] = [
  {
    tableName: "ProjectInvestment",
    columns: ["stripePaymentIntentId"],
    unique: true,
  },
  {
    tableName: "WalletVerificationChallenge",
    columns: ["walletId", "consumedAt"],
    unique: false,
  },
  {
    tableName: "WalletVerificationChallenge",
    columns: ["userId", "expiresAt"],
    unique: false,
  },
  {
    tableName: "ActorAssertionReplay",
    columns: ["expiresAt", "jti"],
    unique: false,
  },
  {
    tableName: "StripeWebhookEvent",
    columns: ["status", "processingLeaseExpiresAt"],
    unique: false,
  },
  {
    tableName: "EconomicSettlement",
    columns: ["idempotencyKey"],
    unique: true,
  },
  {
    tableName: "EconomicSettlement",
    columns: ["externalEventId"],
    unique: true,
  },
  {
    tableName: "EconomicSettlement",
    columns: ["beneficiaryUserId", "status"],
    unique: false,
  },
  {
    tableName: "EconomicSettlement",
    columns: ["projectId", "status"],
    unique: false,
  },
  {
    tableName: "EconomicSettlement",
    columns: ["taskId", "kind"],
    unique: false,
  },
  { tableName: "EconomicOutbox", columns: ["settlementId"], unique: true },
  {
    tableName: "EconomicOutbox",
    columns: ["status", "availableAt"],
    unique: false,
  },
  { tableName: "EconomicOutbox", columns: ["leaseExpiresAt"], unique: false },
  { tableName: "FundingIntent", columns: ["semanticKey"], unique: true },
  {
    tableName: "FundingIntent",
    columns: ["providerCheckoutSessionId"],
    unique: true,
  },
  {
    tableName: "FundingIntent",
    columns: ["providerPaymentIntentId"],
    unique: true,
  },
  {
    tableName: "FundingIntent",
    columns: ["verifiedProviderEventId"],
    unique: true,
  },
  { tableName: "FundingIntent", columns: ["settlementId"], unique: true },
  {
    tableName: "FundingIntent",
    columns: ["funderUserId", "idempotencyKey"],
    unique: true,
  },
  {
    tableName: "FundingIntent",
    columns: ["funderUserId", "status"],
    unique: false,
  },
  {
    tableName: "FundingIntent",
    columns: ["projectId", "status"],
    unique: false,
  },
  {
    tableName: "FundingIntent",
    columns: ["projectTokenConfigId", "status"],
    unique: false,
  },
  {
    tableName: "TaskCommerceAgreement",
    columns: ["semanticKey"],
    unique: true,
  },
  { tableName: "TaskCommerceAgreement", columns: ["taskId"], unique: true },
  { tableName: "TaskCommerceAgreement", columns: ["bidId"], unique: true },
  { tableName: "TaskCommerceAgreement", columns: ["escrowId"], unique: true },
  {
    tableName: "TaskCommerceAgreement",
    columns: ["settlementId"],
    unique: true,
  },
  {
    tableName: "TaskCommerceAgreement",
    columns: ["contributorUserId", "status"],
    unique: false,
  },
  {
    tableName: "TaskCommerceAgreement",
    columns: ["projectId", "status"],
    unique: false,
  },
  {
    tableName: "TaskCommerceAgreement",
    columns: ["questRunId"],
    unique: false,
  },
];

export function fingerprintCatalog(
  tables: readonly string[],
  columns: readonly CatalogColumn[],
  indexes: readonly CatalogIndex[],
): string {
  const canonical = JSON.stringify({
    columns: [...columns]
      .map((column) => ({
        ...column,
        columnDefault: column.columnDefault ?? null,
      }))
      .sort((left, right) =>
        `${left.tableName}.${left.columnName}`.localeCompare(
          `${right.tableName}.${right.columnName}`,
        ),
      ),
    indexes: [...indexes]
      .map((index) => ({
        ...index,
        columns: [...index.columns],
        predicate: index.predicate ?? null,
      }))
      .sort((left, right) =>
        `${left.tableName}.${left.columns.join(",")}`.localeCompare(
          `${right.tableName}.${right.columns.join(",")}`,
        ),
      ),
    tables: [...tables].sort(),
  });
  return createHash("sha256").update(canonical).digest("hex");
}

export function evaluateCommerceMigrationPreflight(
  phase: CommerceMigrationPhase,
  expectedFingerprint: string,
  observation: PreflightObservation,
): PreflightReport {
  const blocked: PreflightFinding[] = [];
  const warnings: PreflightFinding[] = [];
  const presentTables = new Set(observation.presentTables);

  if (observation.fingerprint !== expectedFingerprint) {
    blocked.push({
      code: "baseline-fingerprint-mismatch",
      message:
        "The inspected migration surface does not match the operator-approved baseline fingerprint.",
    });
  }

  for (const table of BASELINE_TABLES) {
    if (!presentTables.has(table))
      blocked.push({
        code: "missing-baseline-table",
        message: `Required baseline table ${table} is absent.`,
      });
  }
  validateColumns(REQUIRED_BASELINE_COLUMNS, observation.columns, blocked);

  const targetTablesPresent = GATED_COMMERCE_TABLES.filter((table) =>
    presentTables.has(table),
  );
  const additiveColumnsPresent = REQUIRED_ADDITIVE_COLUMNS.filter((expected) =>
    observation.columns.some(
      (column) =>
        column.tableName === expected.tableName &&
        column.columnName === expected.columnName,
    ),
  );
  const paymentIntentUnique = hasExactIndex(
    observation.indexes,
    REQUIRED_ADDITIVE_INDEXES[0],
  );

  if (phase === "baseline") {
    if (targetTablesPresent.length > 0)
      blocked.push({
        code: "additive-tables-already-present",
        message: `Baseline phase requires all gated-commerce tables to be absent; found ${targetTablesPresent.join(", ")}.`,
      });
    if (additiveColumnsPresent.length > 0)
      blocked.push({
        code: "additive-columns-already-present",
        message: `Baseline phase requires gated-commerce columns to be absent; found ${additiveColumnsPresent.map((column) => `${column.tableName}.${column.columnName}`).join(", ")}.`,
      });
    if (paymentIntentUnique)
      blocked.push({
        code: "payment-intent-unique-index-already-present",
        message:
          "Baseline phase requires ProjectInvestment.stripePaymentIntentId to remain non-unique until the reviewed additive index migration.",
      });
  } else {
    const missingTables = GATED_COMMERCE_TABLES.filter(
      (table) => !presentTables.has(table),
    );
    if (missingTables.length > 0)
      blocked.push({
        code: "missing-additive-tables",
        message: `Additive phase requires all gated-commerce tables; missing ${missingTables.join(", ")}.`,
      });
    validateColumns(REQUIRED_ADDITIVE_COLUMNS, observation.columns, blocked);
    const requiredIndexes =
      phase === "additive-source"
        ? REQUIRED_ADDITIVE_INDEXES.filter(
            (index) => index.tableName !== "ActorAssertionReplay",
          )
        : REQUIRED_ADDITIVE_INDEXES;
    validateIndexes(requiredIndexes, observation.indexes, blocked);
  }

  const paymentIntentColumnPresent = observation.columns.some(
    (column) =>
      column.tableName === "ProjectInvestment" &&
      column.columnName === "stripePaymentIntentId",
  );
  if (
    paymentIntentColumnPresent &&
    observation.duplicatePaymentIntents.length > 0
  ) {
    blocked.push({
      code: "duplicate-payment-intents",
      message:
        "Existing non-null Stripe payment-intent ids are duplicated; reconcile them before adding or relying on the unique constraint.",
    });
  }

  const assetScale = observation.columns.find(
    (column) =>
      column.tableName === "ProjectTokenConfig" &&
      column.columnName === "assetScale",
  );
  if (assetScale && hasDefault(assetScale)) {
    blocked.push({
      code: "unsafe-asset-scale-default",
      message:
        "ProjectTokenConfig.assetScale has a database default. A default cannot be used as evidence for historical asset-scale backfill.",
    });
  }
  if (assetScale) {
    for (const config of observation.projectTokenConfigBackfills ?? []) {
      if (config.assetScale === null)
        blocked.push({
          code: "asset-scale-backfill-required",
          message: `ProjectTokenConfig ${config.id} has no canonical assetScale.`,
        });
      if (config.assetId === null)
        blocked.push({
          code: "asset-id-chain-backfill-required",
          message: `ProjectTokenConfig ${config.id} has no on-chain assetId; obtain it from the authoritative chain record before enabling settlement.`,
        });
    }
  } else {
    warnings.push({
      code: "asset-backfill-not-yet-queryable",
      message:
        "ProjectTokenConfig.assetScale is absent, so canonical scale backfill cannot be evaluated until the reviewed additive schema stage.",
    });
  }

  return { blocked, warnings };
}

function validateColumns(
  expectedColumns: readonly ColumnExpectation[],
  columns: readonly CatalogColumn[],
  blocked: PreflightFinding[],
): void {
  const actualByName = new Map(
    columns.map((column) => [
      `${column.tableName}.${column.columnName}`,
      column,
    ]),
  );
  for (const expected of expectedColumns) {
    const name = `${expected.tableName}.${expected.columnName}`;
    const actual = actualByName.get(name);
    if (!actual) {
      blocked.push({
        code: "missing-required-column",
        message: `Required column ${name} is absent.`,
      });
      continue;
    }
    if (
      actual.dataType !== expected.dataType ||
      actual.udtName !== expected.udtName
    )
      blocked.push({
        code: "column-type-mismatch",
        message: `Column ${name} must be ${expected.dataType}/${expected.udtName}, found ${actual.dataType}/${actual.udtName}.`,
      });
    if (!expected.nullable.includes(actual.isNullable as "YES" | "NO"))
      blocked.push({
        code: "column-nullability-mismatch",
        message: `Column ${name} has unexpected nullability ${actual.isNullable}.`,
      });
    if (!matchesDefault(actual, expected.defaultRequirement))
      blocked.push({
        code: "column-default-mismatch",
        message: `Column ${name} has an unsafe or unexpected database default.`,
      });
  }
}

function validateIndexes(
  expectedIndexes: readonly IndexExpectation[],
  indexes: readonly CatalogIndex[],
  blocked: PreflightFinding[],
): void {
  for (const expected of expectedIndexes) {
    if (!hasExactIndex(indexes, expected))
      blocked.push({
        code: "missing-or-unsafe-index",
        message: `Required ${expected.unique ? "unique " : ""}index on ${expected.tableName}(${expected.columns.join(", ")}) is missing, partial, invalid, unready, or has the wrong key order.`,
      });
  }
}

function hasExactIndex(
  indexes: readonly CatalogIndex[],
  expected: IndexExpectation,
): boolean {
  return indexes.some(
    (index) =>
      index.tableName === expected.tableName &&
      index.isUnique === expected.unique &&
      index.isValid &&
      index.isReady &&
      !index.predicate &&
      index.columns.length === expected.columns.length &&
      index.columns.every(
        (column, indexPosition) => column === expected.columns[indexPosition],
      ),
  );
}

function hasDefault(column: CatalogColumn): boolean {
  return String.prototype.trim.call(column.columnDefault ?? "") !== "";
}

function matchesDefault(
  column: CatalogColumn,
  requirement: DefaultRequirement,
): boolean {
  if (requirement.kind === "ignore") return true;
  if (requirement.kind === "absent") return !hasDefault(column);
  if (!hasDefault(column)) return false;
  if (!requirement.tokens?.length) return true;
  const normalized = column.columnDefault!.replace(/\s+/g, "").toLowerCase();
  return requirement.tokens.some((token) =>
    normalized.includes(token.replace(/\s+/g, "").toLowerCase()),
  );
}

export function maskPaymentIntentId(value: string): string {
  const suffix = value.slice(-6);
  return `${value.slice(0, Math.min(3, value.length))}…${suffix}`;
}

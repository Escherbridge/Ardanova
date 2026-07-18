import { describe, expect, it } from "vitest";

import {
  BASELINE_TABLES,
  evaluateCommerceMigrationPreflight,
  fingerprintCatalog,
  GATED_COMMERCE_TABLES,
  REQUIRED_ADDITIVE_COLUMNS,
  REQUIRED_ADDITIVE_INDEXES,
  REQUIRED_BASELINE_COLUMNS,
  type CatalogColumn,
  type CatalogIndex,
  type PreflightObservation,
} from "./migration-preflight";

function defaultFor(
  expected: (typeof REQUIRED_ADDITIVE_COLUMNS)[number],
): string | null {
  if (expected.defaultRequirement.kind === "absent") return null;
  if (expected.defaultRequirement.kind === "ignore") return null;
  const token = expected.defaultRequirement.tokens?.[0] ?? "default";
  return token === "now()" ? "CURRENT_TIMESTAMP" : token;
}

function columnsFor(
  expectedColumns: readonly (typeof REQUIRED_ADDITIVE_COLUMNS)[number][],
): CatalogColumn[] {
  return expectedColumns.map((expected) => ({
    tableName: expected.tableName,
    columnName: expected.columnName,
    dataType: expected.dataType,
    udtName: expected.udtName,
    isNullable: expected.nullable[0],
    columnDefault: defaultFor(expected),
  }));
}

function indexesFor(
  expectedIndexes = REQUIRED_ADDITIVE_INDEXES,
): CatalogIndex[] {
  return expectedIndexes.map((expected) => ({
    tableName: expected.tableName,
    columns: [...expected.columns],
    isUnique: expected.unique,
    isValid: true,
    isReady: true,
    predicate: null,
  }));
}

function observation(
  overrides: Partial<PreflightObservation> = {},
): PreflightObservation {
  const presentTables = overrides.presentTables ?? [...BASELINE_TABLES];
  const columns = overrides.columns ?? columnsFor(REQUIRED_BASELINE_COLUMNS);
  const indexes = overrides.indexes ?? [];
  return {
    fingerprint:
      overrides.fingerprint ??
      fingerprintCatalog(presentTables, columns, indexes),
    presentTables,
    columns,
    indexes,
    duplicatePaymentIntents: overrides.duplicatePaymentIntents ?? [],
    projectTokenConfigBackfills: overrides.projectTokenConfigBackfills ?? null,
  };
}

function additiveObservation(
  overrides: Partial<PreflightObservation> = {},
): PreflightObservation {
  const presentTables = overrides.presentTables ?? [
    ...BASELINE_TABLES,
    ...GATED_COMMERCE_TABLES,
  ];
  const columns =
    overrides.columns ??
    columnsFor([
      ...REQUIRED_BASELINE_COLUMNS,
      ...REQUIRED_ADDITIVE_COLUMNS,
    ] as typeof REQUIRED_ADDITIVE_COLUMNS);
  const indexes = overrides.indexes ?? indexesFor();
  return observation({ ...overrides, presentTables, columns, indexes });
}

describe("commerce migration preflight", () => {
  it("accepts the reviewed pre-additive baseline with the existing payment-intent column", () => {
    const actual = observation();
    const report = evaluateCommerceMigrationPreflight(
      "baseline",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked).toEqual([]);
  });

  it("rejects a missing baseline payment-intent column and an early unique index", () => {
    const missingColumn = observation({
      columns: columnsFor([
        REQUIRED_BASELINE_COLUMNS[1],
      ] as typeof REQUIRED_ADDITIVE_COLUMNS),
    });
    const missingReport = evaluateCommerceMigrationPreflight(
      "baseline",
      missingColumn.fingerprint,
      missingColumn,
    );
    expect(missingReport.blocked.map((finding) => finding.code)).toContain(
      "missing-required-column",
    );

    const earlyUnique = observation({
      indexes: indexesFor([REQUIRED_ADDITIVE_INDEXES[0]]),
    });
    const uniqueReport = evaluateCommerceMigrationPreflight(
      "baseline",
      earlyUnique.fingerprint,
      earlyUnique,
    );
    expect(uniqueReport.blocked.map((finding) => finding.code)).toContain(
      "payment-intent-unique-index-already-present",
    );
  });

  it("accepts a complete additive schema with exact valid non-partial indexes", () => {
    const actual = additiveObservation();
    const report = evaluateCommerceMigrationPreflight(
      "additive",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked).toEqual([]);
  });

  it("accepts the established additive source before the replay index upgrade", () => {
    const indexes = indexesFor().map((index) =>
      index.tableName === "ActorAssertionReplay"
        ? { ...index, columns: ["expiresAt"] }
        : index,
    );
    const actual = additiveObservation({ indexes });
    const report = evaluateCommerceMigrationPreflight(
      "additive-source",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked).toEqual([]);
  });

  it("rejects the legacy single-column actor replay cleanup index", () => {
    const indexes = indexesFor().map((index) =>
      index.tableName === "ActorAssertionReplay"
        ? { ...index, columns: ["expiresAt"] }
        : index,
    );
    const actual = additiveObservation({ indexes });
    const report = evaluateCommerceMigrationPreflight(
      "additive",
      actual.fingerprint,
      actual,
    );

    const finding = report.blocked.find(
      (candidate) =>
        candidate.code === "missing-or-unsafe-index" &&
        candidate.message.includes("ActorAssertionReplay"),
    );
    expect(finding?.message).toContain("ActorAssertionReplay(expiresAt, jti)");
  });

  it("rejects missing or malformed target columns and partial payment-intent uniqueness", () => {
    const columns = columnsFor([
      ...REQUIRED_BASELINE_COLUMNS,
      ...REQUIRED_ADDITIVE_COLUMNS,
    ] as typeof REQUIRED_ADDITIVE_COLUMNS)
      .filter(
        (column) =>
          !(
            column.tableName === "Wallet" &&
            column.columnName === "verificationNetwork"
          ),
      )
      .map((column) =>
        column.tableName === "FundingIntent" && column.columnName === "amount"
          ? { ...column, dataType: "double precision" }
          : column,
      );
    const indexes = indexesFor().map((index) =>
      index.tableName === "ProjectInvestment"
        ? { ...index, predicate: '"stripePaymentIntentId" IS NOT NULL' }
        : index,
    );
    const actual = additiveObservation({ columns, indexes });
    const report = evaluateCommerceMigrationPreflight(
      "additive",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked.map((finding) => finding.code)).toEqual(
      expect.arrayContaining([
        "missing-required-column",
        "column-type-mismatch",
        "missing-or-unsafe-index",
      ]),
    );
  });

  it("rejects an asset-scale default rather than accepting it as historical backfill", () => {
    const columns = columnsFor([
      ...REQUIRED_BASELINE_COLUMNS,
      ...REQUIRED_ADDITIVE_COLUMNS,
    ] as typeof REQUIRED_ADDITIVE_COLUMNS).map((column) =>
      column.tableName === "ProjectTokenConfig" &&
      column.columnName === "assetScale"
        ? { ...column, isNullable: "NO", columnDefault: "6" }
        : column,
    );
    const actual = additiveObservation({ columns });
    const report = evaluateCommerceMigrationPreflight(
      "additive",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked.map((finding) => finding.code)).toEqual(
      expect.arrayContaining([
        "column-default-mismatch",
        "unsafe-asset-scale-default",
      ]),
    );
  });

  it("blocks settlement readiness when an authoritative asset backfill is missing", () => {
    const actual = additiveObservation({
      projectTokenConfigBackfills: [
        {
          id: "token-config-1",
          projectId: "project-1",
          assetId: null,
          assetScale: null,
        },
      ],
    });
    const report = evaluateCommerceMigrationPreflight(
      "additive",
      actual.fingerprint,
      actual,
    );

    expect(report.blocked.map((finding) => finding.code)).toEqual(
      expect.arrayContaining([
        "asset-scale-backfill-required",
        "asset-id-chain-backfill-required",
      ]),
    );
  });
});

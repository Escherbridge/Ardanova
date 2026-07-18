import { Prisma, PrismaClient } from "@prisma/client";

import {
  BASELINE_TABLES,
  evaluateCommerceMigrationPreflight,
  fingerprintCatalog,
  GATED_COMMERCE_TABLES,
  maskPaymentIntentId,
  type CatalogColumn,
  type CommerceMigrationPhase,
  type DuplicatePaymentIntent,
  type ProjectTokenConfigBackfillRow,
} from "../src/lib/commerce/migration-preflight";

type TableRow = { table_name: string };
type ColumnRow = {
  table_name: string;
  column_name: string;
  data_type: string;
  is_nullable: string;
  udt_name: string;
  column_default: string | null;
};
type IndexRow = {
  table_name: string;
  key_columns: string | null;
  is_unique: boolean;
  is_valid: boolean;
  is_ready: boolean;
  predicate: string | null;
};
type DuplicatePaymentIntentRow = { payment_intent_id: string; count: bigint };
type TokenConfigRow = {
  id: string;
  project_id: string;
  asset_id: string | null;
  asset_scale: number | null;
};

class PreflightConfigurationError extends Error {}

function parsePhase(): CommerceMigrationPhase {
  const phaseIndex = process.argv.indexOf("--phase");
  const fromArgs = phaseIndex >= 0 ? process.argv[phaseIndex + 1] : undefined;
  const phase = fromArgs ?? process.env.MIGRATION_PREFLIGHT_PHASE;
  if (
    phase !== "baseline" &&
    phase !== "additive-source" &&
    phase !== "additive"
  ) {
    throw new PreflightConfigurationError(
      "Set --phase baseline|additive-source|additive (or MIGRATION_PREFLIGHT_PHASE) explicitly.",
    );
  }
  return phase;
}

function requiredFingerprint(): string {
  const fingerprint = process.env.MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT;
  if (!fingerprint || !/^[a-f0-9]{64}$/i.test(fingerprint)) {
    throw new PreflightConfigurationError(
      "Set MIGRATION_PREFLIGHT_EXPECTED_FINGERPRINT to the reviewed 64-character SHA-256 baseline fingerprint.",
    );
  }
  return fingerprint.toLowerCase();
}

function requireDatabaseUrl(): void {
  if (!process.env.DATABASE_URL) {
    throw new PreflightConfigurationError(
      "DATABASE_URL is required. Use a dedicated database role limited to SELECT.",
    );
  }
}

function safeErrorCode(error: unknown): string {
  if (error instanceof PreflightConfigurationError)
    return "configuration-required";
  if (error instanceof Prisma.PrismaClientKnownRequestError) return error.code;
  if (error instanceof Prisma.PrismaClientInitializationError)
    return "connection-initialization-failed";
  return "unexpected-preflight-error";
}

function safeErrorDetail(error: unknown): string | null {
  return error instanceof PreflightConfigurationError ? error.message : null;
}

async function run(): Promise<void> {
  requireDatabaseUrl();
  const phase = parsePhase();
  const expectedFingerprint = requiredFingerprint();
  const tableNames = [...BASELINE_TABLES, ...GATED_COMMERCE_TABLES];
  const prisma = new PrismaClient({ log: [] });

  try {
    await prisma.$queryRaw(Prisma.sql`SELECT 1`);
    const tables = await prisma.$queryRaw<TableRow[]>(Prisma.sql`
      SELECT table_name
      FROM information_schema.tables
      WHERE table_schema = 'public'
        AND table_type = 'BASE TABLE'
        AND table_name IN (${Prisma.join(tableNames)})
      ORDER BY table_name
    `);
    const columns = await prisma.$queryRaw<ColumnRow[]>(Prisma.sql`
      SELECT table_name, column_name, data_type, is_nullable, udt_name, column_default
      FROM information_schema.columns
      WHERE table_schema = 'public'
        AND table_name IN (${Prisma.join(tableNames)})
      ORDER BY table_name, column_name
    `);
    const indexes = await prisma.$queryRaw<IndexRow[]>(Prisma.sql`
      SELECT
        table_rel.relname AS table_name,
        array_to_string(
          ARRAY(
            SELECT attribute.attname
            FROM unnest(index_rel_data.indkey) WITH ORDINALITY AS key_column(attnum, ordinal)
            JOIN pg_attribute AS attribute
              ON attribute.attrelid = table_rel.oid
             AND attribute.attnum = key_column.attnum
            WHERE key_column.ordinal <= index_rel_data.indnkeyatts
            ORDER BY key_column.ordinal
          ),
          ','
        ) AS key_columns,
        index_rel_data.indisunique AS is_unique,
        index_rel_data.indisvalid AS is_valid,
        index_rel_data.indisready AS is_ready,
        pg_get_expr(index_rel_data.indpred, index_rel_data.indrelid) AS predicate
      FROM pg_index AS index_rel_data
      JOIN pg_class AS table_rel ON table_rel.oid = index_rel_data.indrelid
      JOIN pg_namespace AS namespace ON namespace.oid = table_rel.relnamespace
      WHERE namespace.nspname = 'public'
        AND table_rel.relname IN (${Prisma.join(tableNames)})
    `);

    const normalizedColumns: CatalogColumn[] = columns.map((column) => ({
      tableName: column.table_name,
      columnName: column.column_name,
      dataType: column.data_type,
      isNullable: column.is_nullable,
      udtName: column.udt_name,
      columnDefault: column.column_default,
    }));
    const presentTables = tables.map((table) => table.table_name);
    const hasPaymentIntentColumn = normalizedColumns.some(
      (column) =>
        column.tableName === "ProjectInvestment" &&
        column.columnName === "stripePaymentIntentId",
    );
    const hasAssetScaleColumn = normalizedColumns.some(
      (column) =>
        column.tableName === "ProjectTokenConfig" &&
        column.columnName === "assetScale",
    );
    const duplicatePaymentIntents = hasPaymentIntentColumn
      ? await prisma.$queryRaw<DuplicatePaymentIntentRow[]>(Prisma.sql`
          SELECT "stripePaymentIntentId" AS payment_intent_id, COUNT(*)::bigint AS count
          FROM public."ProjectInvestment"
          WHERE "stripePaymentIntentId" IS NOT NULL
          GROUP BY "stripePaymentIntentId"
          HAVING COUNT(*) > 1
          ORDER BY "stripePaymentIntentId"
          LIMIT 50
        `)
      : [];
    const tokenConfigBackfills = hasAssetScaleColumn
      ? await prisma.$queryRaw<TokenConfigRow[]>(Prisma.sql`
          SELECT id, "projectId" AS project_id, "assetId" AS asset_id, "assetScale" AS asset_scale
          FROM public."ProjectTokenConfig"
          WHERE "assetScale" IS NULL OR "assetId" IS NULL
          ORDER BY id
          LIMIT 100
        `)
      : await prisma.$queryRaw<TokenConfigRow[]>(Prisma.sql`
          SELECT id, "projectId" AS project_id, "assetId" AS asset_id, NULL::integer AS asset_scale
          FROM public."ProjectTokenConfig"
          WHERE "assetId" IS NULL
          ORDER BY id
          LIMIT 100
        `);
    const normalizedIndexes = indexes.map((index) => ({
      tableName: index.table_name,
      columns: index.key_columns ? index.key_columns.split(",") : [],
      isUnique: index.is_unique,
      isValid: index.is_valid,
      isReady: index.is_ready,
      predicate: index.predicate,
    }));
    const fingerprint = fingerprintCatalog(
      presentTables,
      normalizedColumns,
      normalizedIndexes,
    );
    const report = evaluateCommerceMigrationPreflight(
      phase,
      expectedFingerprint,
      {
        fingerprint,
        presentTables,
        columns: normalizedColumns,
        indexes: normalizedIndexes,
        duplicatePaymentIntents: duplicatePaymentIntents.map(
          (duplicate): DuplicatePaymentIntent => ({
            paymentIntentId: duplicate.payment_intent_id,
            count: Number(duplicate.count),
          }),
        ),
        projectTokenConfigBackfills: tokenConfigBackfills?.map(
          (config): ProjectTokenConfigBackfillRow => ({
            id: config.id,
            projectId: config.project_id,
            assetId: config.asset_id,
            assetScale: config.asset_scale,
          }),
        ),
      },
    );

    console.log(`commerce migration preflight phase: ${phase}`);
    console.log(`inspected fingerprint: ${fingerprint}`);
    console.log(`expected fingerprint: ${expectedFingerprint}`);
    for (const duplicate of duplicatePaymentIntents) {
      console.log(
        `duplicate Stripe payment-intent id ${maskPaymentIntentId(duplicate.payment_intent_id)}: ${duplicate.count.toString()} rows`,
      );
    }
    for (const config of tokenConfigBackfills ?? []) {
      console.log(
        `ProjectTokenConfig ${config.id} (project ${config.project_id}): assetScale=${config.asset_scale ?? "NULL"}, assetId=${config.asset_id ?? "NULL"}`,
      );
    }
    for (const warning of report.warnings)
      console.warn(`WARNING [${warning.code}] ${warning.message}`);
    for (const finding of report.blocked)
      console.error(`BLOCKED [${finding.code}] ${finding.message}`);
    if (report.blocked.length > 0) process.exitCode = 1;
  } finally {
    await prisma.$disconnect();
  }
}

run().catch((error: unknown) => {
  console.error(
    `BLOCKED [${safeErrorCode(error)}] Migration preflight could not complete safely.`,
  );
  const detail = safeErrorDetail(error);
  if (detail) console.error(detail);
  console.error(
    "See the gated-commerce migration runbook; no schema or data was changed.",
  );
  process.exitCode = 1;
});

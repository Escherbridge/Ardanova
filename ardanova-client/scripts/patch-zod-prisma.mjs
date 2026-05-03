/**
 * DBML-aware post-generate patcher for zod-prisma-types.
 *
 * Reads the DBML source of truth to identify decimal columns, then patches
 * the generated zod schemas to fix Prisma 6.x type incompatibilities.
 *
 * Issues fixed:
 *   1. DecimalJsLikeSchema — z.object() infers optional props, incompatible with Prisma.DecimalJsLike
 *   2. JsonNullValue*Schema — missing runtime `import { Prisma }` (only has type import)
 *   3. JsonNullValue*Schema — ZodEffects _input type doesn't overlap with Prisma branded types
 *
 * This script is IDEMPOTENT — safe to run multiple times on the same files.
 * It is called by: generate:prisma, generate:dbml, postinstall, and db:push.
 */

import { readFileSync, writeFileSync, existsSync } from "fs";
import { join } from "path";

const ROOT = join(import.meta.dirname, "..");
const DBML_PATH = join(ROOT, "prisma", "database-architecture.dbml");
const ZOD_INPUT_DIR = join(ROOT, "src", "lib", "zod", "inputTypeSchemas");
const ZOD_MODEL_DIR = join(ROOT, "src", "lib", "zod", "modelSchema");

// ---------------------------------------------------------------------------
// 1. Parse DBML for decimal columns
// ---------------------------------------------------------------------------

function parseDecimalFieldsFromDbml() {
  if (!existsSync(DBML_PATH)) {
    console.warn("  DBML not found at", DBML_PATH, "— skipping decimal analysis");
    return { tables: new Map(), count: 0 };
  }

  const dbml = readFileSync(DBML_PATH, "utf-8");
  const tables = new Map(); // Map<tableName, Set<fieldName>>
  let currentTable = null;
  let count = 0;

  for (const line of dbml.split("\n")) {
    const tableMatch = line.match(/^Table\s+(\w+)\s*\{/);
    if (tableMatch) {
      currentTable = tableMatch[1];
      continue;
    }

    if (line.trim() === "}") {
      currentTable = null;
      continue;
    }

    if (currentTable) {
      // Match: fieldName decimal or fieldName decimal(x,y)
      const fieldMatch = line.match(/^\s+(\w+)\s+decimal(?:\([\d,\s]+\))?/);
      if (fieldMatch) {
        if (!tables.has(currentTable)) tables.set(currentTable, new Set());
        tables.get(currentTable).add(fieldMatch[1]);
        count++;
      }
    }
  }

  return { tables, count };
}

// ---------------------------------------------------------------------------
// 2. Patch helpers
// ---------------------------------------------------------------------------

function patchFile(filepath, patchFn) {
  const filename = filepath.split(/[/\\]/).pop();
  try {
    const original = readFileSync(filepath, "utf-8");
    const patched = patchFn(original);
    if (patched !== original) {
      writeFileSync(filepath, patched, "utf-8");
      console.log(`  patched: ${filename}`);
      return true;
    } else {
      console.log(`  ok:      ${filename}`);
      return false;
    }
  } catch (e) {
    if (e.code === "ENOENT") {
      console.log(`  skip:    ${filename} (not found)`);
    } else {
      throw e;
    }
    return false;
  }
}

// ---------------------------------------------------------------------------
// 3. Fix DecimalJsLikeSchema
//    Problem: z.object({ d, e, s, toFixed }) infers optional properties,
//    but Prisma.DecimalJsLike requires them as required.
//    Fix: use `as` cast instead of inline type annotation.
// ---------------------------------------------------------------------------

function patchDecimalJsLikeSchema() {
  const filepath = join(ZOD_INPUT_DIR, "DecimalJsLikeSchema.ts");
  patchFile(filepath, (src) => {
    // Already patched? (has `as z.ZodType`)
    if (src.includes("as z.ZodType<Prisma.DecimalJsLike>")) return src;

    // Replace inline annotation with cast
    return src.replace(
      /export const DecimalJsLikeSchema:\s*z\.ZodType<Prisma\.DecimalJsLike>\s*=\s*(z\.object\(\{[\s\S]*?\}\))/,
      "export const DecimalJsLikeSchema = $1 as z.ZodType<Prisma.DecimalJsLike>"
    );
  });
}

// ---------------------------------------------------------------------------
// 4. Fix JsonNullValue*Schema files
//    Problem: `import type { Prisma }` is type-only but Prisma is used at
//    runtime. ZodEffects _input type doesn't satisfy branded Prisma types.
//    Fix: runtime import + `as unknown as` cast.
// ---------------------------------------------------------------------------

function patchJsonSchemas() {
  const files = [
    "JsonNullValueFilterSchema.ts",
    "JsonNullValueInputSchema.ts",
    "NullableJsonNullValueInputSchema.ts",
  ];

  for (const filename of files) {
    const filepath = join(ZOD_INPUT_DIR, filename);
    patchFile(filepath, (src) => {
      let patched = src;

      // Ensure runtime Prisma import (not type-only)
      if (!patched.includes("import { Prisma }")) {
        if (patched.includes("import type { Prisma }")) {
          patched = patched.replace("import type { Prisma }", "import { Prisma }");
        } else if (!patched.includes("from '@prisma/client'")) {
          patched = patched.replace(
            "import { z } from 'zod';",
            "import { z } from 'zod';\nimport { Prisma } from '@prisma/client';"
          );
        }
      }

      // Already patched cast?
      if (patched.includes("as unknown as z.ZodType<")) return patched;

      // Replace inline type annotation with `as unknown as` cast
      patched = patched.replace(
        /export const (\w+):\s*z\.ZodType<(Prisma\.\w+)>\s*=\s*([\s\S]+?);/,
        "export const $1 = $3 as unknown as z.ZodType<$2>;"
      );

      return patched;
    });
  }
}

// ---------------------------------------------------------------------------
// 5. Main
// ---------------------------------------------------------------------------

console.log("patch-zod-prisma: Analyzing DBML and patching generated schemas...");

const { tables, count } = parseDecimalFieldsFromDbml();
console.log(`  Found ${count} decimal fields across ${tables.size} tables in DBML`);

if (count > 0) {
  console.log(
    `  Tables with decimals: ${[...tables.keys()].join(", ")}`
  );
}

patchDecimalJsLikeSchema();
patchJsonSchemas();

console.log("patch-zod-prisma: Done.");

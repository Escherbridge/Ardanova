/**
 * Post-generate script to fix zod-prisma-types output for Prisma 6.x compatibility.
 *
 * Issues fixed:
 * 1. DecimalJsLikeSchema — z.object() infers optional props, incompatible with Prisma.DecimalJsLike
 * 2. JsonNullValue*Schema — missing `import { Prisma }` (uses Prisma at runtime but only has type import or none)
 * 3. JsonNullValue*Schema — ZodEffects _input type doesn't overlap with Prisma branded types
 */

import { readFileSync, writeFileSync } from "fs";
import { join } from "path";

const ZOD_DIR = join(
  import.meta.dirname,
  "..",
  "src",
  "lib",
  "zod",
  "inputTypeSchemas"
);

function patchFile(filename, patchFn) {
  const filepath = join(ZOD_DIR, filename);
  try {
    const original = readFileSync(filepath, "utf-8");
    const patched = patchFn(original);
    if (patched !== original) {
      writeFileSync(filepath, patched, "utf-8");
      console.log(`  patched: ${filename}`);
    } else {
      console.log(`  skipped (no changes): ${filename}`);
    }
  } catch (e) {
    if (e.code === "ENOENT") {
      console.log(`  skipped (not found): ${filename}`);
    } else {
      throw e;
    }
  }
}

console.log("Patching zod-prisma-types output...");

// 1. DecimalJsLikeSchema — remove inline type annotation, use `as` cast
patchFile("DecimalJsLikeSchema.ts", (src) => {
  return src.replace(
    /export const DecimalJsLikeSchema:\s*z\.ZodType<Prisma\.DecimalJsLike>\s*=\s*(z\.object\(\{[\s\S]*?\}\))/,
    "export const DecimalJsLikeSchema = $1 as z.ZodType<Prisma.DecimalJsLike>"
  );
});

// 2 & 3. Json*Schema files — add missing Prisma import and fix type cast
const jsonSchemaFiles = [
  "JsonNullValueFilterSchema.ts",
  "JsonNullValueInputSchema.ts",
  "NullableJsonNullValueInputSchema.ts",
];

for (const filename of jsonSchemaFiles) {
  patchFile(filename, (src) => {
    let patched = src;

    // Add runtime Prisma import if missing (file may have no import or only `import type`)
    if (!patched.includes("import { Prisma }")) {
      if (patched.includes("import type { Prisma }")) {
        patched = patched.replace(
          "import type { Prisma }",
          "import { Prisma }"
        );
      } else {
        // No Prisma import at all — add after zod import
        patched = patched.replace(
          "import { z } from 'zod';",
          "import { z } from 'zod';\nimport { Prisma } from '@prisma/client';"
        );
      }
    }

    // Replace inline type annotation with `as unknown as` cast
    patched = patched.replace(
      /export const (\w+):\s*z\.ZodType<(Prisma\.\w+)>\s*=\s*([\s\S]+?);/,
      "export const $1 = $3 as unknown as z.ZodType<$2>;"
    );

    return patched;
  });
}

console.log("Done.");

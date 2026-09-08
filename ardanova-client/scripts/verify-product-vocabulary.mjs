#!/usr/bin/env node
// Fails the build when blockchain machinery reaches a member-facing screen.
//
// The product test this enforces, in the owner's words: "if a non-crypto sponsor would see a
// wallet address, a mnemonic, a gas fee, a chain name, a signed challenge, or the word 'token'
// where 'funds'/'stake'/'units' would do - that is a defect, not a detail."
//
// Why a script and not a review note: the Wave 0 remediation asserted in prose that the client
// had been swept for this vocabulary. It had not. src/app/credentials/[id]/page.tsx - an
// ungated page that shows a worker their earned tier - was still rendering "Blockchain Status",
// "Minted on Algorand", "Algorand Standard Asset", "ASA #", raw transaction hashes and a raw
// creator address. A claim in prose is not a control, so this is the control.
//
// What it reads: the parsed TSX/TS of every member-facing file, and only the spans a person can
// actually see - JSX text and string/template literals in copy positions. Identifiers, imports,
// types, class names, wire values and comparisons are all excluded on purpose, because
// `isOnChain` as a field name is not a word anyone reads.
//
// What it does NOT read: operator surfaces (src/app/admin/**, which is role-gated server-side),
// server routes (src/app/api/**), tests, and generated code. Those may say what they mean.

import { readdirSync, readFileSync, statSync, existsSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import ts from "typescript";

const scriptDirectory = dirname(fileURLToPath(import.meta.url));
const defaultRoot = resolve(scriptDirectory, "..");

// ---------------------------------------------------------------------------
// What counts as a member-facing surface
// ---------------------------------------------------------------------------

/**
 * Directories, relative to the client root, whose rendered copy a member can reach.
 *
 * Deliberately the rendered surface only. `src/lib` is not scanned, because it mixes member copy
 * (`lib/commerce/on-ramp-status.ts`) with operator tooling that must say what it means
 * (`lib/commerce/migration-preflight.ts`, a database preflight run from the command line). The
 * copy modules under `src/lib` carry their own vocabulary tests instead - see
 * `on-ramp-status.test.ts` and `credentials/credential-proof.test.ts`, both of which assert the
 * same forbidden words over every string those modules can return. That split is the honest
 * boundary; it is stated here so nobody reads this guard as covering more than it does.
 */
const SCANNED_DIRECTORIES = ["src/app", "src/components"];

/**
 * Paths excluded from the scan, relative to the client root.
 *
 * `src/app/admin` is an operator surface, and that is a control rather than an assumption:
 * `src/app/admin/layout.tsx:26` is a server component that redirects any session whose role is
 * not ADMIN, so it gates the whole segment before a page renders. This exclusion is worth exactly
 * as much as that line - if it is ever weakened, this list must shrink with it.
 * `src/app/api` is HTTP, not a screen.
 */
const EXCLUDED_PATH_PREFIXES = ["src/app/admin/", "src/app/api/"];

function isExcludedFile(relativePath) {
  if (
    EXCLUDED_PATH_PREFIXES.some((prefix) => relativePath.startsWith(prefix))
  ) {
    return true;
  }
  return (
    relativePath.endsWith(".test.ts") ||
    relativePath.endsWith(".test.tsx") ||
    relativePath.endsWith(".spec.ts") ||
    relativePath.endsWith(".spec.tsx") ||
    relativePath.endsWith(".d.ts")
  );
}

// ---------------------------------------------------------------------------
// The vocabulary
// ---------------------------------------------------------------------------

/**
 * Machinery a non-crypto member must never be shown. Each entry is a regular expression source
 * matched case-insensitively against one span of visible copy, with word boundaries.
 */
const MACHINERY_WORDS = [
  "algorand",
  "solana",
  "ethereum",
  "polygon",
  "avalanche",
  "bitcoin",
  "cardano",
  "block ?chain",
  "on[- ]?chain",
  "off[- ]?chain",
  "soulbound",
  "mnemonic",
  "seed phrase",
  "recovery phrase",
  "private key",
  "public key",
  "key ?pair",
  "gas fee",
  "gas price",
  "gwei",
  "wallet",
  "smart contract",
  "testnet",
  "mainnet",
  "devnet",
  "asa",
  "nft",
  "non[- ]fungible",
  "signed challenge",
  "transaction hash",
  "tx hash",
  "txid",
  "mint",
  "mints",
  "minted",
  "minting",
  "burn",
  "burns",
  "burned",
  "web3",
  "defi",
  "dapp",
  "crypto",
  "cryptocurrency",
];

/**
 * The money words. The thesis names these directly: the word "token" where "funds", "stake" or
 * "units" would do is a defect. These are enforced exactly as hard as the machinery above -
 * there is no warning tier, because a warning tier is how the first list would have rotted.
 */
const MONEY_WORDS = ["token", "tokens", "tokenomics"];

const VOCABULARY = [
  ...MACHINERY_WORDS.map((word) => ({ word, tier: "machinery" })),
  ...MONEY_WORDS.map((word) => ({ word, tier: "money" })),
].map((entry) => ({
  ...entry,
  pattern: new RegExp("\\b" + entry.word + "\\b", "i"),
}));

// ---------------------------------------------------------------------------
// Extracting the spans a person can actually see
// ---------------------------------------------------------------------------

/** JSX attributes that carry machine values rather than copy. */
const NON_COPY_ATTRIBUTES = new Set([
  "className",
  "class",
  "key",
  "id",
  "href",
  "src",
  "srcSet",
  "type",
  "role",
  "variant",
  "size",
  "icon",
  "name",
  "htmlFor",
  "target",
  "rel",
  "method",
  "action",
  "style",
  "sortKey",
  "autoComplete",
  "inputMode",
  "pattern",
  "accept",
  "as",
  "sizes",
]);

/** Object-literal keys whose values are machine values rather than copy. */
const NON_COPY_PROPERTY_NAMES = new Set([
  "className",
  "class",
  "key",
  "id",
  "href",
  "src",
  "icon",
  "variant",
  "accent",
  "tone",
  "sortKey",
  "path",
  "route",
  "slug",
  "field",
  "column",
  "testId",
  "queryKey",
  "as",
]);

/**
 * A string that is one bare identifier-shaped token is a wire value, not copy: `"Token"` passed
 * to a translator, `"FUNDS_APPLIED"` compared against, `"neon-green"` handed to a variant. Real
 * copy that is a single word still gets caught, because JSX text is never exempted here.
 */
function looksLikeWireValue(text) {
  return /^[A-Za-z][A-Za-z0-9_.:-]*$/.test(text) && !/\s/.test(text);
}

function jsxAttributeNameOf(node) {
  const parent = node.parent;
  if (parent && ts.isJsxAttribute(parent)) {
    return parent.name.getText();
  }
  if (
    parent &&
    ts.isJsxExpression(parent) &&
    parent.parent &&
    ts.isJsxAttribute(parent.parent)
  ) {
    return parent.parent.name.getText();
  }
  return null;
}

/** Whether a string/template literal sits somewhere a person will never read it. */
function isNonCopyPosition(node) {
  const parent = node.parent;
  if (!parent) return true;

  // Module specifiers: import "~/..." / export ... from "..." / import("...")
  if (
    ts.isImportDeclaration(parent) ||
    ts.isExportDeclaration(parent) ||
    ts.isExternalModuleReference(parent) ||
    ts.isImportTypeNode(parent)
  ) {
    return true;
  }

  // Type-level literals: `type X = "wallet" | "account"`
  if (ts.isLiteralTypeNode(parent)) return true;

  // Object and enum keys, and obj["key"] access.
  if (ts.isPropertyAssignment(parent) && parent.name === node) return true;
  if (ts.isComputedPropertyName(parent)) return true;
  if (
    ts.isElementAccessExpression(parent) &&
    parent.argumentExpression === node
  )
    return true;
  if (ts.isEnumMember(parent) && parent.name === node) return true;

  // Comparisons and switch cases test wire values.
  if (ts.isBinaryExpression(parent)) {
    const operator = parent.operatorToken.kind;
    if (
      operator === ts.SyntaxKind.EqualsEqualsEqualsToken ||
      operator === ts.SyntaxKind.ExclamationEqualsEqualsToken ||
      operator === ts.SyntaxKind.EqualsEqualsToken ||
      operator === ts.SyntaxKind.ExclamationEqualsToken
    ) {
      return true;
    }
  }
  if (ts.isCaseClause(parent)) return true;

  // Attributes and object properties that carry machine values.
  const attributeName = jsxAttributeNameOf(node);
  if (attributeName !== null) {
    if (
      NON_COPY_ATTRIBUTES.has(attributeName) ||
      attributeName.startsWith("data-") ||
      attributeName.startsWith("aria-hidden")
    ) {
      return true;
    }
  }
  if (
    ts.isPropertyAssignment(parent) &&
    parent.name &&
    NON_COPY_PROPERTY_NAMES.has(parent.name.getText().replace(/["']/g, ""))
  ) {
    return true;
  }

  // Class-name helpers take class lists, never sentences.
  if (ts.isCallExpression(parent)) {
    const callee = parent.expression.getText();
    if (
      /(^|\.)(cn|clsx|cva|twMerge|encodeURIComponent|require)$/.test(callee)
    ) {
      return true;
    }
  }

  return false;
}

function collectVisibleSpans(sourceFile) {
  const spans = [];

  const record = (node, text, exemptWireValues) => {
    const trimmed = text.trim();
    if (trimmed.length === 0) return;
    if (!/[A-Za-z]/.test(trimmed)) return;
    if (exemptWireValues && looksLikeWireValue(trimmed)) return;
    const { line } = sourceFile.getLineAndCharacterOfPosition(
      node.getStart(sourceFile),
    );
    spans.push({ line: line + 1, text: trimmed });
  };

  const visit = (node) => {
    if (ts.isJsxText(node)) {
      // Everything a person reads between tags. Never exempted as a wire value.
      record(node, node.text, false);
    } else if (
      ts.isStringLiteral(node) ||
      ts.isNoSubstitutionTemplateLiteral(node)
    ) {
      if (!isNonCopyPosition(node)) record(node, node.text, true);
    } else if (ts.isTemplateExpression(node)) {
      if (!isNonCopyPosition(node)) {
        record(node, node.head.text, true);
        for (const span of node.templateSpans) {
          record(node, span.literal.text, true);
        }
      }
    }
    ts.forEachChild(node, visit);
  };

  visit(sourceFile);
  return spans;
}

// ---------------------------------------------------------------------------
// The scan
// ---------------------------------------------------------------------------

function walkDirectory(directory, files = []) {
  for (const entry of readdirSync(directory)) {
    const candidate = join(directory, entry);
    if (statSync(candidate).isDirectory()) {
      walkDirectory(candidate, files);
    } else if (candidate.endsWith(".ts") || candidate.endsWith(".tsx")) {
      files.push(candidate);
    }
  }
  return files;
}

export function scanForProductVocabulary(clientRoot) {
  const violations = [];
  let scannedFiles = 0;

  for (const scanned of SCANNED_DIRECTORIES) {
    const directory = join(clientRoot, scanned);
    if (!existsSync(directory)) continue;

    for (const file of walkDirectory(directory)) {
      const relativePath = relative(clientRoot, file).split(sep).join("/");
      if (isExcludedFile(relativePath)) continue;

      scannedFiles += 1;
      const source = readFileSync(file, "utf8");
      const sourceFile = ts.createSourceFile(
        relativePath,
        source,
        ts.ScriptTarget.Latest,
        /* setParentNodes */ true,
        relativePath.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
      );

      for (const span of collectVisibleSpans(sourceFile)) {
        for (const entry of VOCABULARY) {
          if (entry.pattern.test(span.text)) {
            violations.push({
              file: relativePath,
              line: span.line,
              word: entry.word,
              tier: entry.tier,
              copy: span.text.replace(/\s+/g, " ").slice(0, 120),
            });
          }
        }
      }
    }
  }

  return { scannedFiles, violations };
}

// ---------------------------------------------------------------------------
// CLI
// ---------------------------------------------------------------------------

function parseArguments(argv) {
  const options = { clientRoot: defaultRoot, json: false };
  for (let index = 0; index < argv.length; index += 1) {
    const flag = argv[index];
    if (flag === "--root") {
      index += 1;
      options.clientRoot = resolve(argv[index]);
    } else if (flag === "--json") {
      options.json = true;
    } else {
      throw new Error("Unrecognized argument: " + flag);
    }
  }
  return options;
}

function main(argv) {
  let options;
  try {
    options = parseArguments(argv);
  } catch (error) {
    console.error("[verify:vocabulary] " + error.message);
    return 1;
  }

  const { scannedFiles, violations } = scanForProductVocabulary(
    options.clientRoot,
  );

  if (options.json) {
    console.log(JSON.stringify({ scannedFiles, violations }, null, 2));
    return violations.length === 0 ? 0 : 1;
  }

  if (violations.length === 0) {
    console.log(
      "[verify:vocabulary] " +
        scannedFiles +
        " member-facing files contain no blockchain vocabulary.",
    );
    return 0;
  }

  console.error(
    "[verify:vocabulary] Blockchain vocabulary reached a member-facing screen in " +
      violations.length +
      " place(s):\n",
  );
  for (const violation of violations) {
    console.error(
      "  " +
        violation.file +
        ":" +
        violation.line +
        "  [" +
        violation.tier +
        ": " +
        violation.word +
        "]  " +
        violation.copy,
    );
  }
  console.error(
    "\n  A non-crypto sponsor must not be shown a chain name, a wallet, a mint, a hash or the\n" +
      "  word 'token' where 'funds', 'stake' or 'units' would do.\n" +
      "\n  To fix: translate the node's word into the member's word where the screen reads it -\n" +
      "  see src/lib/credentials/credential-proof.ts and src/lib/commerce/on-ramp-status.ts for\n" +
      "  the shape. If the surface is genuinely for operators, it belongs under src/app/admin,\n" +
      "  behind the ADMIN redirect, not behind a comment.",
  );
  return 1;
}

if (
  process.argv[1] &&
  resolve(process.argv[1]) === resolve(fileURLToPath(import.meta.url))
) {
  process.exit(main(process.argv.slice(2)));
}

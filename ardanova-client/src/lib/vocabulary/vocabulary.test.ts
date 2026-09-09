/**
 * The mechanical half of the product thesis: "a sponsor never meets the chain." A commit message
 * saying the vocabulary is gone is not a control - a grep that fails the build is. This file is
 * that grep, run as part of the normal `npm test` suite (vitest picks up any `src/**\/*.test.ts`)
 * so a future label can't reintroduce the words below without either rewording it or adding a
 * reasoned line to `allowlist.json`.
 *
 * What counts as "rendered copy", scanned under `src/app` and `src/components`:
 *   - JSX text (everything between tags a person reads)
 *   - a string literal assigned to a `label`, `title`, `placeholder`, or `aria-label` key -
 *     whether that's a literal JSX prop (`<Input label="...">`) or a plain object property in a
 *     data-driven config (`{ label: "...", icon: Wallet }`), since both render the same way to a
 *     screen.
 *   - every string leaf value in a JSON file under a `messages/`, `locales/`, or `i18n/`
 *     directory, in case this app ever grows an i18n message catalogue (it has none today, so
 *     this path is presently a no-op; it exists so the net does not have to be rebuilt later).
 * Deliberately NOT scanned: identifiers, imports, type-level string literals, object/enum keys,
 * comparisons, className-shaped helpers (cn/clsx/cva), and any other string position - a wire
 * value nobody reads is not copy. `src/lib` is not scanned; it is where this control itself lives,
 * and its money-facing modules (on-ramp-status.ts, credentials/credential-proof.ts) already carry
 * their own vocabulary tests over every string they can return.
 */

import { describe, expect, it } from "vitest";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import ts from "typescript";
import allowlistJson from "./allowlist.json";
import { CHAIN_WORDS, type ChainWord } from "./chain-words";

const HERE = dirname(fileURLToPath(import.meta.url));
/** ardanova-client/ - three levels above src/lib/vocabulary/. */
const CLIENT_ROOT = resolve(HERE, "../../..");

/** Directories whose rendered copy a person can reach. Kept in sync with SCANNED_DIRECTORIES below. */
const SCANNED_DIRECTORIES = ["src/app", "src/components"];

/** JSX/object-literal keys that carry rendered copy, per this file's own doc comment above. */
const COPY_KEY_NAMES = new Set(["label", "title", "placeholder", "aria-label"]);

/** Directory name segments (case-insensitive) that mark a JSON file as an i18n message catalogue. */
const MESSAGE_DIRECTORY_NAMES = new Set(["messages", "locales", "i18n"]);

// ---------------------------------------------------------------------------
// Allowlist shape and matching
// ---------------------------------------------------------------------------

interface PathAllowlistEntry {
  pattern: string;
  reason: string;
}

interface StringAllowlistEntry {
  word: string;
  file?: string;
  pathPrefix?: string;
  text?: string;
  reason: string;
}

interface Allowlist {
  paths: PathAllowlistEntry[];
  strings: StringAllowlistEntry[];
}

const allowlist = allowlistJson as unknown as Allowlist;

/** Converts a `paths[].pattern` glob (`**` = any depth, `*` = one path segment) into a RegExp. */
function globToRegExp(glob: string): RegExp {
  let source = "";
  for (let i = 0; i < glob.length; i += 1) {
    const char = glob[i];
    if (char === "*" && glob[i + 1] === "*") {
      source += ".*";
      i += 1;
    } else if (char === "*") {
      source += "[^/]*";
    } else if (".+^${}()|[]\\".includes(char)) {
      source += "\\" + char;
    } else {
      source += char;
    }
  }
  return new RegExp("^" + source + "$");
}

function isReasoned(reason: unknown): reason is string {
  return typeof reason === "string" && reason.trim().length > 0;
}

function isPathAllowlisted(relativeFile: string): boolean {
  return allowlist.paths.some(
    (entry) => isReasoned(entry.reason) && globToRegExp(entry.pattern).test(relativeFile),
  );
}

interface Violation {
  file: string;
  line: number;
  word: string;
  text: string;
}

function isStringAllowlisted(violation: Violation): boolean {
  return allowlist.strings.some((entry) => {
    if (!isReasoned(entry.reason) || entry.word !== violation.word) return false;
    if (entry.file && entry.file !== violation.file) return false;
    if (entry.pathPrefix && !violation.file.startsWith(entry.pathPrefix)) return false;
    if (entry.text && !violation.text.includes(entry.text)) return false;
    return Boolean(entry.file || entry.pathPrefix);
  });
}

function isAllowlisted(violation: Violation): boolean {
  return isPathAllowlisted(violation.file) || isStringAllowlisted(violation);
}

// ---------------------------------------------------------------------------
// Extracting the spans a person can actually see
// ---------------------------------------------------------------------------

/** The `label`/`title`/`placeholder`/`aria-label` key a literal sits under, JSX prop or object property. */
function copyKeyNameOf(node: ts.Node): string | null {
  const parent = node.parent;
  if (!parent) return null;
  if (ts.isJsxAttribute(parent)) return parent.name.getText();
  if (ts.isJsxExpression(parent) && parent.parent && ts.isJsxAttribute(parent.parent)) {
    return parent.parent.name.getText();
  }
  if (ts.isPropertyAssignment(parent) && parent.initializer === node) {
    return parent.name.getText().replace(/^["']|["']$/g, "");
  }
  return null;
}

interface Span {
  line: number;
  text: string;
}

function collectVisibleSpans(sourceFile: ts.SourceFile): Span[] {
  const spans: Span[] = [];
  const record = (node: ts.Node, rawText: string) => {
    const trimmed = rawText.trim();
    if (trimmed.length === 0 || !/[A-Za-z]/.test(trimmed)) return;
    const { line } = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
    spans.push({ line: line + 1, text: trimmed });
  };
  const visit = (node: ts.Node) => {
    if (ts.isJsxText(node)) {
      record(node, node.text);
    } else if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) {
      const key = copyKeyNameOf(node);
      if (key && COPY_KEY_NAMES.has(key)) record(node, node.text);
    } else if (ts.isTemplateExpression(node)) {
      const key = copyKeyNameOf(node);
      if (key && COPY_KEY_NAMES.has(key)) {
        record(node, node.head.text);
        for (const span of node.templateSpans) record(node, span.literal.text);
      }
    }
    ts.forEachChild(node, visit);
  };
  visit(sourceFile);
  return spans;
}

/** Every string leaf value in a JSON value, for an i18n message catalogue. */
function collectJsonStrings(value: unknown, out: string[] = []): string[] {
  if (typeof value === "string") {
    out.push(value);
  } else if (Array.isArray(value)) {
    for (const item of value) collectJsonStrings(item, out);
  } else if (value && typeof value === "object") {
    for (const item of Object.values(value)) collectJsonStrings(item, out);
  }
  return out;
}

function isMessageCatalogueFile(relativeFile: string): boolean {
  if (!relativeFile.endsWith(".json")) return false;
  const segments = relativeFile.split("/");
  return segments.some((segment) => MESSAGE_DIRECTORY_NAMES.has(segment.toLowerCase()));
}

// ---------------------------------------------------------------------------
// Walking the tree
// ---------------------------------------------------------------------------

function isExcludedFile(relativeFile: string): boolean {
  return (
    relativeFile.endsWith(".test.ts") ||
    relativeFile.endsWith(".test.tsx") ||
    relativeFile.endsWith(".spec.ts") ||
    relativeFile.endsWith(".spec.tsx") ||
    relativeFile.endsWith(".d.ts")
  );
}

function walkDirectory(directory: string, files: string[] = []): string[] {
  for (const entry of readdirSync(directory)) {
    const candidate = join(directory, entry);
    if (statSync(candidate).isDirectory()) {
      walkDirectory(candidate, files);
    } else if (
      candidate.endsWith(".ts") ||
      candidate.endsWith(".tsx") ||
      candidate.endsWith(".json")
    ) {
      files.push(candidate);
    }
  }
  return files;
}

function wordsMatching(text: string): ChainWord[] {
  return CHAIN_WORDS.filter((word) => word.pattern.test(text));
}

function scanForChainWords(clientRoot: string, scannedDirectories: string[]) {
  const violations: Violation[] = [];
  let scannedFiles = 0;

  for (const scanned of scannedDirectories) {
    const directory = join(clientRoot, scanned);
    if (!existsSync(directory)) continue;

    for (const file of walkDirectory(directory)) {
      const relativeFile = relative(clientRoot, file).split(sep).join("/");
      if (isExcludedFile(relativeFile)) continue;
      scannedFiles += 1;

      if (relativeFile.endsWith(".json")) {
        if (!isMessageCatalogueFile(relativeFile)) continue;
        const parsed = JSON.parse(readFileSync(file, "utf8")) as unknown;
        for (const text of collectJsonStrings(parsed)) {
          for (const word of wordsMatching(text)) {
            violations.push({ file: relativeFile, line: 1, word: word.id, text: text.slice(0, 160) });
          }
        }
        continue;
      }

      const source = readFileSync(file, "utf8");
      const sourceFile = ts.createSourceFile(
        relativeFile,
        source,
        ts.ScriptTarget.Latest,
        true,
        relativeFile.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
      );
      for (const span of collectVisibleSpans(sourceFile)) {
        for (const word of wordsMatching(span.text)) {
          violations.push({
            file: relativeFile,
            line: span.line,
            word: word.id,
            text: span.text.replace(/\s+/g, " ").slice(0, 160),
          });
        }
      }
    }
  }

  return { scannedFiles, violations };
}

function formatViolations(violations: Violation[]): string {
  const lines = violations.map((v) => `  ${v.file}:${v.line}  [${v.word}]  ${v.text}`);
  return (
    `Chain vocabulary reached rendered copy in ${violations.length} place(s), outside the ` +
    `committed allowlist (src/lib/vocabulary/allowlist.json):\n\n${lines.join("\n")}\n\n` +
    `A non-crypto sponsor must not see a chain name, a wallet, a mint, a hash, or the word ` +
    `"token" where "funds"/"stake"/"units" would do. Reword the copy, or add a reasoned entry ` +
    `to allowlist.json if this specific string is genuinely a different, non-blockchain sense ` +
    `of the word.`
  );
}

// ---------------------------------------------------------------------------
// Extraction and matching self-tests
// ---------------------------------------------------------------------------

function spansOf(source: string, fileName = "fixture.tsx"): Span[] {
  const sourceFile = ts.createSourceFile(fileName, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TSX);
  return collectVisibleSpans(sourceFile);
}

describe("collectVisibleSpans", () => {
  it("reads JSX text as copy", () => {
    const spans = spansOf("const El = () => <p>Your wallet address is ready</p>");
    expect(spans.map((s) => s.text)).toContain("Your wallet address is ready");
  });

  it("reads a string literal on a label/title/placeholder/aria-label JSX prop", () => {
    const spans = spansOf('const El = () => <Input aria-label="Wallet address" placeholder="0x..." />');
    expect(spans.map((s) => s.text)).toEqual(expect.arrayContaining(["Wallet address", "0x..."]));
  });

  it("reads a string literal on a label/title key in a plain data object", () => {
    const spans = spansOf('const navGroups = [{ label: "DeFi", items: [] }]', "fixture.ts");
    expect(spans.map((s) => s.text)).toContain("DeFi");
  });

  it("does not read a className, an import path, or an identifier", () => {
    const spans = spansOf(
      [
        'import { cn } from "~/lib/utils"',
        'const wallet = "chain"',
        'const El = () => <div className={cn("wallet-chain")}>{wallet}</div>',
      ].join("\n"),
    );
    expect(spans).toHaveLength(0);
  });

  it("does not read a comment", () => {
    const spans = spansOf("// mint an NFT on-chain\nconst El = () => <p>ok</p>");
    expect(spans.map((s) => s.text)).toEqual(["ok"]);
  });
});

describe("allowlist matching", () => {
  it("every allowlist entry carries a non-empty reason", () => {
    const unreasoned = [
      ...allowlist.paths.filter((entry) => !isReasoned(entry.reason)).map((e) => `path:${e.pattern}`),
      ...allowlist.strings
        .filter((entry) => !isReasoned(entry.reason))
        .map((e) => `string:${e.word}@${e.file ?? e.pathPrefix ?? "?"}`),
    ];
    expect(unreasoned, `entries with no reason: ${unreasoned.join(", ")}`).toHaveLength(0);
  });

  it("every string entry names a file or a pathPrefix, and every path pattern is a non-empty glob", () => {
    for (const entry of allowlist.strings) {
      expect(Boolean(entry.file || entry.pathPrefix), JSON.stringify(entry)).toBe(true);
    }
    for (const entry of allowlist.paths) {
      expect(entry.pattern.length > 0, JSON.stringify(entry)).toBe(true);
    }
  });

  it("exempts a hit under an allowlisted path", () => {
    expect(
      isAllowlisted({ file: "src/app/admin/kyc/page.tsx", line: 1, word: "node", text: "Node operator tooling" }),
    ).toBe(true);
  });

  it("does not exempt a word the allowlist never named for that file", () => {
    expect(
      isAllowlisted({ file: "src/app/settings/payouts/page.tsx", line: 1, word: "wallet-address", text: "your wallet address" }),
    ).toBe(false);
  });

  it("rejects an entry with no reason, even if it would otherwise match", () => {
    const bogus: Allowlist = {
      paths: [],
      strings: [{ word: "custody", file: "src/app/settings/payouts/page.tsx", reason: "" }],
    };
    const matches = bogus.strings.some(
      (entry) => isReasoned(entry.reason) && entry.word === "custody" && entry.file === "src/app/settings/payouts/page.tsx",
    );
    expect(matches).toBe(false);
  });
});

// ---------------------------------------------------------------------------
// The gate
// ---------------------------------------------------------------------------

describe("repo-wide chain vocabulary", () => {
  it("rendered copy under src/app and src/components contains no chain word outside the allowlist", () => {
    const { scannedFiles, violations } = scanForChainWords(CLIENT_ROOT, SCANNED_DIRECTORIES);
    expect(scannedFiles).toBeGreaterThan(0);

    const unallowed = violations.filter((violation) => !isAllowlisted(violation));
    expect(unallowed, unallowed.length > 0 ? formatViolations(unallowed) : "").toHaveLength(0);
  });
});

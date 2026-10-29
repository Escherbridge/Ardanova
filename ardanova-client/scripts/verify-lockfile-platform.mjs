#!/usr/bin/env node
// Fails when package-lock.json was regenerated on a platform that drops the
// wasm32 optional bindings' nested, exact-pinned dependencies. See
// conductor/RUNBOOK.md "Client lockfile platform drift".
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { satisfies } from "./verify-azoa-sdk-pin.mjs";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const wasmBindingKey = /(^|\/)[^/]*wasm32-wasi$/;
const regenerateCommand =
  'podman run --rm -v <ardanova-client>:/app -w /app node:20-alpine sh -c "npm i -g npm@^11 && npm install --package-lock-only"';

/** Resolves `dependencyName` from the lock entry at `fromKey`, nearest node_modules first (npm's lookup order). */
export function resolveFrom(packages, fromKey, dependencyName) {
  let base = fromKey;
  for (;;) {
    const nested = `${base}/node_modules/${dependencyName}`;
    if (packages[nested]) return nested;
    const parentCut = base.lastIndexOf("/node_modules/");
    if (parentCut < 0) break;
    base = base.slice(0, parentCut);
  }
  const hoisted = `node_modules/${dependencyName}`;
  return packages[hoisted] ? hoisted : null;
}

/** Every wasm32 binding dependency that would not resolve to a satisfying lock entry under `npm ci`. */
export function collectPlatformProblems(lockfile) {
  const packages = lockfile?.packages ?? {};
  const problems = [];
  let bindingCount = 0;
  for (const [key, entry] of Object.entries(packages)) {
    if (!wasmBindingKey.test(key)) continue;
    bindingCount += 1;
    for (const [dependencyName, range] of Object.entries(entry.dependencies ?? {})) {
      const resolvedKey = resolveFrom(packages, key, dependencyName);
      if (!resolvedKey) {
        problems.push(`${key} needs ${dependencyName}@${range}, but the lockfile has no entry for it`);
        continue;
      }
      const version = packages[resolvedKey].version;
      const verdict = satisfies(version, range);
      if (verdict === null) {
        problems.push(`${key} pins ${dependencyName} as "${range}", which this check cannot verify`);
      } else if (!verdict) {
        problems.push(`${key} needs ${dependencyName}@${range}, but it resolves to ${resolvedKey}@${version}`);
      }
    }
  }
  return { problems, bindingCount };
}

export function main(argv) {
  const lockfilePath = argv[0] ?? path.resolve(scriptDirectory, "..", "package-lock.json");
  const lockfile = JSON.parse(readFileSync(lockfilePath, "utf8"));
  const { problems, bindingCount } = collectPlatformProblems(lockfile);

  if (problems.length === 0) {
    console.log(
      `[verify-lockfile-platform] ${bindingCount} wasm32 binding(s) resolve every dependency npm ci needs on Linux.`,
    );
    return 0;
  }

  console.error("[verify-lockfile-platform] package-lock.json would fail npm ci on the Linux builder:\n");
  for (const problem of problems) console.error("  - " + problem);
  console.error(
    "\nThis happens when the lock is regenerated on Windows, which never writes these nested entries.\n" +
      "Regenerate it on the builder's platform, then commit package-lock.json:\n  " +
      regenerateCommand +
      "\n",
  );
  return 1;
}

const invokedPath = process.argv[1] ?? "";
if (invokedPath.endsWith("verify-lockfile-platform.mjs")) {
  try {
    process.exit(main(process.argv.slice(2)));
  } catch (error) {
    console.error("[verify-lockfile-platform] " + error.message);
    process.exit(1);
  }
}

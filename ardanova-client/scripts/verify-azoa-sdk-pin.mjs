#!/usr/bin/env node
// Asserts that the azoa-sdk dependency tells one coherent story across
// package.json, package-lock.json and node_modules. Dependency-free on purpose:
// CI runs it BEFORE `npm ci`, when node_modules does not exist yet.
//
// Why this exists: `npm ci` aborts with an opaque EUSAGE when package.json and
// the lockfile disagree, and a machine-local `npm link` hides that disagreement
// from every local build. See conductor/RUNBOOK.md sections 1-2.

import { readFileSync, lstatSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const clientRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const PACKAGE_NAME = "azoa-sdk";
const SDK_SOURCE = "oasis-sleek/sdk/azoa-wallet";

function parseArguments(argv) {
  const options = {
    packageJsonPath: join(clientRoot, "package.json"),
    lockfilePath: join(clientRoot, "package-lock.json"),
    nodeModulesPath: join(clientRoot, "node_modules"),
    allowLinked: false,
  };
  for (let index = 0; index < argv.length; index += 1) {
    const flag = argv[index];
    if (flag === "--allow-linked") {
      options.allowLinked = true;
    } else if (flag === "--package-json") {
      index += 1;
      options.packageJsonPath = argv[index];
    } else if (flag === "--lockfile") {
      index += 1;
      options.lockfilePath = argv[index];
    } else if (flag === "--node-modules") {
      index += 1;
      options.nodeModulesPath = argv[index];
    } else {
      throw new Error("Unrecognized argument: " + flag);
    }
  }
  return options;
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

/** Parses `x.y.z` (ignoring any prerelease or build suffix) into a numeric triple. */
function parseVersion(version) {
  const match = /^(\d+)\.(\d+)\.(\d+)/.exec(String(version).trim());
  if (!match) return null;
  return [Number(match[1]), Number(match[2]), Number(match[3])];
}

function compareVersions(left, right) {
  for (let index = 0; index < 3; index += 1) {
    if (left[index] !== right[index]) {
      return left[index] < right[index] ? -1 : 1;
    }
  }
  return 0;
}

/**
 * Minimal range check for the three shapes this repo actually pins with.
 * Anything else returns `null`, which callers MUST treat as a failure: an
 * unverifiable range is not a passing range.
 */
export function satisfies(version, range) {
  const target = parseVersion(version);
  if (typeof range !== "string") return null;
  const base = parseVersion(range.replace(/^[~^]/, ""));
  if (!target || !base) return null;
  const major = base[0];
  const minor = base[1];
  const patch = base[2];
  if (range.startsWith("^")) {
    // npm caret on 0.x is minor-locked, and on 0.0.z it is patch-locked.
    // This is exactly why "^0.3.0" can never accept a published 0.4.0.
    const upper =
      major > 0
        ? [major + 1, 0, 0]
        : minor > 0
          ? [0, minor + 1, 0]
          : [0, 0, patch + 1];
    return (
      compareVersions(target, base) >= 0 && compareVersions(target, upper) < 0
    );
  }
  if (range.startsWith("~")) {
    return (
      compareVersions(target, base) >= 0 &&
      compareVersions(target, [major, minor + 1, 0]) < 0
    );
  }
  if (/^\d+\.\d+\.\d+/.test(range)) return compareVersions(target, base) === 0;
  return null;
}

export function collectPinProblems(state) {
  const declaredRange = state.declaredRange;
  const problems = [];

  if (!declaredRange) {
    problems.push(
      'package.json does not declare a "' + PACKAGE_NAME + '" dependency.',
    );
    return problems;
  }
  if (satisfies(declaredRange.replace(/^[~^]/, ""), declaredRange) === null) {
    problems.push(
      'package.json pins "' +
        PACKAGE_NAME +
        '": "' +
        declaredRange +
        '", a range shape this guard cannot verify. Failing closed rather ' +
        "than passing an unchecked pin.",
    );
  }
  if (state.lockedRange !== declaredRange) {
    problems.push(
      'package.json wants "' +
        declaredRange +
        '" but package-lock.json records "' +
        (state.lockedRange ?? "(absent)") +
        '". `npm ci` exits EUSAGE on this before it contacts the registry, so ' +
        "publishing " +
        PACKAGE_NAME +
        " alone will NOT turn CI green -- the lockfile must be regenerated too.",
    );
  }
  if (!state.lockedVersion) {
    problems.push(
      'package-lock.json has no resolved "' + PACKAGE_NAME + '" entry.',
    );
  } else if (satisfies(state.lockedVersion, declaredRange) !== true) {
    problems.push(
      "package-lock.json resolves " +
        PACKAGE_NAME +
        "@" +
        state.lockedVersion +
        ', which does not satisfy "' +
        declaredRange +
        '".',
    );
  }
  if (
    state.lockedVersion &&
    state.lockedResolved &&
    !state.lockedResolved.includes("-" + state.lockedVersion + ".tgz")
  ) {
    problems.push(
      "package-lock.json resolves " +
        PACKAGE_NAME +
        "@" +
        state.lockedVersion +
        ' from "' +
        state.lockedResolved +
        '", which is not that version\'s registry tarball.',
    );
  }
  if (state.installedIsLink && !state.allowLinked) {
    problems.push(
      "node_modules/" +
        PACKAGE_NAME +
        " is a symlink to local source. A link is machine-local and invisible " +
        "to git, so a green build here proves nothing about a clean clone. " +
        "Pass --allow-linked for local development only, never in a release gate.",
    );
  }
  if (
    state.installedVersion &&
    satisfies(state.installedVersion, declaredRange) !== true
  ) {
    problems.push(
      "node_modules/" +
        PACKAGE_NAME +
        " is version " +
        state.installedVersion +
        ', which does not satisfy the declared "' +
        declaredRange +
        '".',
    );
  }
  return problems;
}

function inspectInstalled(nodeModulesPath) {
  const packagePath = join(nodeModulesPath, PACKAGE_NAME);
  if (!existsSync(packagePath)) {
    return { installedVersion: null, installedIsLink: false };
  }
  let installedIsLink = false;
  try {
    installedIsLink = lstatSync(packagePath).isSymbolicLink();
  } catch {
    installedIsLink = false;
  }
  let installedVersion = null;
  try {
    installedVersion =
      readJson(join(packagePath, "package.json")).version ?? null;
  } catch {
    installedVersion = null;
  }
  return { installedVersion, installedIsLink };
}

export function main(argv) {
  const options = parseArguments(argv);
  const packageJson = readJson(options.packageJsonPath);
  const lockfile = readJson(options.lockfilePath);
  const declaredRange = packageJson.dependencies?.[PACKAGE_NAME] ?? null;
  const lockedEntry =
    lockfile.packages?.["node_modules/" + PACKAGE_NAME] ?? null;
  const installed = inspectInstalled(options.nodeModulesPath);

  const problems = collectPinProblems({
    declaredRange,
    lockedRange: lockfile.packages?.[""]?.dependencies?.[PACKAGE_NAME] ?? null,
    lockedVersion: lockedEntry?.version ?? null,
    lockedResolved: lockedEntry?.resolved ?? null,
    installedVersion: installed.installedVersion,
    installedIsLink: installed.installedIsLink,
    allowLinked: options.allowLinked,
  });

  if (problems.length === 0) {
    console.log(
      "[verify-azoa-sdk-pin] " +
        PACKAGE_NAME +
        " pin is consistent (" +
        declaredRange +
        ").",
    );
    return 0;
  }

  const wanted = String(declaredRange ?? "").replace(/^[~^]/, "") || "<version>";
  console.error(
    "[verify-azoa-sdk-pin] The " +
      PACKAGE_NAME +
      " pin is not internally consistent:\n",
  );
  for (const problem of problems) console.error("  - " + problem);
  console.error(
    "\nRemediation, in this order (see conductor/RUNBOOK.md section 2):\n" +
      "  1. Publish " +
      PACKAGE_NAME +
      "@" +
      wanted +
      " from " +
      SDK_SOURCE +
      ".\n" +
      "  2. In ardanova-client: npm install " +
      PACKAGE_NAME +
      "@" +
      declaredRange +
      "\n" +
      "  3. Commit package-lock.json.\n" +
      "  4. Verify from a clean clone with no npm link: npm ci && npm run build.\n",
  );
  return 1;
}

const invokedPath = process.argv[1] ?? "";
if (invokedPath.endsWith("verify-azoa-sdk-pin.mjs")) {
  try {
    process.exit(main(process.argv.slice(2)));
  } catch (error) {
    console.error("[verify-azoa-sdk-pin] " + error.message);
    process.exit(1);
  }
}

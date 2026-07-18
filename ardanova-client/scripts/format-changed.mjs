import { existsSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const clientPrefix = "ardanova-client/";
const supportedFile = /\.(?:[cm]?[jt]sx?|css|json|mdx?|ya?ml)$/i;

function gitLines(args) {
  const result = spawnSync("git", args, { encoding: "utf8" });
  if (result.status !== 0) {
    console.error(result.stderr || "Could not inspect changed files.");
    process.exit(result.status ?? 1);
  }
  return result.stdout.split(/\r?\n/).filter(Boolean);
}

function toClientPath(repoPath) {
  const clientPath = repoPath.startsWith(clientPrefix)
    ? repoPath.slice(clientPrefix.length)
    : repoPath;
  const localPath = existsSync(clientPath) ? clientPath : `../${repoPath}`;
  return supportedFile.test(localPath) && existsSync(localPath)
    ? localPath
    : null;
}

const workingPaths = gitLines([
  "diff",
  "--name-only",
  "--diff-filter=ACMR",
  "HEAD",
]);
const untrackedPaths = gitLines(["ls-files", "--others", "--exclude-standard"]);
const recentCommitPaths =
  process.env.CI && workingPaths.length === 0 && untrackedPaths.length === 0
    ? gitLines(["diff", "--name-only", "--diff-filter=ACMR", "HEAD^", "HEAD"])
    : [];

const files = [
  ...new Set(
    [...workingPaths, ...untrackedPaths, ...recentCommitPaths]
      .map(toClientPath)
      .filter(Boolean),
  ),
];

if (files.length === 0) {
  console.log("[format:changed] No changed frontend source files.");
  process.exit(0);
}

const prettier = fileURLToPath(
  new URL("../node_modules/prettier/bin/prettier.cjs", import.meta.url),
);
const result = spawnSync(
  process.execPath,
  [prettier, "--check", ...files, "--cache"],
  {
    stdio: "inherit",
  },
);
process.exit(result.status ?? 1);

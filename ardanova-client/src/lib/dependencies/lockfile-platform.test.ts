import { execFileSync } from "node:child_process";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { afterEach, describe, expect, it } from "vitest";

// Exercises the real artifact CI runs (scripts/verify-lockfile-platform.mjs).
// It exists because a Windows-regenerated lock (5fc7d30) silently failed seven
// Railway client builds; see conductor/RUNBOOK.md "Client lockfile platform drift".

const clientRoot = path.resolve(__dirname, "../../..");
const guardScript = path.join(clientRoot, "scripts", "verify-lockfile-platform.mjs");
const binding = "node_modules/@rolldown/binding-wasm32-wasi";

const scratchDirectories: string[] = [];
afterEach(() => {
  for (const directory of scratchDirectories.splice(0)) {
    rmSync(directory, { recursive: true, force: true });
  }
});

function runGuard(packages: Record<string, unknown> | null): { status: number; output: string } {
  const args: string[] = [];
  if (packages) {
    const directory = mkdtempSync(path.join(tmpdir(), "lockfile-platform-"));
    scratchDirectories.push(directory);
    const lockfilePath = path.join(directory, "package-lock.json");
    writeFileSync(lockfilePath, JSON.stringify({ lockfileVersion: 3, packages }));
    args.push(lockfilePath);
  }
  try {
    const stdout = execFileSync(process.execPath, [guardScript, ...args], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
    return { status: 0, output: stdout };
  } catch (error) {
    const failure = error as { status?: number; stdout?: string; stderr?: string };
    return { status: failure.status ?? 1, output: `${failure.stdout ?? ""}${failure.stderr ?? ""}` };
  }
}

const bindingNeeding = (range: string) => ({ [binding]: { version: "1.0.0", dependencies: { "@emnapi/core": range } } });

describe("verify-lockfile-platform", () => {
  it("passes the repository's own lockfile", () => {
    const run = runGuard(null);
    expect(run.output).toContain("resolve every dependency");
    expect(run.status).toBe(0);
  });

  it.each([
    {
      name: "a Windows-written lock: the nested exact pin is missing and the hoisted copy is a different version",
      packages: { ...bindingNeeding("1.11.1"), "node_modules/@emnapi/core": { version: "1.11.2" } },
      expected: "needs @emnapi/core@1.11.1, but it resolves to node_modules/@emnapi/core@1.11.2",
    },
    {
      name: "the dependency has no lock entry anywhere",
      packages: bindingNeeding("1.11.1"),
      expected: "needs @emnapi/core@1.11.1, but the lockfile has no entry for it",
    },
    {
      name: "a range shape the check cannot verify",
      packages: { ...bindingNeeding(">=1.0.0"), "node_modules/@emnapi/core": { version: "1.11.2" } },
      expected: 'pins @emnapi/core as ">=1.0.0", which this check cannot verify',
    },
  ])("fails $name", ({ packages, expected }) => {
    const run = runGuard(packages);
    expect(run.output).toContain(expected);
    expect(run.output).toContain("node:20-alpine");
    expect(run.status).toBe(1);
  });

  it("prefers the binding's own nested entry over a non-matching hoisted copy, as npm does", () => {
    const run = runGuard({
      ...bindingNeeding("1.11.1"),
      [`${binding}/node_modules/@emnapi/core`]: { version: "1.11.1" },
      "node_modules/@emnapi/core": { version: "1.11.2" },
    });
    expect(run.status).toBe(0);
  });

  it("walks up nested node_modules before falling back to the root", () => {
    const nestedBinding = "node_modules/host/node_modules/@tailwindcss/oxide-wasm32-wasi";
    const run = runGuard({
      [nestedBinding]: { version: "4.0.0", dependencies: { "@emnapi/core": "^1.4.0" } },
      "node_modules/host/node_modules/@emnapi/core": { version: "1.4.3" },
      "node_modules/@emnapi/core": { version: "0.45.0" },
    });
    expect(run.status).toBe(0);
  });
});

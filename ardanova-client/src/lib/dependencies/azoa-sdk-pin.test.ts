import { execFileSync } from "node:child_process";
import {
  mkdtempSync,
  mkdirSync,
  rmSync,
  symlinkSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { describe, expect, it } from "vitest";

// Exercises the real artifact CI runs (scripts/verify-azoa-sdk-pin.mjs), not an
// extracted copy of its logic. The guard exists because commit ea9b9d1 claimed a
// lockfile fix it never landed; a claim in prose is not a control, so this test
// pins the control itself.

const clientRoot = path.resolve(__dirname, "../../..");
const guardScript = path.join(clientRoot, "scripts", "verify-azoa-sdk-pin.mjs");

interface GuardRun {
  status: number;
  output: string;
}

function runGuard(args: string[]): GuardRun {
  try {
    const stdout = execFileSync(process.execPath, [guardScript, ...args], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
    return { status: 0, output: stdout };
  } catch (error) {
    const failure = error as {
      status?: number;
      stdout?: string;
      stderr?: string;
    };
    return {
      status: failure.status ?? 1,
      output: `${failure.stdout ?? ""}${failure.stderr ?? ""}`,
    };
  }
}

function writeFixture(options: {
  declaredRange: string;
  lockedRange: string;
  lockedVersion: string;
  lockedResolved?: string;
  installedVersion?: string;
  installAsSymlink?: boolean;
}): { packageJsonPath: string; lockfilePath: string; nodeModulesPath: string } {
  const root = mkdtempSync(path.join(tmpdir(), "azoa-sdk-pin-"));
  const packageJsonPath = path.join(root, "package.json");
  const lockfilePath = path.join(root, "package-lock.json");
  const nodeModulesPath = path.join(root, "node_modules");

  writeFileSync(
    packageJsonPath,
    JSON.stringify({ dependencies: { "azoa-sdk": options.declaredRange } }),
  );
  writeFileSync(
    lockfilePath,
    JSON.stringify({
      packages: {
        "": { dependencies: { "azoa-sdk": options.lockedRange } },
        "node_modules/azoa-sdk": {
          version: options.lockedVersion,
          resolved:
            options.lockedResolved ??
            `https://registry.npmjs.org/azoa-sdk/-/azoa-sdk-${options.lockedVersion}.tgz`,
        },
      },
    }),
  );

  mkdirSync(nodeModulesPath, { recursive: true });
  if (options.installedVersion) {
    const realPackage = path.join(root, "linked-source");
    mkdirSync(realPackage, { recursive: true });
    writeFileSync(
      path.join(realPackage, "package.json"),
      JSON.stringify({ name: "azoa-sdk", version: options.installedVersion }),
    );
    const installedPath = path.join(nodeModulesPath, "azoa-sdk");
    if (options.installAsSymlink) {
      symlinkSync(
        realPackage,
        installedPath,
        process.platform === "win32" ? "junction" : "dir",
      );
    } else {
      mkdirSync(installedPath, { recursive: true });
      writeFileSync(
        path.join(installedPath, "package.json"),
        JSON.stringify({ name: "azoa-sdk", version: options.installedVersion }),
      );
    }
  }
  return { packageJsonPath, lockfilePath, nodeModulesPath };
}

function runAgainstFixture(
  options: Parameters<typeof writeFixture>[0] & { allowLinked?: boolean },
): GuardRun {
  const fixture = writeFixture(options);
  const args = [
    "--package-json",
    fixture.packageJsonPath,
    "--lockfile",
    fixture.lockfilePath,
    "--node-modules",
    fixture.nodeModulesPath,
  ];
  if (options.allowLinked) args.push("--allow-linked");
  try {
    return runGuard(args);
  } finally {
    rmSync(path.dirname(fixture.packageJsonPath), {
      recursive: true,
      force: true,
    });
  }
}

describe("verify-azoa-sdk-pin", () => {
  it("passes when the manifest, the lockfile and the install all agree", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.4.0",
      lockedRange: "^0.4.0",
      lockedVersion: "0.4.1",
      installedVersion: "0.4.1",
    });
    expect(result.status).toBe(0);
    expect(result.output).toContain("pin is consistent");
  });

  it("names the EUSAGE abort when package.json and the lockfile disagree", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.4.0",
      lockedRange: "^0.1.0",
      lockedVersion: "0.1.0",
    });
    expect(result.status).toBe(1);
    expect(result.output).toContain("EUSAGE");
    // The exact misconception the wave-0 commit message created.
    expect(result.output).toContain("alone will NOT turn CI green");
  });

  // The defect the guard was written to catch: npm's caret is minor-locked on
  // 0.x, so "^0.3.0" can never resolve a published 0.4.0. Publishing would have
  // left CI red with a message that blamed the registry.
  it("rejects a caret pin that the version being published cannot satisfy", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.3.0",
      lockedRange: "^0.3.0",
      lockedVersion: "0.4.0",
    });
    expect(result.status).toBe(1);
    expect(result.output).toContain("does not satisfy");
  });

  it("rejects a lockfile whose resolved tarball is not the locked version", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.4.0",
      lockedRange: "^0.4.0",
      lockedVersion: "0.4.0",
      lockedResolved:
        "https://registry.npmjs.org/azoa-sdk/-/azoa-sdk-0.1.0.tgz",
    });
    expect(result.status).toBe(1);
    expect(result.output).toContain("registry tarball");
  });

  it("fails on a symlinked install, because a link is invisible to git", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.4.0",
      lockedRange: "^0.4.0",
      lockedVersion: "0.4.0",
      installedVersion: "0.4.0",
      installAsSymlink: true,
    });
    expect(result.status).toBe(1);
    expect(result.output).toContain("symlink to local source");
  });

  it("tolerates a symlinked install only behind --allow-linked", () => {
    const result = runAgainstFixture({
      declaredRange: "^0.4.0",
      lockedRange: "^0.4.0",
      lockedVersion: "0.4.0",
      installedVersion: "0.4.0",
      installAsSymlink: true,
      allowLinked: true,
    });
    expect(result.status).toBe(0);
  });

  it("fails closed on a range shape it cannot verify", () => {
    const result = runAgainstFixture({
      declaredRange: "latest",
      lockedRange: "latest",
      lockedVersion: "0.4.0",
    });
    expect(result.status).toBe(1);
    expect(result.output).toContain("cannot verify");
  });

  // Ties the guard to this repo's actual files, so the pin cannot drift back to
  // a state the guard would have caught. Both branches assert something true:
  // once azoa-sdk is published and the lockfile regenerated, this flips to the
  // exit-0 branch rather than needing an edit.
  it("reports the real repository state accurately", () => {
    const result = runGuard([]);
    if (result.status === 0) {
      expect(result.output).toContain("pin is consistent");
    } else {
      // Still blocked on OI-01 (azoa-sdk unpublished, npm whoami 401s).
      expect(result.output).toContain("npm install azoa-sdk@^0.4.0");
      expect(result.output).toContain("Publish azoa-sdk@0.4.0");
    }
  });
});

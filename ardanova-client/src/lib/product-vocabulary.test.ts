import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import path from "node:path";
import { afterEach, describe, expect, it } from "vitest";

// Exercises the real artifact CI runs (scripts/verify-product-vocabulary.mjs), not an extracted
// copy of its logic.
//
// The guard exists because the Wave 0 remediation asserted in prose that the client had been
// swept for blockchain vocabulary. It had not: src/app/credentials/[id]/page.tsx - an ungated
// page showing a worker their earned tier - still said "Blockchain Status", "Minted on
// Algorand", "Algorand Standard Asset" and "ASA #". A claim in prose is not a control.

const clientRoot = path.resolve(__dirname, "../..");
const guardScript = path.join(
  clientRoot,
  "scripts",
  "verify-product-vocabulary.mjs",
);

interface GuardRun {
  status: number;
  output: string;
}

function runGuard(args: string[]): GuardRun {
  try {
    const stdout = execFileSync(process.execPath, [guardScript, ...args], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
      cwd: clientRoot,
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

const fixtureRoots: string[] = [];

/** Writes one throwaway client tree containing exactly the files given. */
function fixture(files: Record<string, string>): string {
  const root = mkdtempSync(path.join(tmpdir(), "product-vocabulary-"));
  fixtureRoots.push(root);
  for (const [relativePath, contents] of Object.entries(files)) {
    const absolute = path.join(root, relativePath);
    mkdirSync(path.dirname(absolute), { recursive: true });
    writeFileSync(absolute, contents, "utf8");
  }
  return root;
}

afterEach(() => {
  while (fixtureRoots.length > 0) {
    rmSync(fixtureRoots.pop()!, { recursive: true, force: true });
  }
});

/** The copy that actually shipped on the credential detail page, verbatim. */
const shippedCredentialPage = `"use client";
export default function CredentialDetailPage() {
  return (
    <div>
      <h2>Blockchain Status</h2>
      <div>{isOnChain ? "Minted on Algorand" : "Pending mint"}</div>
      <div>
        {isOnChain
          ? "This credential is verified on the Algorand blockchain"
          : "This credential has not yet been minted on-chain"}
      </div>
      <div>On-chain data matches platform records</div>
      <h3>Algorand Standard Asset</h3>
      <p>ASA #{asaInfo.assetId}</p>
      <span>{credential.isTransferable ? "Yes" : "No (Soulbound)"}</span>
    </div>
  );
}
`;

describe("the words a non-crypto member is shown", () => {
  it("catches every chain term the shipped credential page rendered", () => {
    const root = fixture({
      "src/app/credentials/[id]/page.tsx": shippedCredentialPage,
    });
    const result = runGuard(["--root", root]);

    expect(result.status).toBe(1);
    for (const term of [
      "algorand",
      "block ?chain",
      "on[- ]?chain",
      "soulbound",
      "asa",
      "minted",
    ]) {
      expect(result.output, `the guard missed "${term}"`).toContain(term);
    }
  });

  it("catches the money words the thesis names, not only chain names", () => {
    const root = fixture({
      "src/components/holdings.tsx":
        "export const Holdings = () => <p>No token balance records were returned.</p>;\n",
    });
    const result = runGuard(["--root", root]);

    expect(result.status).toBe(1);
    expect(result.output).toContain("money: token");
  });

  it("passes copy that says the same thing in the member's words", () => {
    const root = fixture({
      "src/app/credentials/[id]/page.tsx": `"use client";
export default function CredentialDetailPage() {
  return (
    <div>
      <h2>Verification</h2>
      <div>Independently verifiable outside ArdaNova.</div>
      <span>Reference</span>
    </div>
  );
}
`,
    });
    expect(runGuard(["--root", root]).status).toBe(0);
  });

  it("leaves the role-gated operator surface alone", () => {
    const root = fixture({
      "src/app/admin/kyc/page.tsx":
        "export default function Kyc() { return <p>Algorand wallet address</p>; }\n",
      "src/app/api/sdk/route.ts":
        'export const GET = () => Response.json({ note: "on-chain assetId" });\n',
    });
    expect(runGuard(["--root", root]).status).toBe(0);
  });

  it("does not mistake an identifier, an import or a type for something a person reads", () => {
    const root = fixture({
      "src/components/reader.tsx": `import { tokenBalances } from "~/lib/api/token-balances";
type ChainKind = "wallet" | "account";
const isOnChain = true;
const mintTxHash = tokenBalances.mintTxHash;
export const Reader = () => (
  <div className="border-token bg-mint-500" data-kind="wallet">
    <span>{isOnChain ? "Verified" : "Verification pending"}</span>
  </div>
);
`,
    });
    const result = runGuard(["--root", root]);
    expect(result.status, result.output).toBe(0);
  });

  // Ties the guard to this repository's actual files. The file count assertion is what stops it
  // passing vacuously: a scan that silently walked nothing would still exit 0.
  it("reports the real client tree as clean, having actually read it", () => {
    const result = runGuard(["--json"]);
    const report = JSON.parse(result.output) as {
      scannedFiles: number;
      violations: unknown[];
    };

    expect(report.scannedFiles).toBeGreaterThan(100);
    expect(report.violations).toEqual([]);
    expect(result.status).toBe(0);
  });
});

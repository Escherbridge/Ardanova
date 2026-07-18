"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import {
  ArrowUpRight,
  Check,
  DatabaseZap,
  Loader2,
  RefreshCw,
  ShieldCheck,
  TriangleAlert,
} from "lucide-react";

import { CustodialAccountCard } from "~/components/azoa/custodial-account-card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import type { AzoaKycSessionDto } from "~/lib/api/ardanova";
import { normalizeInternalReturnTo } from "~/lib/auth-navigation";
import { api } from "~/trpc/react";

function formatSessionExpiry(value: string | null | undefined): string {
  if (!value) return "an unknown time";
  const expiry = new Date(value);
  if (Number.isNaN(expiry.getTime())) return "an unknown time";
  return `${expiry.toISOString().replace("T", " ").replace(".000Z", " UTC")}`;
}

export default function VerificationPage() {
  const searchParams = useSearchParams();
  const utils = api.useUtils();
  const [providerSession, setProviderSession] =
    useState<AzoaKycSessionDto | null>(null);
  const [currentTime, setCurrentTime] = useState<number | null>(null);

  const capabilities = api.azoaCustodialAccount.getCapabilities.useQuery(
    undefined,
    {
      retry: false,
    },
  );
  const custodyStatus = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    retry: false,
  });
  const ensureCustody = api.azoaCustodialAccount.ensure.useMutation({
    onSuccess: async () => {
      await utils.azoaCustodialAccount.getStatus.invalidate();
    },
  });
  const beginKyc = api.azoaCustodialAccount.beginKyc.useMutation({
    onSuccess: async (value) => {
      setProviderSession(value);
      await utils.azoaCustodialAccount.getStatus.invalidate();
    },
  });

  const providerSessionExpiry = providerSession?.expiresAt
    ? Date.parse(providerSession.expiresAt)
    : Number.NaN;
  const providerSessionExpired = Boolean(
    providerSession?.expiresAt &&
      (!Number.isFinite(providerSessionExpiry) ||
        (currentTime !== null && providerSessionExpiry <= currentTime)),
  );

  useEffect(() => {
    setCurrentTime(Date.now());
    if (!Number.isFinite(providerSessionExpiry)) return;

    const delay = Math.max(
      0,
      Math.min(providerSessionExpiry - Date.now() + 250, 2_147_000_000),
    );
    const timeout = window.setTimeout(() => setCurrentTime(Date.now()), delay);
    return () => window.clearTimeout(timeout);
  }, [providerSessionExpiry]);

  const status = custodyStatus.data?.kycStatus ?? "Unknown";
  const returnTo = normalizeInternalReturnTo(
    searchParams.get("returnTo") ?? undefined,
  );
  const developmentSimulation =
    providerSession?.developmentSimulation ??
    capabilities.data?.developmentSimulation ??
    false;
  const canStart = Boolean(
    custodyStatus.data?.identityReady &&
      custodyStatus.data.kycReady &&
      capabilities.data?.kycReady &&
      (capabilities.data.hostedVerification ||
        capabilities.data.developmentSimulation),
  );

  return (
    <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-6 sm:px-6 sm:py-8">
      <header className="border-foreground grid gap-5 border-b-2 pb-6 md:grid-cols-[1fr_auto] md:items-end">
        <div>
          <p className="text-primary font-mono text-xs font-black tracking-[0.16em] uppercase">
            Profile / Trust boundary
          </p>
          <h1 className="mt-2 font-mono text-4xl font-black tracking-[-0.055em] uppercase sm:text-5xl">
            Verify once. Act with clarity.
          </h1>
          <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed sm:text-base">
            Azoa owns verification and key custody. ArdaNova starts the flow and
            reads the outcome, but never stores identity documents or private
            keys.
          </p>
        </div>
        <Button
          variant="outline"
          className="min-h-11 w-full md:w-auto"
          onClick={() => {
            void capabilities.refetch();
            void custodyStatus.refetch();
          }}
          disabled={capabilities.isFetching || custodyStatus.isFetching}
        >
          <RefreshCw
            className={
              capabilities.isFetching || custodyStatus.isFetching
                ? "size-4 animate-spin"
                : "size-4"
            }
            aria-hidden="true"
          />
          Check status
        </Button>
      </header>

      <CustodialAccountCard
        status={custodyStatus.data}
        isLoading={custodyStatus.isLoading}
        isEnsuring={ensureCustody.isPending}
        statusError={custodyStatus.error?.message}
        ensureError={ensureCustody.error?.message}
        verificationHref={null}
        onEnsure={() => ensureCustody.mutate()}
      />

      {returnTo && custodyStatus.data?.ready && (
        <section
          className="border-foreground bg-primary text-primary-foreground flex flex-col gap-4 border-2 p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6"
          aria-labelledby="verification-resume-title"
        >
          <div>
            <p className="font-mono text-xs font-black tracking-[0.14em] uppercase">
              Setup confirmed
            </p>
            <h2
              id="verification-resume-title"
              className="mt-1 font-mono text-xl font-black uppercase"
            >
              Continue where you left off
            </h2>
            <p className="mt-1 max-w-2xl text-sm leading-relaxed opacity-90">
              {developmentSimulation
                ? "Your development-only Azoa simulation and managed wallet are ready for local testing. This is not production identity proof."
                : "Your Azoa action account, identity check, and managed wallet are ready. Return to the work that asked for verification."}
            </p>
          </div>
          <Button
            variant="secondary"
            className="min-h-11 w-full shrink-0 sm:w-auto"
            asChild
          >
            <Link href={returnTo}>Resume my work</Link>
          </Button>
        </section>
      )}

      <section
        className="border-foreground bg-card border-2"
        aria-labelledby="verification-flow-title"
      >
        <div className="border-foreground grid gap-4 border-b-2 p-5 sm:grid-cols-[1fr_auto] sm:items-center sm:p-6">
          <div>
            <div className="mb-2 flex flex-wrap items-center gap-2">
              <Badge variant={status === "Approved" ? "success" : "outline"}>
                {status}
              </Badge>
              {developmentSimulation && (
                <Badge variant="secondary">Development simulation</Badge>
              )}
              <span className="text-muted-foreground font-mono text-xs uppercase">
                {capabilities.data?.kycProvider
                  ? `${capabilities.data.kycProvider} via Azoa`
                  : "Provider selected by node operator"}
              </span>
            </div>
            <h2
              id="verification-flow-title"
              className="font-mono text-2xl font-black tracking-[-0.04em] uppercase"
            >
              Identity verification
            </h2>
          </div>
          {status !== "Approved" && !providerSession && (
            <Button
              className="min-h-11 w-full sm:w-auto"
              disabled={!canStart || beginKyc.isPending}
              onClick={() => beginKyc.mutate()}
            >
              {beginKyc.isPending ? (
                <Loader2 className="size-4 animate-spin" aria-hidden="true" />
              ) : (
                <ShieldCheck className="size-4" aria-hidden="true" />
              )}
              {developmentSimulation
                ? "Start development simulation"
                : "Begin verification"}
            </Button>
          )}
        </div>

        {status === "Approved" ? (
          <div className="flex items-start gap-4 p-5 sm:p-6">
            <span className="border-foreground bg-primary text-primary-foreground inline-flex size-10 shrink-0 items-center justify-center border-2">
              <Check className="size-5" aria-hidden="true" />
            </span>
            <div>
              <h3 className="font-mono text-lg font-black uppercase">
                {developmentSimulation
                  ? "Development simulation approved"
                  : "Azoa approved"}
              </h3>
              <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                {developmentSimulation
                  ? "This approval exists only to exercise the local review loop. It is not KYC, identity evidence, or permission for a Production value action."
                  : "Sensitive platform gates confirm this status with Azoa again; a cached ArdaNova badge is never sufficient authority."}
              </p>
            </div>
          </div>
        ) : (
          <div className="space-y-5 p-5 sm:p-6">
            {(!capabilities.data?.kycReady ||
              capabilities.error ||
              beginKyc.error) && (
              <div
                className="border-destructive flex items-start gap-3 border-2 p-4"
                role="status"
              >
                <TriangleAlert
                  className="text-destructive mt-0.5 size-5 shrink-0"
                  aria-hidden="true"
                />
                <p className="text-sm leading-relaxed">
                  {beginKyc.error?.message ??
                    capabilities.error?.message ??
                    "Identity verification is not configured on this Azoa node yet. Wallet custody may be configured independently."}
                </p>
              </div>
            )}

            {!providerSession && (
              <>
                {capabilities.data?.developmentSimulation && (
                  <div
                    className="border-foreground bg-secondary flex items-start gap-3 border-2 p-4"
                    role="status"
                  >
                    <TriangleAlert
                      className="mt-0.5 size-5 shrink-0"
                      aria-hidden="true"
                    />
                    <div>
                      <h3 className="font-mono text-sm font-black uppercase">
                        Development review simulation
                      </h3>
                      <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                        This Azoa node is explicitly offering a development-only
                        manual review loop. It collects no document URL and
                        proves no identity. Production nodes never expose this
                        option.
                      </p>
                    </div>
                  </div>
                )}

                <div className="grid gap-4 sm:grid-cols-3">
                  {[
                    [
                      "01",
                      "Create account",
                      "An explicit action creates your tenant-bound Azoa avatar.",
                    ],
                    [
                      "02",
                      developmentSimulation
                        ? "Run a simulation"
                        : "Use the provider",
                      developmentSimulation
                        ? "Start a clearly labeled development attempt without submitting identity material."
                        : "The node operator configures a reviewed hosted or private-upload provider.",
                    ],
                    [
                      "03",
                      "Review outcome",
                      "ArdaNova receives only a normalized status and safe account references.",
                    ],
                  ].map(([number, title, body]) => (
                    <div
                      key={number}
                      className="border-foreground border-2 p-4"
                    >
                      <span className="text-primary font-mono text-xs font-black">
                        {number}
                      </span>
                      <h3 className="mt-3 font-mono text-sm font-black uppercase">
                        {title}
                      </h3>
                      <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
                        {body}
                      </p>
                    </div>
                  ))}
                </div>

                {capabilities.data?.kycReady &&
                  capabilities.data.acceptsDocumentReferences &&
                  !capabilities.data.hostedVerification &&
                  !capabilities.data.developmentSimulation && (
                    <div className="border-foreground flex items-start gap-3 border-2 p-4">
                      <DatabaseZap
                        className="text-primary mt-0.5 size-5 shrink-0"
                        aria-hidden="true"
                      />
                      <div>
                        <h3 className="font-mono text-sm font-black uppercase">
                          Operator upload required
                        </h3>
                        <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                          This node currently uses a private-reference workflow.
                          ArdaNova will not create a dead-end attempt or ask for
                          a public document URL. Ask the node operator to enable
                          a reviewed hosted flow or an Azoa-owned private
                          upload.
                        </p>
                      </div>
                    </div>
                  )}
              </>
            )}

            {providerSession && (
              <div className="space-y-4">
                <div className="border-foreground grid gap-4 border-2 p-4 sm:grid-cols-[1fr_auto] sm:items-center">
                  <div>
                    <p className="font-mono text-xs font-black tracking-[0.12em] uppercase">
                      {providerSession.developmentSimulation
                        ? "Development review simulation"
                        : `${providerSession.provider} verification`}
                    </p>
                    <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
                      {providerSession.developmentSimulation
                        ? "A node operator may now approve or reject this test attempt. Do not submit identity documents; this simulation is not identity verification."
                        : (providerSession.instructions ??
                          "Follow the selected provider's instructions. Verification never moves funds or grants transaction authority.")}
                    </p>
                    {providerSession.expiresAt && (
                      <p
                        className={`mt-2 font-mono text-xs font-bold uppercase ${
                          providerSessionExpired
                            ? "text-destructive"
                            : "text-muted-foreground"
                        }`}
                        role="status"
                      >
                        {providerSessionExpired
                          ? "Session expired"
                          : "Session expires"}{" "}
                        {formatSessionExpiry(providerSession.expiresAt)}
                      </p>
                    )}
                  </div>
                  {providerSession.hostedVerification &&
                    providerSession.verificationUrl &&
                    !providerSessionExpired && (
                      <Button className="min-h-11 w-full sm:w-auto" asChild>
                        <a
                          href={providerSession.verificationUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          Continue with provider
                          <ArrowUpRight className="size-4" aria-hidden="true" />
                        </a>
                      </Button>
                    )}
                </div>

                <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
                  <Button
                    type="button"
                    variant="outline"
                    className="min-h-11 w-full sm:w-auto"
                    onClick={() => void custodyStatus.refetch()}
                    disabled={custodyStatus.isFetching}
                  >
                    <RefreshCw
                      className={
                        custodyStatus.isFetching
                          ? "size-4 animate-spin"
                          : "size-4"
                      }
                      aria-hidden="true"
                    />
                    {providerSession.developmentSimulation
                      ? "Check operator review"
                      : "I'm done — check status"}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11 w-full sm:w-auto"
                    onClick={() => beginKyc.mutate()}
                    disabled={!canStart || beginKyc.isPending}
                  >
                    {beginKyc.isPending && (
                      <Loader2
                        className="size-4 animate-spin"
                        aria-hidden="true"
                      />
                    )}
                    {providerSessionExpired
                      ? "Start a new session"
                      : "Refresh this session"}
                  </Button>
                </div>

                {providerSession.acceptsDocumentReferences &&
                  !providerSession.hostedVerification &&
                  !providerSession.developmentSimulation && (
                    <div className="border-foreground flex items-start gap-3 border-2 p-4">
                      <DatabaseZap
                        className="text-primary mt-0.5 size-5 shrink-0"
                        aria-hidden="true"
                      />
                      <div>
                        <h3 className="font-mono text-sm font-black uppercase">
                          Private upload required
                        </h3>
                        <p className="text-muted-foreground mt-1 text-sm leading-relaxed">
                          This provider expects operator-managed document
                          references. ArdaNova will not ask you to paste public
                          identity-document URLs. The node operator must add a
                          private, scoped upload flow before this method can be
                          used here.
                        </p>
                      </div>
                    </div>
                  )}
              </div>
            )}
          </div>
        )}
      </section>
    </div>
  );
}

import Link from "next/link";
import { redirect } from "next/navigation";
import { revalidatePath } from "next/cache";
import { ArrowUpRight, ServerCog, ShieldCheck, Unlink } from "lucide-react";

import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import { auth } from "~/server/auth";
import { getAdminApiClient } from "~/server/admin-api-client";
import { runWithActorAssertion } from "~/server/actor-assertion";

// ---------------------------------------------------------------------------
// Wire shapes returned by ArdaNova.API.Controllers.AdminAzoaBindingController
// ---------------------------------------------------------------------------
interface AzoaBindingResponse {
  userId: string;
  azoaAvatarId: string | null;
  azoaWalletId: string | null;
  azoaWalletAddress: string | null;
  verificationLevel: string;
}

interface AzoaBindingUnboundResponse {
  userId: string;
  operatorActorId: string;
  reason: string;
  previousAzoaAvatarId: string | null;
  previousAzoaWalletId: string | null;
  previousAzoaWalletAddress: string | null;
  previousVerificationLevel: string;
  verificationLevel: string;
  unboundAt: string;
}

function buildKycUrl(params: Record<string, string | undefined>): string {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) query.set(key, value);
  }
  const search = query.toString();
  return search ? `/admin/kyc?${search}` : "/admin/kyc";
}

/** Server action: severs a stale local AZOA binding. Re-verifies admin auth independent of the page's own gate. */
async function unbindAzoaAccount(identifier: string, formData: FormData) {
  "use server";

  const reason = String(formData.get("reason") ?? "").trim();
  const session = await auth();
  if (!session?.user?.id || session.user.role !== "ADMIN") {
    redirect(
      buildKycUrl({ identifier, error: "Admin access required." }),
    );
  }
  if (!reason) {
    redirect(
      buildKycUrl({ identifier, error: "A reason is required to unbind an AZOA account." }),
    );
  }

  const client = getAdminApiClient();
  const response = await runWithActorAssertion(
    { subject: session.user.id, role: session.user.role },
    () =>
      client.post<AzoaBindingUnboundResponse>(
        `/api/admin/azoa-binding/${encodeURIComponent(identifier)}/unbind`,
        { reason },
      ),
  );

  if (response.error || !response.data) {
    redirect(
      buildKycUrl({
        identifier,
        error: response.error ?? "Unbind failed.",
      }),
    );
  }

  revalidatePath("/admin/kyc");
  redirect(buildKycUrl({ identifier, unbound: "1" }));
}

export default async function KycOperationsPage({
  searchParams,
}: {
  searchParams: Promise<{ identifier?: string; error?: string; unbound?: string }>;
}) {
  const params = await searchParams;
  const identifier = params.identifier?.trim();

  const session = await auth();
  if (!session?.user?.id || session.user.role !== "ADMIN") {
    redirect("/dashboard");
  }

  let binding: AzoaBindingResponse | null = null;
  let lookupError: string | null = null;

  if (identifier) {
    const client = getAdminApiClient();
    const response = await runWithActorAssertion(
      { subject: session.user.id, role: session.user.role },
      () =>
        client.get<AzoaBindingResponse>(
          `/api/admin/azoa-binding/${encodeURIComponent(identifier)}`,
        ),
    );
    if (response.error || !response.data) {
      lookupError = response.error ?? "Lookup failed.";
    } else {
      binding = response.data;
    }
  }

  const hasBinding = Boolean(
    binding &&
      (binding.azoaAvatarId ?? binding.azoaWalletId ?? binding.azoaWalletAddress),
  );

  return (
    <div className="mx-auto w-full max-w-5xl space-y-6 px-4 py-6 sm:px-6 sm:py-8">
      <header className="border-foreground border-b-2 pb-6">
        <Badge variant="outline" className="font-mono uppercase">
          Authority moved to Azoa
        </Badge>
        <h1 className="mt-4 font-mono text-4xl font-black tracking-[-0.055em] uppercase sm:text-5xl">
          Identity operations
        </h1>
        <p className="text-muted-foreground mt-3 max-w-2xl text-sm leading-relaxed sm:text-base">
          ArdaNova no longer reviews identity documents or approves KYC locally.
          The configured Azoa node owns provider selection, review tooling,
          retention, and the authoritative decision. AZOA is also the single
          writer of every member&apos;s verification level: this application
          only projects what Azoa reports.
        </p>
      </header>

      <section className="border-foreground bg-card grid border-2 md:grid-cols-2">
        <div className="border-foreground space-y-4 border-b-2 p-5 md:border-r-2 md:border-b-0 md:p-6">
          <ServerCog className="text-primary size-8" aria-hidden="true" />
          <div>
            <h2 className="font-mono text-xl font-black uppercase">
              Node operator tooling
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
              Configure a supported provider and conduct provider-appropriate
              review inside Azoa. Unknown or incomplete providers remain
              unavailable.
            </p>
          </div>
        </div>
        <div className="space-y-4 p-5 md:p-6">
          <ShieldCheck className="text-primary size-8" aria-hidden="true" />
          <div>
            <h2 className="font-mono text-xl font-black uppercase">
              ArdaNova boundary
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
              This application stores safe account references and normalized
              status only. It does not expose document links, provider payloads,
              or override external-provider decisions.
            </p>
          </div>
        </div>
      </section>

      <section className="border-foreground bg-card space-y-3 border-2 p-5 md:p-6">
        <h2 className="font-mono text-xl font-black uppercase">
          Member-facing statuses
        </h2>
        <p className="text-muted-foreground text-sm leading-relaxed">
          Azoa now computes an{" "}
          <span className="text-foreground font-mono font-semibold">
            &quot;Under review — contact support&quot;
          </span>{" "}
          state for a member whose identity is verified but whose account
          still awaits a compliance decision (an AML screening match that
          needs a human verdict). No reason is disclosed to the member or to
          ArdaNova; Azoa&apos;s operator resolves it from the node&apos;s own
          review queue. ArdaNova&apos;s projection of this exact state is not
          wired yet — see AzoaCustodialAccountService.ProjectVerificationLevel
          for the blocking gap — so it still surfaces as a generic error here
          today.
        </p>
      </section>

      <section className="border-foreground bg-card space-y-5 border-2 p-5 md:p-6">
        <div className="flex items-start gap-3">
          <Unlink className="text-primary mt-1 size-8 shrink-0" aria-hidden="true" />
          <div>
            <h2 className="font-mono text-xl font-black uppercase">
              Unbind an AZOA account
            </h2>
            <p className="text-muted-foreground mt-2 text-sm leading-relaxed">
              Recovery tool for a node rebuild (a dev volume wipe invalidates
              every azoaAvatarId Azoa previously issued). ArdaNova&apos;s unique
              index on azoaAvatarId keeps enforcing the stale value, so the
              member&apos;s next account-setup call Conflicts forever until it is
              cleared here. Unbinding never contacts Azoa — it only clears
              ArdaNova&apos;s local pointers so Azoa can provision a fresh
              binding on the member&apos;s next call. Every unbind is logged with
              the operator&apos;s identity and the reason given.
            </p>
          </div>
        </div>

        <form className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1 space-y-1.5">
            <Label htmlFor="identifier" className="font-mono text-xs uppercase">
              ArdaNova user id or email
            </Label>
            <Input
              id="identifier"
              name="identifier"
              defaultValue={identifier ?? ""}
              placeholder="usr_... or member@example.com"
              className="border-foreground border-2 font-mono"
              required
            />
          </div>
          <Button type="submit" variant="outline" className="border-foreground border-2 font-mono uppercase">
            Look up
          </Button>
        </form>

        {params.error && (
          <p className="border-destructive text-destructive border-2 p-3 font-mono text-sm">
            {params.error}
          </p>
        )}

        {params.unbound === "1" && (
          <p className="border-2 border-emerald-600 p-3 font-mono text-sm text-emerald-600">
            AZOA binding cleared. The member can start account setup again on
            their next visit.
          </p>
        )}

        {identifier && lookupError && !params.error && (
          <p className="border-destructive text-destructive border-2 p-3 font-mono text-sm">
            {lookupError}
          </p>
        )}

        {binding && (
          <div className="border-foreground/40 space-y-4 border-2 border-dashed p-4">
            <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
              <div>
                <dt className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  User id
                </dt>
                <dd className="font-mono">{binding.userId}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  Verification level
                </dt>
                <dd className="font-mono">{binding.verificationLevel}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  AZOA avatar id
                </dt>
                <dd className="font-mono">{binding.azoaAvatarId ?? "— not bound —"}</dd>
              </div>
              <div>
                <dt className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  AZOA wallet id
                </dt>
                <dd className="font-mono">{binding.azoaWalletId ?? "— not bound —"}</dd>
              </div>
              <div className="sm:col-span-2">
                <dt className="text-muted-foreground font-mono text-xs tracking-widest uppercase">
                  AZOA wallet address
                </dt>
                <dd className="font-mono break-all">
                  {binding.azoaWalletAddress ?? "— not bound —"}
                </dd>
              </div>
            </dl>

            {hasBinding ? (
              <form
                action={unbindAzoaAccount.bind(null, binding.userId)}
                className="space-y-3 border-t-2 border-dashed pt-4"
              >
                <div className="space-y-1.5">
                  <Label htmlFor="reason" className="font-mono text-xs uppercase">
                    Reason (required, audited)
                  </Label>
                  <Textarea
                    id="reason"
                    name="reason"
                    required
                    maxLength={1000}
                    placeholder="e.g. AZOA devnet volume wipe on 2026-09-07; user Conflict-locked on EnsureAsync"
                    className="border-foreground border-2 font-mono"
                  />
                </div>
                <Button
                  type="submit"
                  variant="outline"
                  className="border-destructive text-destructive hover:bg-destructive/10 border-2 font-mono uppercase"
                >
                  <Unlink className="mr-2 size-4" aria-hidden="true" />
                  Unbind AZOA account
                </Button>
              </form>
            ) : (
              <p className="text-muted-foreground font-mono text-xs uppercase">
                Nothing to unbind — this user has no AZOA binding.
              </p>
            )}
          </div>
        )}
      </section>

      <Button variant="outline" className="min-h-11 w-full sm:w-auto" asChild>
        <Link href="/dashboard/profile">
          View the member-facing profile flow
          <ArrowUpRight className="size-4" aria-hidden="true" />
        </Link>
      </Button>
    </div>
  );
}

"use client";

import Link from "next/link";
import { useState } from "react";
import { useSession } from "next-auth/react";
import {
  CheckCircle2,
  CircleAlert,
  ExternalLink,
  Loader2,
  LockKeyhole,
  Plus,
  Star,
  Trash2,
  WalletCards,
} from "lucide-react";
import { api, type RouterInputs, type RouterOutputs } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { buildSignInHref } from "~/lib/auth-navigation";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";

type WalletProvider = RouterInputs["wallet"]["create"]["provider"];
type ManagedWallet = RouterOutputs["wallet"]["getMyWallets"][number];

const providers = [
  "PERA",
  "DEFLY",
  "ALGOSIGNER",
  "WALLETCONNECT",
  "OTHER",
] as const satisfies readonly WalletProvider[];

function isWalletProvider(value: string): value is WalletProvider {
  return providers.some((candidate) => candidate === value);
}

function shortenAddress(address: string): string {
  return address.length <= 18
    ? address
    : `${address.slice(0, 8)}...${address.slice(-6)}`;
}

export default function WalletSettingsPage() {
  const { data: session, status: sessionStatus } = useSession();
  const [address, setAddress] = useState("");
  const [label, setLabel] = useState("");
  const [provider, setProvider] = useState<WalletProvider>("PERA");
  const [formError, setFormError] = useState<string | null>(null);

  const walletsQuery = api.wallet.getMyWallets.useQuery(undefined, {
    enabled: Boolean(session),
  });
  const custodyQuery = api.azoaCustodialAccount.getStatus.useQuery(undefined, {
    enabled: Boolean(session),
    retry: false,
  });
  const wallets = walletsQuery.data ?? [];

  const createWallet = api.wallet.create.useMutation({
    onSuccess: () => {
      setAddress("");
      setLabel("");
      setFormError(null);
      void walletsQuery.refetch();
    },
    onError: (error) => setFormError(error.message),
  });
  const setPrimary = api.wallet.setPrimary.useMutation({
    onSuccess: () => void walletsQuery.refetch(),
  });
  const deleteWallet = api.wallet.delete.useMutation({
    onSuccess: () => void walletsQuery.refetch(),
  });

  function addWallet(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const normalizedAddress = address.trim();
    if (!normalizedAddress) {
      setFormError("Enter a public wallet address.");
      return;
    }

    createWallet.mutate({
      address: normalizedAddress,
      provider,
      label: label.trim() || undefined,
      isPrimary: wallets.length === 0,
    });
  }

  function removeWallet(wallet: ManagedWallet) {
    if (
      window.confirm(
        `Remove ${wallet.label || shortenAddress(wallet.address)} from your wallet list?`,
      )
    ) {
      deleteWallet.mutate({ id: wallet.id });
    }
  }

  if (sessionStatus === "loading") {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="text-system size-8 animate-spin" />
      </div>
    );
  }

  if (!session) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center p-4">
        <Card className="border-primary/50 max-w-sm">
          <CardContent className="space-y-3 pt-6 text-center">
            <p className="text-primary font-mono">
              Sign in to manage external wallets.
            </p>
            <Button asChild variant="outline">
              <Link href={buildSignInHref("/settings/wallets")}>SIGN IN</Link>
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-3xl space-y-8 py-8">
      <div className="border-border space-y-2 border-b-2 pb-6">
        <div className="flex items-center gap-3">
          <WalletCards className="text-system size-7" />
          <h1 className="font-mono text-2xl font-bold tracking-tight uppercase">
            External Wallets
          </h1>
        </div>
        <p className="text-muted-foreground text-sm">
          Keep track of public addresses you control. ArdaNova never asks for a
          private key, mnemonic, or custody access.
        </p>
      </div>

      <Card className="border-system/20 bg-system/5">
        <CardContent className="space-y-3 pt-6">
          <div className="flex items-start gap-3">
            <LockKeyhole className="text-system mt-0.5 size-5 shrink-0" />
            <div>
              <p className="font-mono text-sm font-semibold">Safety status</p>
              <p className="text-muted-foreground text-sm">
                Adding an address does not move assets, prove ownership, create
                a payout account, or enable export. Ownership verification
                requires a one-time signed proof through a supported browser
                wallet.
              </p>
            </div>
          </div>
          {custodyQuery.data?.ready ? (
            <div className="text-success flex items-center gap-2 text-xs">
              <CheckCircle2 className="size-3.5" />
              Your Azoa-managed wallet is ready. External addresses listed here
              remain non-custodial and require their own ownership proof.
            </div>
          ) : (
            <div className="text-muted-foreground flex items-center gap-2 text-xs">
              <CircleAlert className="size-3.5" />
              Azoa account setup and identity verification are managed from your
              profile. This external-wallet list never exports custody keys.
            </div>
          )}
        </CardContent>
      </Card>

      <section className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="font-mono text-sm font-bold tracking-widest uppercase">
            Your wallet list
          </h2>
          <Badge variant="outline">{wallets.length} saved</Badge>
        </div>

        {walletsQuery.isLoading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="text-system size-6 animate-spin" />
          </div>
        ) : walletsQuery.error ? (
          <Card className="border-destructive/50">
            <CardContent className="text-destructive pt-6 text-sm">
              Unable to load wallets. Refresh and try again.
            </CardContent>
          </Card>
        ) : wallets.length === 0 ? (
          <Card className="border-dashed">
            <CardContent className="text-muted-foreground pt-6 text-sm">
              No external wallet addresses saved yet.
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-3">
            {wallets.map((wallet) => (
              <Card key={wallet.id} className="border-border">
                <CardContent className="flex flex-col gap-4 pt-5 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-mono text-sm font-semibold">
                        {wallet.label || "External wallet"}
                      </p>
                      {wallet.isPrimary && (
                        <Badge variant="success">Primary</Badge>
                      )}
                      <Badge
                        variant={wallet.isVerified ? "success" : "secondary"}
                      >
                        {wallet.isVerified
                          ? "Proof verified"
                          : "Proof required"}
                      </Badge>
                    </div>
                    <p
                      className="text-muted-foreground font-mono text-xs"
                      title={wallet.address}
                    >
                      {shortenAddress(wallet.address)}
                    </p>
                    <p className="text-muted-foreground text-xs">
                      {wallet.provider}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {!wallet.isPrimary && (
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={setPrimary.isPending}
                        onClick={() => setPrimary.mutate({ id: wallet.id })}
                      >
                        <Star className="mr-1.5 size-3.5" /> SET PRIMARY
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={deleteWallet.isPending}
                      onClick={() => removeWallet(wallet)}
                    >
                      <Trash2 className="mr-1.5 size-3.5" /> REMOVE
                    </Button>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </section>

      <Card>
        <CardContent className="pt-6">
          <form className="space-y-4" onSubmit={addWallet}>
            <div className="flex items-center gap-2">
              <Plus className="text-system size-4" />
              <h2 className="font-mono text-sm font-bold tracking-widest uppercase">
                Add a public address
              </h2>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="wallet-address">Public address</Label>
                <Input
                  id="wallet-address"
                  value={address}
                  onChange={(event) => setAddress(event.target.value)}
                  placeholder="Your Algorand public address"
                  autoComplete="off"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="wallet-label">Label (optional)</Label>
                <Input
                  id="wallet-label"
                  value={label}
                  onChange={(event) => setLabel(event.target.value)}
                  placeholder="Personal wallet"
                  maxLength={80}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="wallet-provider">Wallet provider</Label>
                <Select
                  value={provider}
                  onValueChange={(value) => {
                    if (isWalletProvider(value)) setProvider(value);
                  }}
                >
                  <SelectTrigger id="wallet-provider">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {providers.map((item) => (
                      <SelectItem key={item} value={item}>
                        {item}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            {formError && (
              <p className="text-destructive text-sm">{formError}</p>
            )}
            <Button
              type="submit"
              variant="default"
              disabled={createWallet.isPending}
            >
              <Plus className="mr-2 size-4" />
              {createWallet.isPending ? "SAVING..." : "SAVE PUBLIC ADDRESS"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card className="border-border/70">
        <CardContent className="flex flex-col gap-3 pt-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="font-mono text-sm font-semibold">
              Fund a project or review your holdings
            </p>
            <p className="text-muted-foreground text-sm">
              Funding occurs through a project&apos;s server-issued checkout. A
              redirect alone is never a payment or allocation receipt.
            </p>
          </div>
          <Button asChild variant="outline">
            <Link href="/projects">
              BROWSE PROJECTS <ExternalLink className="ml-2 size-4" />
            </Link>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

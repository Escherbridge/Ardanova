"use client";

import { useState } from "react";
import { useSession } from "next-auth/react";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "~/components/ui/dialog";
import { Input } from "~/components/ui/input";
import { Loader2, AlertCircle, CheckCircle2, XCircle, Shield } from "lucide-react";
import { toast } from "sonner";
import { cn } from "~/lib/utils";

import SupplyBreakdownBar from "~/components/equity/supply-breakdown-bar";
import AllocationTable from "~/components/equity/allocation-table";

interface EquityTabProps {
  projectId: string;
  isOwner: boolean;
}

export default function EquityTab({ projectId, isOwner }: EquityTabProps) {
  const { data: configRaw, isLoading: configLoading, error: configError } =
    api.projectTokens.getConfigByProject.useQuery({ projectId });
  const config = configRaw as unknown as TokenConfig | undefined;

  if (configLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="size-6 animate-spin text-primary" />
      </div>
    );
  }

  if (configError ?? !config) {
    return (
      <div className="border-2 border-destructive/40 bg-destructive/5 p-6 flex items-start gap-3">
        <AlertCircle className="size-5 text-destructive shrink-0 mt-0.5" />
        <div>
          <p className="font-mono text-sm font-bold text-destructive">TOKEN CONFIG NOT FOUND</p>
          <p className="text-xs text-muted-foreground mt-1">
            No token configuration exists for this project.
          </p>
        </div>
      </div>
    );
  }

  return <EquityTabContent config={config} isOwner={isOwner} />;
}

type TokenConfig = {
  id: string;
  projectId: string;
  totalSupply: number;
  fundingGoal: number;
  fundingRaised: number;
  unitName: string;
  assetName: string;
  contributorSupply: number;
  investorSupply: number;
  founderSupply: number;
  burnedSupply: number;
  status: string;
  gateStatus: string;
};

type GateStatus = "FUNDING" | "ACTIVE" | "SUCCEEDED" | "FAILED";

const gateStatusConfig: Record<GateStatus, { label: string; color: string; bg: string; border: string }> = {
  FUNDING: { label: "FUNDING", color: "text-[#00d4ff]", bg: "bg-[#00d4ff]/10", border: "border-[#00d4ff]/40" },
  ACTIVE: { label: "ACTIVE", color: "text-[#00ff88]", bg: "bg-[#00ff88]/10", border: "border-[#00ff88]/40" },
  SUCCEEDED: { label: "SUCCEEDED", color: "text-[#00ff88]", bg: "bg-[#00ff88]/10", border: "border-[#00ff88]/40" },
  FAILED: { label: "FAILED", color: "text-[#ff0080]", bg: "bg-[#ff0080]/10", border: "border-[#ff0080]/40" },
};

function EquityTabContent({
  config,
  isOwner,
}: {
  config: TokenConfig;
  isOwner: boolean;
}) {
  const { data: session } = useSession();
  const gateStatus = config.gateStatus as GateStatus;
  const gsc = gateStatusConfig[gateStatus] ?? gateStatusConfig.FUNDING;

  const { data: supplyRaw, isLoading: supplyLoading } =
    api.projectTokens.getSupply.useQuery({ id: config.id });
  const supply = supplyRaw as unknown as { totalSupply: number; contributorSupply: number; investorSupply: number; founderSupply: number; burnedSupply: number } | undefined;

  const { data: allocationsRaw, isLoading: allocLoading } =
    api.projectTokens.getAllocations.useQuery({ configId: config.id });
  const allocations = allocationsRaw as unknown as { id: string; taskId?: string; userId?: string; equityPercentage: number; tokenAmount: number; status: string; holderClass: string; description?: string }[] | undefined;

  const clearGateMutation = api.projectTokens.clearGate.useMutation({
    onSuccess: () => {
      toast.success("Gate cleared successfully");
    },
    onError: (err) => {
      toast.error(`Failed to clear gate: ${err.message}`);
    },
  });

  const failProjectMutation = api.projectTokens.failProject.useMutation({
    onSuccess: () => {
      toast.success("Project marked as failed");
    },
    onError: (err) => {
      toast.error(`Failed to update project: ${err.message}`);
    },
  });

  const [failConfirmText, setFailConfirmText] = useState("");
  const [failDialogOpen, setFailDialogOpen] = useState(false);
  const [succeedDialogOpen, setSucceedDialogOpen] = useState(false);

  const handleClearGate = () => {
    if (!session?.user?.id) return;
    clearGateMutation.mutate({ configId: config.id, verifiedByUserId: session.user.id });
    setSucceedDialogOpen(false);
  };

  const handleFailProject = () => {
    if (failConfirmText !== "FAIL PROJECT") return;
    failProjectMutation.mutate({ configId: config.id, reason: "Manually failed by owner" });
    setFailDialogOpen(false);
  };

  return (
    <div className="space-y-6">
      {/* Supply Breakdown */}
      <Card className="bg-card border-2 border-border">
        <CardHeader className="pb-2">
          <CardTitle className="text-base font-mono tracking-wide">TOKEN SUPPLY</CardTitle>
        </CardHeader>
        <CardContent>
          {supplyLoading ? (
            <div className="flex items-center gap-2 py-6 justify-center">
              <Loader2 className="size-4 animate-spin text-muted-foreground" />
            </div>
          ) : supply ? (
            <SupplyBreakdownBar
              totalSupply={supply.totalSupply}
              contributorSupply={supply.contributorSupply}
              investorSupply={supply.investorSupply}
              founderSupply={supply.founderSupply}
              burnedSupply={supply.burnedSupply}
              unitName={config.unitName}
            />
          ) : (
            <p className="text-xs text-muted-foreground">Supply data unavailable.</p>
          )}
        </CardContent>
      </Card>

      {/* Gate Management (owner only) */}
      {isOwner && (
        <Card className="bg-card border-2 border-border">
          <CardHeader className="pb-2">
            <CardTitle className="text-base font-mono tracking-wide flex items-center gap-2">
              <Shield className="size-4" />
              GATE MANAGEMENT
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Current gate status */}
            <div className="flex items-center gap-3">
              <span className="text-xs text-muted-foreground">Current status:</span>
              <span
                className={cn(
                  "font-mono text-xs font-bold border px-2 py-0.5",
                  gsc.color,
                  gsc.bg,
                  gsc.border,
                )}
              >
                {gsc.label}
              </span>
            </div>

            {/* Actions */}
            {(gateStatus === "FUNDING" || gateStatus === "ACTIVE") && (
              <div className="flex gap-3 flex-wrap">
                {/* Mark Succeeded */}
                <Dialog open={succeedDialogOpen} onOpenChange={setSucceedDialogOpen}>
                  <DialogTrigger asChild>
                    <Button
                      variant="outline"
                      size="sm"
                      className="border-[#00ff88]/40 text-[#00ff88] hover:bg-[#00ff88]/10 hover:border-[#00ff88] font-mono"
                    >
                      <CheckCircle2 className="size-4 mr-2" />
                      MARK SUCCEEDED
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle className="font-mono">Clear Gate</DialogTitle>
                      <DialogDescription>
                        This will advance the project to the next gate status. This action cannot be undone.
                      </DialogDescription>
                    </DialogHeader>
                    <div className="py-4">
                      <p className="text-sm text-muted-foreground">
                        Current status: <span className="font-mono font-bold text-[#00d4ff]">{gateStatus}</span>
                      </p>
                      <p className="text-sm text-muted-foreground mt-1">
                        Next status: <span className="font-mono font-bold text-[#00ff88]">
                          {gateStatus === "FUNDING" ? "ACTIVE" : "SUCCEEDED"}
                        </span>
                      </p>
                    </div>
                    <DialogFooter>
                      <Button variant="outline" onClick={() => setSucceedDialogOpen(false)}>
                        Cancel
                      </Button>
                      <Button
                        onClick={handleClearGate}
                        disabled={clearGateMutation.isPending}
                        className="bg-[#00ff88] text-background hover:bg-[#00ff88]/80 font-mono"
                      >
                        {clearGateMutation.isPending ? (
                          <Loader2 className="size-4 animate-spin mr-2" />
                        ) : (
                          <CheckCircle2 className="size-4 mr-2" />
                        )}
                        CONFIRM CLEAR GATE
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>

                {/* Fail Project */}
                <Dialog open={failDialogOpen} onOpenChange={setFailDialogOpen}>
                  <DialogTrigger asChild>
                    <Button
                      variant="outline"
                      size="sm"
                      className="border-[#ff0080]/40 text-[#ff0080] hover:bg-[#ff0080]/10 hover:border-[#ff0080] font-mono"
                    >
                      <XCircle className="size-4 mr-2" />
                      FAIL PROJECT
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle className="font-mono text-[#ff0080]">Fail Project</DialogTitle>
                      <DialogDescription>
                        This will permanently mark the project as failed. All investors will be refunded. This cannot be undone.
                      </DialogDescription>
                    </DialogHeader>
                    <div className="py-4 space-y-3">
                      <p className="text-sm text-muted-foreground">
                        Type <span className="font-mono font-bold text-[#ff0080]">FAIL PROJECT</span> to confirm:
                      </p>
                      <Input
                        value={failConfirmText}
                        onChange={(e) => setFailConfirmText(e.target.value)}
                        placeholder="FAIL PROJECT"
                        className="font-mono border-2 border-[#ff0080]/40 focus:border-[#ff0080]"
                      />
                    </div>
                    <DialogFooter>
                      <Button variant="outline" onClick={() => setFailDialogOpen(false)}>
                        Cancel
                      </Button>
                      <Button
                        onClick={handleFailProject}
                        disabled={failConfirmText !== "FAIL PROJECT" || failProjectMutation.isPending}
                        className="bg-[#ff0080] text-background hover:bg-[#ff0080]/80 font-mono"
                      >
                        {failProjectMutation.isPending ? (
                          <Loader2 className="size-4 animate-spin mr-2" />
                        ) : (
                          <XCircle className="size-4 mr-2" />
                        )}
                        CONFIRM FAIL
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
              </div>
            )}

            {(gateStatus === "SUCCEEDED" || gateStatus === "FAILED") && (
              <p className="text-xs text-muted-foreground">
                {gateStatus === "SUCCEEDED"
                  ? "Project has successfully completed all gates."
                  : "Project has been marked as failed. No further actions available."}
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {/* Allocation Table */}
      <Card className="bg-card border-2 border-border">
        <CardHeader className="pb-2">
          <CardTitle className="text-base font-mono tracking-wide">ALL ALLOCATIONS</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {allocLoading ? (
            <div className="flex items-center gap-2 py-10 justify-center">
              <Loader2 className="size-4 animate-spin text-muted-foreground" />
              <span className="text-sm text-muted-foreground">Loading allocations...</span>
            </div>
          ) : (
            <AllocationTable allocations={allocations ?? []} />
          )}
        </CardContent>
      </Card>
    </div>
  );
}

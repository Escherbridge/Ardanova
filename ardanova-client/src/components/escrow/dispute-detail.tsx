import { AlertTriangle, LockKeyhole, Scale } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { EscrowStatusBadge } from "./escrow-status-badge";
import type { EscrowData } from "./escrow-detail-card";

interface DisputeDetailProps {
  escrow: EscrowData;
}

export function DisputeDetail({ escrow }: DisputeDetailProps) {
  const disputedDate = escrow.disputedAt
    ? new Date(escrow.disputedAt).toLocaleDateString("en-US", {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "Not exposed by the current API";

  return (
    <Card className="border-neon-pink/20 border-2">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between gap-3">
          <CardTitle className="flex items-center gap-2 text-base">
            <AlertTriangle className="text-neon-pink h-5 w-5" />
            Active Dispute
          </CardTitle>
          <EscrowStatusBadge status={escrow.status} />
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid gap-3 rounded-md border border-white/10 bg-white/5 p-3 text-sm sm:grid-cols-2">
          <div>
            <p className="text-muted-foreground text-xs">Escrow ID</p>
            <p className="font-mono text-xs break-all">
              {escrow.id.slice(0, 16)}...
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Amount</p>
            <p className="text-neon-pink font-mono font-bold">
              {escrow.amount.toLocaleString("en-US", {
                minimumFractionDigits: 2,
                maximumFractionDigits: 6,
              })}
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Task</p>
            <p className="font-mono text-xs break-all">
              {escrow.taskId.slice(0, 16)}...
            </p>
          </div>
          <div>
            <p className="text-muted-foreground text-xs">Disputed at</p>
            <p className="text-xs">{disputedDate}</p>
          </div>
        </div>

        <div
          className="border-neon-yellow/30 bg-neon-yellow/5 space-y-3 rounded-md border-2 p-4"
          role="status"
        >
          <div className="flex items-start gap-3">
            <LockKeyhole className="text-neon-yellow mt-0.5 h-5 w-5 shrink-0" />
            <div className="space-y-1">
              <p className="font-medium">Resolution controls unavailable</p>
              <p className="text-muted-foreground text-sm">
                The backend does not yet expose an administrator adjudication
                contract. Its existing release and refund operations are
                funder-only actions, so this review remains read-only and no
                funds can move from this screen.
              </p>
            </div>
          </div>
          <div className="border-neon-yellow/20 text-muted-foreground flex items-start gap-3 border-t pt-3 text-sm">
            <Scale className="mt-0.5 h-4 w-4 shrink-0" />
            <p>
              A future resolution flow must record administrator authority, the
              decision, supporting evidence, and the resulting escrow transition
              as one auditable backend operation.
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

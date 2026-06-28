"use client";

import { useState } from "react";
import { AlertTriangle, Loader2, AlertCircle, SendHorizonal } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Textarea } from "~/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";
import { api } from "~/trpc/react";

type DisputeReason =
  | "SCOPE_DISPUTE"
  | "QUALITY_ISSUE"
  | "NON_DELIVERY"
  | "OTHER";

const DISPUTE_REASON_LABELS: Record<DisputeReason, string> = {
  SCOPE_DISPUTE: "Scope Dispute — work exceeded agreed scope",
  QUALITY_ISSUE: "Quality Issue — deliverables below agreed standard",
  NON_DELIVERY: "Non-Delivery — work was not delivered",
  OTHER: "Other — describe in notes below",
};

interface DisputeFormProps {
  escrowId: string;
  onSubmit: () => void;
}

export function DisputeForm({ escrowId, onSubmit }: DisputeFormProps) {
  const [reason, setReason] = useState<DisputeReason | "">("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState<string | null>(null);

  const utils = api.useUtils();
  const disputeMutation = api.taskEscrow.dispute.useMutation({
    onSuccess: () => {
      void utils.taskEscrow.getByTaskId.invalidate();
      void utils.taskEscrow.getByFunderId.invalidate();
      setReason("");
      setDescription("");
      setError(null);
      onSubmit();
    },
    onError: (err) => {
      setError(err.message ?? "Failed to raise dispute. Please try again.");
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason) {
      setError("Please select a dispute reason.");
      return;
    }
    if (description.trim().length < 20) {
      setError("Please provide a description of at least 20 characters.");
      return;
    }
    setError(null);
    disputeMutation.mutate({ id: escrowId });
  };

  const isValid = reason !== "" && description.trim().length >= 20;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="flex items-center gap-2 rounded-md border border-neon-pink/30 bg-neon-pink/5 p-3">
        <AlertTriangle className="h-4 w-4 shrink-0 text-neon-pink" />
        <p className="text-sm text-neon-pink">
          Raising a dispute will freeze funds until resolution. Use this only if
          you cannot resolve the issue directly.
        </p>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium">Dispute Reason *</label>
        <Select
          value={reason}
          onValueChange={(val) => setReason(val as DisputeReason)}
          disabled={disputeMutation.isPending}
        >
          <SelectTrigger>
            <SelectValue placeholder="Select a reason..." />
          </SelectTrigger>
          <SelectContent>
            {(Object.keys(DISPUTE_REASON_LABELS) as DisputeReason[]).map(
              (key) => (
                <SelectItem key={key} value={key}>
                  {DISPUTE_REASON_LABELS[key]}
                </SelectItem>
              )
            )}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium">
          Description *{" "}
          <span className="text-xs text-muted-foreground">
            ({description.trim().length}/20 min)
          </span>
        </label>
        <Textarea
          placeholder="Describe the issue in detail. Include relevant dates, deliverable references, and what resolution you are seeking."
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          className="min-h-[120px] resize-none"
          disabled={disputeMutation.isPending}
        />
      </div>

      {error && (
        <div className="flex items-start gap-2 rounded-md border border-destructive/50 bg-destructive/10 p-3">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-destructive" />
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      <Button
        type="submit"
        variant="destructive"
        disabled={!isValid || disputeMutation.isPending}
        className="flex w-full items-center gap-2"
      >
        {disputeMutation.isPending ? (
          <Loader2 className="h-4 w-4 animate-spin" />
        ) : (
          <SendHorizonal className="h-4 w-4" />
        )}
        Submit Dispute
      </Button>
    </form>
  );
}

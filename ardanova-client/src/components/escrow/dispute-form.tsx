"use client";

import { useState } from "react";
import {
  AlertTriangle,
  Loader2,
  AlertCircle,
  SendHorizonal,
} from "lucide-react";
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
    if (description.trim().length > 4000) {
      setError("Please keep the dispute description within 4,000 characters.");
      return;
    }
    setError(null);
    disputeMutation.mutate({
      id: escrowId,
      reason,
      description: description.trim(),
    });
  };

  const descriptionLength = description.trim().length;
  const isValid =
    reason !== "" && descriptionLength >= 20 && descriptionLength <= 4000;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="border-destructive bg-destructive/10 flex items-center gap-2 border p-3">
        <AlertTriangle
          className="text-destructive h-4 w-4 shrink-0"
          aria-hidden="true"
        />
        <p id="dispute-warning" className="text-destructive text-sm">
          Raising a dispute will freeze funds until resolution. Use this only if
          you cannot resolve the issue directly.
        </p>
      </div>

      <div className="space-y-2">
        <label htmlFor="dispute-reason" className="text-sm font-medium">
          Dispute Reason *
        </label>
        <Select
          value={reason}
          onValueChange={(val) => setReason(val as DisputeReason)}
          disabled={disputeMutation.isPending}
        >
          <SelectTrigger id="dispute-reason" aria-describedby="dispute-warning">
            <SelectValue placeholder="Select a reason..." />
          </SelectTrigger>
          <SelectContent>
            {(Object.keys(DISPUTE_REASON_LABELS) as DisputeReason[]).map(
              (key) => (
                <SelectItem key={key} value={key}>
                  {DISPUTE_REASON_LABELS[key]}
                </SelectItem>
              ),
            )}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-2">
        <label htmlFor="dispute-description" className="text-sm font-medium">
          Description *{" "}
          <span
            id="dispute-description-requirement"
            className="text-muted-foreground text-xs"
          >
            ({descriptionLength}/4000; 20 minimum)
          </span>
        </label>
        <Textarea
          id="dispute-description"
          placeholder="Describe the issue in detail. Include relevant dates, deliverable references, and what resolution you are seeking."
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          className="min-h-[120px] resize-none"
          disabled={disputeMutation.isPending}
          aria-describedby="dispute-description-requirement dispute-warning"
          maxLength={4000}
        />
      </div>

      {error && (
        <div
          className="border-destructive bg-destructive/10 flex items-start gap-2 border p-3"
          role="alert"
        >
          <AlertCircle
            className="text-destructive mt-0.5 h-4 w-4 shrink-0"
            aria-hidden="true"
          />
          <p className="text-destructive text-sm">{error}</p>
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

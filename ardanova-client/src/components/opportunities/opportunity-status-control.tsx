"use client";

import { describeMutationError } from "~/components/ui/query-error-copy";

import { useRouter } from "next/navigation";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";

import { Button } from "~/components/ui/button";
import { Card, CardContent } from "~/components/ui/card";
import { api } from "~/trpc/react";

interface OpportunityStatusControlProps {
  opportunityId: string;
}

const statusCopy: Record<string, string> = {
  DRAFT: "Draft: not accepting applications. Only your team can see it.",
  OPEN: "Open: anyone who can see this role can apply.",
  CLOSED: "Closed: no new applications.",
  FILLED: "Filled: no new applications.",
};

/** Lets the role's managers open, pause, or close applications. */
export function OpportunityStatusControl({
  opportunityId,
}: OpportunityStatusControlProps) {
  const router = useRouter();
  const utils = api.useUtils();
  const opportunity = api.opportunity.getById.useQuery({ id: opportunityId });
  const refreshOnChange = {
    onSuccess: () => {
      void utils.opportunity.getById.invalidate({ id: opportunityId });
      router.refresh();
    },
    onError: (error: { message: string }) => toast.error(describeMutationError(error, "update the opportunity")),
  };
  const update = api.opportunity.update.useMutation(refreshOnChange);
  const close = api.opportunity.close.useMutation(refreshOnChange);
  const isPending = update.isPending || close.isPending;
  const status = opportunity.data?.status ?? undefined;

  if (!status) return null;

  return (
    <Card className="bg-card border-border">
      <CardContent className="flex flex-wrap items-center justify-between gap-3 py-4">
        <p className="text-sm" data-testid="opportunity-status-copy">
          {statusCopy[status] ?? status}
        </p>
        <div className="flex gap-2">
          {status !== "OPEN" ? (
            <Button
              size="sm"
              disabled={isPending}
              onClick={() => update.mutate({ id: opportunityId, status: "OPEN" })}
            >
              {update.isPending ? <Loader2 className="animate-spin" /> : null}
              Open for applications
            </Button>
          ) : (
            <>
              <Button
                size="sm"
                variant="outline"
                disabled={isPending}
                onClick={() =>
                  update.mutate({ id: opportunityId, status: "DRAFT" })
                }
              >
                Pause
              </Button>
              <Button
                size="sm"
                variant="outline"
                disabled={isPending}
                onClick={() => close.mutate({ id: opportunityId })}
              >
                {close.isPending ? <Loader2 className="animate-spin" /> : null}
                Close
              </Button>
            </>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

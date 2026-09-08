import { Badge } from "~/components/ui/badge";

interface TaskEconomicStateProps {
  allocationUnits?: number | null;
  escrowStatus?: string | null;
}

const stateByEscrowStatus: Record<
  string,
  {
    label: string;
    detail: string;
    variant: "secondary" | "warning" | "neon" | "destructive";
  }
> = {
  NONE: {
    label: "NOT RESERVED",
    detail: "No units have been set aside for this award yet.",
    variant: "secondary",
  },
  FUNDED: {
    label: "RESERVED",
    detail: "The award's units are set aside and wait for an approved release.",
    variant: "warning",
  },
  RELEASED: {
    label: "PAID",
    detail: "The award was released to the contributor.",
    variant: "neon",
  },
  DISPUTED: {
    label: "DISPUTED",
    detail: "This award is disputed; no payout should be assumed until it is resolved.",
    variant: "destructive",
  },
  REFUNDED: {
    label: "REFUNDED",
    detail: "The reserved units were returned; no payout was made on this award.",
    variant: "secondary",
  },
};

export function TaskEconomicState({
  allocationUnits,
  escrowStatus,
}: TaskEconomicStateProps) {
  if (!allocationUnits || allocationUnits <= 0) return null;

  const state =
    stateByEscrowStatus[(escrowStatus ?? "NONE").toUpperCase()] ??
    stateByEscrowStatus.NONE;
  return (
    <Badge
      title={state.detail}
      variant={state.variant}
      size="sm"
      className="text-[10px]"
    >
      {state.label}
    </Badge>
  );
}

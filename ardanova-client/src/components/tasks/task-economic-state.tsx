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
    label: "ALLOCATION UNFUNDED",
    detail: "This project-token allocation has not been escrowed yet.",
    variant: "secondary",
  },
  FUNDED: {
    label: "ALLOCATION ESCROWED",
    detail: "The allocation is funded and waits for an approved release.",
    variant: "warning",
  },
  RELEASED: {
    label: "SETTLEMENT PENDING",
    detail:
      "Release is recorded. Do not treat this as an on-node settlement until the allocation receipt is recorded.",
    variant: "neon",
  },
  DISPUTED: {
    label: "ALLOCATION DISPUTED",
    detail: "The escrow is disputed; no settlement should be assumed.",
    variant: "destructive",
  },
  REFUNDED: {
    label: "ALLOCATION RETURNED",
    detail: "The escrow records a return and no task allocation is settled.",
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

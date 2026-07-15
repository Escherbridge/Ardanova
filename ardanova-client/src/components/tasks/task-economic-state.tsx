import { Badge } from "~/components/ui/badge";

interface TaskEconomicStateProps {
  equityReward?: number | null;
  escrowStatus?: string | null;
}

const stateByEscrowStatus: Record<string, { label: string; detail: string; variant: "secondary" | "warning" | "neon" | "destructive" }> = {
  NONE: {
    label: "REWARD UNFUNDED",
    detail: "This equity reward has not been escrowed yet.",
    variant: "secondary",
  },
  FUNDED: {
    label: "REWARD ESCROWED",
    detail: "The reward is funded and waits for an approved release.",
    variant: "warning",
  },
  RELEASED: {
    label: "SETTLEMENT PENDING",
    detail: "Release is recorded. Do not treat this as an on-node settlement until the allocation receipt is recorded.",
    variant: "neon",
  },
  DISPUTED: {
    label: "REWARD DISPUTED",
    detail: "The escrow is disputed; no settlement should be assumed.",
    variant: "destructive",
  },
  REFUNDED: {
    label: "REWARD REFUNDED",
    detail: "The escrow was refunded and cannot be paid as a task reward.",
    variant: "secondary",
  },
};

export function TaskEconomicState({ equityReward, escrowStatus }: TaskEconomicStateProps) {
  if (!equityReward || equityReward <= 0) return null;

  const state = stateByEscrowStatus[(escrowStatus ?? "NONE").toUpperCase()] ?? stateByEscrowStatus.NONE;
  return (
    <Badge title={state.detail} variant={state.variant} size="sm" className="text-[10px]">
      {state.label}
    </Badge>
  );
}

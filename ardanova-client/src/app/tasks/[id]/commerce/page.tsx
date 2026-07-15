"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import { ArrowLeft, CheckSquare, CircleDollarSign, Loader2 } from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { TaskEconomicState } from "~/components/tasks/task-economic-state";
import { api } from "~/trpc/react";

export default function TaskCommercePage() {
  const params = useParams<{ id: string }>();
  const taskId = params.id;
  const { status } = useSession();
  const { data: commerce, isLoading, error } = api.task.getCommerce.useQuery(
    { id: taskId },
    { enabled: status === "authenticated" && Boolean(taskId) },
  );

  if (status === "loading" || isLoading) {
    return <LoadingState />;
  }

  if (status !== "authenticated") {
    return (
      <CommerceShell title="Sign in required">
        <p className="text-muted-foreground">Sign in to view this task's commerce status.</p>
      </CommerceShell>
    );
  }

  if (error || !commerce) {
    return (
      <CommerceShell title="Commerce task unavailable">
        <p className="text-muted-foreground">The task could not be loaded, or is no longer available to you.</p>
      </CommerceShell>
    );
  }

  const award = commerce.awardAmount;

  return (
    <CommerceShell title="Task commerce">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CheckSquare className="size-5 text-neon" />
            {commerce.title}
          </CardTitle>
          <p className="text-sm text-muted-foreground">{commerce.description ?? "No task description was supplied."}</p>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="rounded-lg border border-border bg-muted/30 p-4">
            <p className="text-sm font-medium">Accepted agreement</p>
            <p className="mt-1 text-sm text-muted-foreground">
              The local task agreement is accepted. It is not an escrow, token allocation, payment, or settlement receipt.
            </p>
          </div>
          <div className="flex items-center justify-between gap-3 rounded-lg border border-border p-4">
            <div>
              <p className="text-sm font-medium">Project-token award</p>
              <p className="text-sm text-muted-foreground">{award} {commerce.assetCode} units are reserved in the agreement.</p>
            </div>
            <TaskEconomicState equityReward={award} escrowStatus={commerce.escrowStatus} />
          </div>
          <div className="rounded-lg border border-warning/40 bg-warning/10 p-4 text-sm text-muted-foreground">
            Funding, escrow, quest linking, release approval, and AZOA settlement are separate gated steps. No value has moved from viewing this page.
          </div>
        </CardContent>
      </Card>
    </CommerceShell>
  );
}

function CommerceShell({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-8">
      <Button variant="ghost" asChild className="-ml-2">
        <Link href="/tasks"><ArrowLeft className="mr-2 size-4" />Back to tasks</Link>
      </Button>
      <div className="flex items-center gap-3">
        <div className="rounded-lg bg-neon/10 p-3"><CircleDollarSign className="size-6 text-neon" /></div>
        <h1 className="text-3xl font-bold">{title}</h1>
      </div>
      {children}
    </div>
  );
}

function LoadingState() {
  return (
    <div className="flex min-h-64 items-center justify-center">
      <Loader2 className="size-6 animate-spin text-neon" />
    </div>
  );
}

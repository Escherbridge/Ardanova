"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  AlertCircle,
  ArrowLeft,
  CheckSquare,
  CircleDollarSign,
  Loader2,
  ShieldCheck,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { EscrowDetailCard } from "~/components/escrow/escrow-detail-card";
import { EscrowTimeline } from "~/components/escrow/escrow-timeline";
import { ReleaseDialog } from "~/components/escrow/release-dialog";
import { DisputeForm } from "~/components/escrow/dispute-form";
import { api } from "~/trpc/react";

/**
 * One task, and the money set aside for it. This is the surface the escrow lifecycle is driven
 * from: opening the record, confirming the money really is set aside, releasing it to the
 * worker, disputing it, and asking for it back.
 */
export default function TaskDetailPage() {
  const params = useParams<{ id: string }>();
  const taskId = params.id;
  const { data: session, status: sessionStatus } = useSession();
  const viewerId = session?.user?.id;

  const [releaseOpen, setReleaseOpen] = useState(false);
  const [disputeOpen, setDisputeOpen] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const taskQuery = api.task.getById.useQuery(
    { id: taskId },
    { enabled: Boolean(taskId) },
  );
  const task = taskQuery.data;

  const projectQuery = api.project.getById.useQuery(
    { id: task?.projectId ?? "" },
    { enabled: Boolean(task?.projectId) },
  );

  const escrowQuery = api.taskEscrow.getByTaskId.useQuery(
    { taskId },
    {
      enabled: sessionStatus === "authenticated" && Boolean(taskId),
      retry: false,
    },
  );
  const escrow = escrowQuery.data ?? null;

  const utils = api.useUtils();
  const refreshEscrow = () => {
    void utils.taskEscrow.getByTaskId.invalidate({ taskId });
    void utils.taskEscrow.getByFunderId.invalidate();
    void utils.task.getById.invalidate({ id: taskId });
  };

  const createEscrow = api.taskEscrow.create.useMutation({
    onSuccess: () => {
      setActionError(null);
      refreshEscrow();
    },
    onError: (error) => setActionError(error.message),
  });

  const fundEscrow = api.taskEscrow.fund.useMutation({
    onSuccess: () => {
      setActionError(null);
      refreshEscrow();
    },
    onError: (error) => setActionError(error.message),
  });

  const refundEscrow = api.taskEscrow.refund.useMutation({
    onSuccess: () => {
      setActionError(null);
      setDisputeOpen(false);
      refreshEscrow();
    },
    onError: (error) => setActionError(error.message),
  });

  if (taskQuery.isLoading) {
    return (
      <TaskShell title="Task">
        <div className="flex min-h-40 items-center justify-center">
          <Loader2 className="text-neon size-6 animate-spin" />
        </div>
      </TaskShell>
    );
  }

  if (taskQuery.error || !task) {
    return (
      <TaskShell title="Task unavailable">
        <p className="text-muted-foreground">
          This task could not be loaded, or is no longer available to you.
        </p>
      </TaskShell>
    );
  }

  const isProjectOwner =
    Boolean(viewerId) && projectQuery.data?.createdById === viewerId;
  const isFunder = escrow !== null && escrow.funderId === viewerId;
  const workerName = task.assignedTo?.name ?? undefined;
  const isBusy =
    createEscrow.isPending || fundEscrow.isPending || refundEscrow.isPending;

  return (
    <TaskShell title={task.title}>
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CheckSquare className="text-neon size-5" />
            {task.title}
          </CardTitle>
          <p className="text-muted-foreground text-sm">
            {task.description ?? "No description was supplied for this task."}
          </p>
        </CardHeader>
        <CardContent className="text-muted-foreground space-y-2 text-sm">
          <p>
            Status: <span className="text-foreground">{task.status}</span>
          </p>
          {workerName && (
            <p>
              Being done by:{" "}
              <span className="text-foreground">{workerName}</span>
            </p>
          )}
          {projectQuery.data && (
            <p>
              Part of{" "}
              <Link
                href={`/projects/${projectQuery.data.slug}`}
                className="text-neon underline underline-offset-4"
              >
                {projectQuery.data.title}
              </Link>
            </p>
          )}
        </CardContent>
      </Card>

      {actionError && (
        <div
          className="border-destructive bg-destructive/10 flex items-start gap-2 border p-3"
          role="alert"
        >
          <AlertCircle
            className="text-destructive mt-0.5 h-4 w-4 shrink-0"
            aria-hidden="true"
          />
          <p className="text-destructive text-sm">{actionError}</p>
        </div>
      )}

      {sessionStatus !== "authenticated" && (
        <Card className="border-2 border-white/10">
          <CardContent className="text-muted-foreground py-6 text-sm">
            Sign in to see whether money is set aside for this work.
          </CardContent>
        </Card>
      )}

      {sessionStatus === "authenticated" && escrowQuery.isLoading && (
        <div className="flex min-h-24 items-center justify-center">
          <Loader2 className="text-neon size-5 animate-spin" />
        </div>
      )}

      {sessionStatus === "authenticated" && !escrowQuery.isLoading && !escrow && (
        <Card className="border-2 border-white/10">
          <CardHeader className="pb-2">
            <CardTitle className="flex items-center gap-2 text-base">
              <CircleDollarSign className="text-muted-foreground size-5" />
              No money is set aside for this work yet
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            {isProjectOwner ? (
              <>
                <p className="text-muted-foreground">
                  Opening this record takes the agreed figure from the accepted
                  assignment — you are not asked to type an amount. It starts as
                  a record only; the money counts as set aside once the platform
                  confirms it.
                </p>
                <Button
                  type="button"
                  variant="neon"
                  onClick={() => {
                    setActionError(null);
                    createEscrow.mutate({ taskId });
                  }}
                  disabled={isBusy}
                  className="flex min-h-11 items-center gap-2"
                >
                  {createEscrow.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <ShieldCheck className="h-4 w-4" />
                  )}
                  Set money aside for this work
                </Button>
              </>
            ) : (
              <p className="text-muted-foreground">
                Only the person who created this project can set money aside for
                its work, and only they can see that record.
              </p>
            )}
          </CardContent>
        </Card>
      )}

      {escrow && (
        <div className="grid gap-6 lg:grid-cols-2">
          <EscrowDetailCard
            escrow={escrow}
            taskTitle={task.title}
            isFunder={isFunder}
            isLoading={isBusy}
            onFund={() => {
              setActionError(null);
              fundEscrow.mutate({ id: escrow.id });
            }}
            onRelease={() => {
              setActionError(null);
              setReleaseOpen(true);
            }}
            onDispute={() => {
              setActionError(null);
              setDisputeOpen((open) => !open);
            }}
            onRefund={() => {
              setActionError(null);
              refundEscrow.mutate({ id: escrow.id });
            }}
          />
          <EscrowTimeline
            escrow={{
              createdAt: escrow.createdAt,
              fundedAt: escrow.fundedAt,
              releasedAt: escrow.releasedAt,
              refundedAt: escrow.refundedAt,
              disputedAt: escrow.disputedAt,
              status: escrow.status,
            }}
          />
        </div>
      )}

      {escrow && isFunder && disputeOpen && escrow.status === "FUNDED" && (
        <Card className="border-destructive/30 border-2">
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Raise a dispute</CardTitle>
          </CardHeader>
          <CardContent>
            <DisputeForm
              escrowId={escrow.id}
              onSubmit={() => {
                setDisputeOpen(false);
                refreshEscrow();
              }}
            />
          </CardContent>
        </Card>
      )}

      {escrow && (
        <ReleaseDialog
          escrowId={escrow.id}
          amount={escrow.amount}
          recipientName={workerName}
          open={releaseOpen}
          onOpenChange={setReleaseOpen}
          onConfirm={refreshEscrow}
        />
      )}
    </TaskShell>
  );
}

function TaskShell({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div className="mx-auto max-w-4xl space-y-6 px-4 py-8">
      <Button variant="ghost" asChild className="-ml-2">
        <Link href="/tasks">
          <ArrowLeft className="mr-2 size-4" />
          Back to tasks
        </Link>
      </Button>
      <h1 className="text-3xl font-bold">{title}</h1>
      {children}
    </div>
  );
}

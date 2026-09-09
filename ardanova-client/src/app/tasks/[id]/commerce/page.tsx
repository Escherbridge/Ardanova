"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useSession } from "next-auth/react";
import {
  ArrowLeft,
  CheckSquare,
  CircleDollarSign,
  Loader2,
  Users,
} from "lucide-react";
import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { TaskEconomicState } from "~/components/tasks/task-economic-state";
import { api } from "~/trpc/react";

export default function TaskCommercePage() {
  const params = useParams<{ id: string }>();
  const taskId = params.id;
  const { status } = useSession();
  const utils = api.useUtils();
  const isSignedIn = status === "authenticated" && Boolean(taskId);

  const {
    data: commerce,
    isLoading,
    error,
  } = api.task.getCommerce.useQuery({ id: taskId }, { enabled: isSignedIn });

  // Who the viewer is, and what they may do about this award, is decided by the server. The
  // panel below is only a render of that answer: the control that admits an approval is
  // ProjectTokenService.ApproveWorkCompletionAsync.
  const approvalQuery = api.projectTokens.getTaskWorkApproval.useQuery(
    { taskId },
    { enabled: isSignedIn },
  );

  const approve = api.projectTokens.approveTaskWork.useMutation({
    onSuccess: () => {
      void utils.projectTokens.getTaskWorkApproval.invalidate({ taskId });
      void utils.task.getCommerce.invalidate({ id: taskId });
    },
  });

  if (status === "loading" || isLoading) {
    return <LoadingState />;
  }

  if (status !== "authenticated") {
    return (
      <CommerceShell title="Sign in required">
        <p className="text-muted-foreground">
          Sign in to view this task&apos;s commerce status.
        </p>
      </CommerceShell>
    );
  }

  if (error || !commerce) {
    return (
      <CommerceShell title="Commerce task unavailable">
        <p className="text-muted-foreground">
          The task could not be loaded, or is no longer available to you.
        </p>
      </CommerceShell>
    );
  }

  const award = commerce.awardAmount;
  const approval = approvalQuery.data;

  return (
    <CommerceShell title="Task commerce">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CheckSquare className="text-neon size-5" />
            {commerce.title}
          </CardTitle>
          <p className="text-muted-foreground text-sm">
            {commerce.description ?? "No task description was supplied."}
          </p>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="border-border bg-muted/30 rounded-lg border p-4">
            <p className="text-sm font-medium">Accepted agreement</p>
            <p className="text-muted-foreground mt-1 text-sm">
              The agreement is accepted. Viewing this page moves nothing —
              the status below is the only record of whether units have
              actually been reserved, paid, or disputed.
            </p>
          </div>
          <div className="border-border flex items-center justify-between gap-3 rounded-lg border p-4">
            <div>
              <p className="text-sm font-medium">Award</p>
              <p className="text-muted-foreground text-sm">
                {award} {commerce.assetCode} units for this task.
              </p>
            </div>
            <TaskEconomicState
              allocationUnits={award}
              escrowStatus={commerce.escrowStatus}
            />
          </div>
          <SharedAwardNotice
            assetCode={commerce.assetCode}
            workerShareUnits={approval?.workerShareUnits ?? null}
            guildShareUnits={approval?.guildShareUnits ?? null}
          />
          <ApproveWorkPanel
            assetCode={commerce.assetCode}
            approval={approval}
            isLoading={approvalQuery.isLoading}
            isApproving={approve.isPending}
            failure={approve.error?.message ?? null}
            onApprove={() => approve.mutate({ taskId })}
          />
          {/* Said in the worker's own terms, so it is not shown to the owner, for whom
              "yours" would be untrue. The approval panel above is the owner's view. */}
          {approval?.viewerIsProjectOwner ? null : (
            <AwardWindowNotice
              releasesAt={commerce.workerHoldReleasesAt}
              isRestricted={commerce.isRestricted ?? true}
            />
          )}
        </CardContent>
      </Card>
    </CommerceShell>
  );
}

/**
 * The project owner's approval of completed work. Approving hands the worker the units that
 * were set aside for them at acceptance; nothing else on the platform can do it, which is why
 * this panel exists rather than a line of copy telling the worker to wait.
 */
function ApproveWorkPanel({
  assetCode,
  approval,
  isLoading,
  isApproving,
  failure,
  onApprove,
}: {
  assetCode: string;
  approval:
    | {
        viewerIsProjectOwner: boolean;
        workerShareUnits: number | null;
        allocationId: string | null;
        isApproved: boolean;
        blockedReason: string | null;
      }
    | undefined;
  isLoading: boolean;
  isApproving: boolean;
  failure: string | null;
  onApprove: () => void;
}) {
  if (isLoading || !approval?.viewerIsProjectOwner) return null;

  if (approval.isApproved) {
    return (
      <div className="border-border bg-muted/30 rounded-lg border p-4">
        <p className="text-sm font-medium">You approved this work</p>
        <p className="text-muted-foreground mt-1 text-sm">
          The units are the worker&apos;s now, held in their own window until
          they can move them.
        </p>
      </div>
    );
  }

  const share = approval.workerShareUnits;

  return (
    <div className="border-border rounded-lg border p-4">
      <p className="text-sm font-medium">Approve the completed work</p>
      <p className="text-muted-foreground mt-1 text-sm">
        {share === null
          ? "Approving hands the worker the units set aside for them at acceptance."
          : `Approving hands the worker their ${share} ${assetCode} units. They are already set aside; approving is what makes them the worker's.`}
      </p>
      {approval.blockedReason ? (
        <p className="text-muted-foreground mt-3 text-sm">
          {approval.blockedReason}
        </p>
      ) : (
        <Button
          className="mt-3"
          onClick={onApprove}
          disabled={isApproving || !approval.allocationId}
        >
          {isApproving ? (
            <>
              <Loader2 className="mr-2 size-4 animate-spin" />
              Approving…
            </>
          ) : (
            "Approve completed work"
          )}
        </Button>
      )}
      {failure ? (
        <p className="text-destructive mt-3 text-sm">{failure}</p>
      ) : null}
    </div>
  );
}

/**
 * Says plainly that part of the award belongs to a guild. Without it the award line reads as
 * the worker's whole share, which it is not once a bid declares a split.
 */
function SharedAwardNotice({
  assetCode,
  workerShareUnits,
  guildShareUnits,
}: {
  assetCode: string;
  workerShareUnits: number | null;
  guildShareUnits: number | null;
}) {
  if (guildShareUnits === null || guildShareUnits <= 0) return null;

  return (
    <div className="border-border bg-muted/30 rounded-lg border p-4">
      <p className="flex items-center gap-2 text-sm font-medium">
        <Users className="text-neon size-4" />
        Shared with a guild
      </p>
      <p className="text-muted-foreground mt-1 text-sm">
        {workerShareUnits === null
          ? `${guildShareUnits} ${assetCode} units of this award are the guild's; the rest is the worker's.`
          : `${workerShareUnits} ${assetCode} units are the worker's and ${guildShareUnits} are the guild's. Each share is set aside separately.`}
      </p>
      <p className="text-muted-foreground mt-1 text-sm">
        The guild&apos;s share is held for the guild. Approving here settles the
        worker&apos;s share only.
      </p>
    </div>
  );
}

/**
 * The worker's own window, said in the worker's terms. The award is already theirs; the only
 * question the notice answers is when they can move it, and why it is held until then.
 *
 * Never phrase this as a refund or chargeback window: these units come from the platform
 * reserve and were never bought with a card, so there is nothing to reverse.
 */
function AwardWindowNotice({
  releasesAt,
  isRestricted,
}: {
  releasesAt?: string | null;
  isRestricted: boolean;
}) {
  if (!isRestricted && !releasesAt) return null;

  const releaseDate = releasesAt ? new Date(releasesAt) : null;
  const readableDate =
    releaseDate && !Number.isNaN(releaseDate.getTime())
      ? releaseDate.toLocaleDateString(undefined, {
          year: "numeric",
          month: "long",
          day: "numeric",
        })
      : null;

  if (!isRestricted) {
    return (
      <div className="border-border bg-muted/30 rounded-lg border p-4">
        <p className="text-sm font-medium">This award is yours to move</p>
        <p className="text-muted-foreground mt-1 text-sm">
          The holding period has ended. You can move these units whenever you like.
        </p>
      </div>
    );
  }

  return (
    <div className="border-border bg-muted/30 rounded-lg border p-4">
      <p className="text-sm font-medium">
        {readableDate
          ? `Yours, and you can move it from ${readableDate}`
          : "Yours once the work is approved"}
      </p>
      <p className="text-muted-foreground mt-1 text-sm">
        {readableDate
          ? "It is held until then so you are not taxed on it before you can actually realise it. Nobody can take it back in the meantime."
          : "The award is set aside for you now. Once the project owner approves your completed work, you will see the date from which you can move it."}
      </p>
    </div>
  );
}

function CommerceShell({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div className="mx-auto max-w-3xl space-y-6 px-4 py-8">
      <Button variant="ghost" asChild className="-ml-2">
        <Link href="/tasks">
          <ArrowLeft className="mr-2 size-4" />
          Back to tasks
        </Link>
      </Button>
      <div className="flex items-center gap-3">
        <div className="bg-neon/10 rounded-lg p-3">
          <CircleDollarSign className="text-neon size-6" />
        </div>
        <h1 className="text-3xl font-bold">{title}</h1>
      </div>
      {children}
    </div>
  );
}

function LoadingState() {
  return (
    <div className="flex min-h-64 items-center justify-center">
      <Loader2 className="text-neon size-6 animate-spin" />
    </div>
  );
}

"use client";

import { Check, Inbox, Loader2, X } from "lucide-react";

import { api } from "~/trpc/react";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "~/components/ui/card";
import { describeMutationError } from "~/components/ui/query-error-copy";

interface ApplicationsPanelProps {
  guildId: string;
  /** ADMIN requests can be accepted only by the owner; the API enforces the same rule. */
  isOwner: boolean;
}

/** Pending applications for the guild's owner and admins to accept or reject. */
export function ApplicationsPanel({
  guildId,
  isOwner,
}: ApplicationsPanelProps) {
  const utils = api.useUtils();
  const { data: applications, isLoading } = api.guild.getApplications.useQuery({
    guildId,
  });
  const { data: members } = api.guild.getMembers.useQuery({ guildId });

  const review = api.guild.reviewApplication.useMutation({
    onSuccess: () => {
      void utils.guild.getApplications.invalidate({ guildId });
      void utils.guild.getMembers.invalidate({ guildId });
    },
  });

  const pending = (applications ?? []).filter(
    (application) => application.status === "PENDING",
  );

  if (isLoading || pending.length === 0) return null;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Inbox className="size-5" />
          Applications
        </CardTitle>
        <CardDescription>
          People asking to join. Accepting makes them a member straight away.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ul className="space-y-3">
          {pending.map((application) => {
            const reviewing =
              review.isPending &&
              review.variables?.applicationId === application.id;
            const needsOwner =
              application.requestedRole === "ADMIN" && !isOwner;
            const alreadyMember = members?.some(
              (member) => member.userId === application.userId,
            );
            return (
              <li
                key={application.id}
                className="border-border space-y-2 rounded border p-3 text-sm"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="font-medium">
                    {application.applicant?.name ?? "A member without a name"}
                  </span>
                  <Badge variant="info">{application.requestedRole}</Badge>
                </div>
                <p className="whitespace-pre-wrap">{application.message}</p>
                {application.skills && (
                  <p className="text-muted-foreground">
                    Skills: {application.skills}
                  </p>
                )}
                <p className="text-muted-foreground text-xs">
                  Applied {new Date(application.appliedAt).toLocaleDateString()}
                </p>
                {needsOwner && (
                  <p className="text-muted-foreground text-xs">
                    Only the guild owner can accept an ADMIN request.
                  </p>
                )}
                {alreadyMember && (
                  <p className="text-muted-foreground text-xs">
                    Already a member; reject to close this application.
                  </p>
                )}
                <div className="flex gap-2">
                  <Button
                    size="sm"
                    disabled={reviewing || needsOwner || alreadyMember}
                    onClick={() =>
                      review.mutate({
                        guildId,
                        applicationId: application.id,
                        status: "APPROVED",
                      })
                    }
                  >
                    {reviewing && review.variables?.status === "APPROVED" ? (
                      <Loader2 className="mr-2 size-4 animate-spin" />
                    ) : (
                      <Check className="mr-2 size-4" />
                    )}
                    Accept
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={reviewing}
                    onClick={() =>
                      review.mutate({
                        guildId,
                        applicationId: application.id,
                        status: "REJECTED",
                      })
                    }
                  >
                    {reviewing && review.variables?.status === "REJECTED" ? (
                      <Loader2 className="mr-2 size-4 animate-spin" />
                    ) : (
                      <X className="mr-2 size-4" />
                    )}
                    Reject
                  </Button>
                </div>
              </li>
            );
          })}
        </ul>
        {review.error && (
          <p role="alert" className="text-destructive mt-3 text-sm">
            {describeMutationError(review.error, "review that application")}
          </p>
        )}
      </CardContent>
    </Card>
  );
}

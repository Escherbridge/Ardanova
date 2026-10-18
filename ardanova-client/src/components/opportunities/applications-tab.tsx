"use client";

import { useState } from "react";
import {
  User,
  Mail,
  FileText,
  ExternalLink,
  Loader2,
  CheckCircle,
  XCircle,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { api } from "~/trpc/react";

interface ApplicationsTabProps {
  opportunityId: string;
  isOwner: boolean;
}

const statusColors: Record<string, string> = {
  pending: "bg-yellow-500/20 text-yellow-500 border-yellow-500/30",
  withdrawn: "bg-muted text-muted-foreground border-border",
  accepted: "bg-green-500/20 text-green-500 border-green-500/30",
  rejected: "bg-red-500/20 text-red-500 border-red-500/30",
};

const statusLabels: Record<string, string> = {
  pending: "Pending",
  withdrawn: "Withdrawn",
  accepted: "Accepted",
  rejected: "Rejected",
};

export default function ApplicationsTab({
  opportunityId,
  isOwner,
}: ApplicationsTabProps) {
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  // A decision is final (accepting also seats the applicant), so each one is confirmed first.
  const [confirming, setConfirming] = useState<{
    applicationId: string;
    status: "accepted" | "rejected";
  } | null>(null);
  const [feedback, setFeedback] = useState<{
    type: "success" | "error";
    message: string;
  } | null>(null);

  const { data: applications, isLoading } =
    api.opportunity.getApplications.useQuery(
      { opportunityId },
      { enabled: isOwner },
    );

  const utils = api.useUtils();

  const updateStatus = api.opportunity.updateApplicationStatus.useMutation({
    onMutate: async (newData) => {
      if (!newData) return;
      setUpdatingId(newData.applicationId);
      setFeedback(null);

      // Cancel any outgoing refetches
      await utils.opportunity.getApplications.cancel({ opportunityId });

      // Snapshot previous value
      const previous = utils.opportunity.getApplications.getData({
        opportunityId,
      });

      // Optimistically update
      utils.opportunity.getApplications.setData({ opportunityId }, (old) => {
        if (!old) return old;
        return old.map((app) =>
          app.id === newData.applicationId
            ? { ...app, status: newData.status }
            : app,
        );
      });

      return { previous };
    },
    onError: (err, newData, context) => {
      // Roll back on error
      if (context?.previous) {
        utils.opportunity.getApplications.setData(
          { opportunityId },
          context.previous,
        );
      }
      setFeedback({
        type: "error",
        message: err.message || "Failed to update status",
      });
      setTimeout(() => setFeedback(null), 3000);
    },
    onSuccess: () => {
      setFeedback({ type: "success", message: "Status updated successfully" });
      setTimeout(() => setFeedback(null), 2000);
    },
    onSettled: () => {
      setUpdatingId(null);
      // Always refetch for consistency
      void utils.opportunity.getApplications.invalidate({ opportunityId });
    },
  });

  // A reviewer decides a PENDING application once: accept or reject. Withdrawing is the applicant's.
  const handleStatusChange = (
    applicationId: string,
    newStatus: "accepted" | "rejected",
  ) => {
    setConfirming(null);
    updateStatus.mutate({ applicationId, opportunityId, status: newStatus });
  };

  if (!isOwner) {
    return (
      <Card className="bg-card border-border border-2">
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">
            Only the opportunity owner can view applications.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (isLoading) {
    return (
      <Card className="bg-card border-border border-2">
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">Loading applications...</p>
        </CardContent>
      </Card>
    );
  }

  if (!applications || applications.length === 0) {
    return (
      <Card className="bg-card border-border border-2">
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">No applications yet.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Feedback notification */}
      {feedback && (
        <div
          className={`flex items-center gap-2 rounded-none p-3 text-sm ${
            feedback.type === "success"
              ? "border border-green-500/30 bg-green-500/10 text-green-500"
              : "border border-red-500/30 bg-red-500/10 text-red-500"
          }`}
        >
          {feedback.type === "success" ? (
            <CheckCircle className="size-4" />
          ) : (
            <XCircle className="size-4" />
          )}
          {feedback.message}
        </div>
      )}

      {applications.map((application) => (
        <Card key={application.id} className="bg-card border-border border-2">
          <CardHeader>
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <Avatar>
                  <AvatarImage
                    src={application.applicant?.image ?? undefined}
                    alt=""
                  />
                  <AvatarFallback>
                    {application.applicant?.name?.[0]?.toUpperCase() ?? "U"}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <CardTitle className="flex items-center gap-2 text-base">
                    <User className="text-muted-foreground size-4" />
                    {application.applicant?.name ?? "Unknown applicant"}
                  </CardTitle>
                  <p className="text-muted-foreground mt-1 flex items-center gap-1 text-sm">
                    <Mail className="size-3" />
                    {application.applicant?.email ?? "Email unavailable"}
                  </p>
                </div>
              </div>
              <Badge
                className={
                  statusColors[
                    application.status?.toLowerCase() ?? "pending"
                  ] ?? statusColors.pending
                }
                variant="outline"
              >
                {statusLabels[application.status?.toLowerCase() ?? "pending"] ??
                  application.status ??
                  "Pending"}
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <h4 className="mb-2 flex items-center gap-2 text-sm font-semibold">
                <FileText className="text-muted-foreground size-4" />
                Cover Letter
              </h4>
              <p className="text-foreground text-sm leading-relaxed whitespace-pre-wrap">
                {application.coverLetter}
              </p>
            </div>

            {application.portfolio && (
              <div>
                <Button variant="outline" size="sm" asChild className="gap-2">
                  <a
                    href={application.portfolio}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    <ExternalLink className="size-4" />
                    View Portfolio
                  </a>
                </Button>
              </div>
            )}

            {(application.status?.toLowerCase() ?? "pending") === "pending" && (
              <div className="flex flex-wrap items-center gap-3 border-t pt-2">
                {updatingId === application.id ? (
                  <span className="text-muted-foreground flex items-center gap-2 text-sm">
                    <Loader2 className="size-4 animate-spin" />
                    Updating...
                  </span>
                ) : confirming?.applicationId === application.id ? (
                  <>
                    <span className="text-sm">
                      {confirming.status === "accepted"
                        ? `Accept ${application.applicant?.name ?? "this applicant"}? They join the team in this role. This can't be undone.`
                        : `Reject ${application.applicant?.name ?? "this applicant"}? This can't be undone.`}
                    </span>
                    <Button
                      size="sm"
                      className="min-h-11"
                      variant={
                        confirming.status === "accepted"
                          ? "default"
                          : "destructive"
                      }
                      onClick={() =>
                        handleStatusChange(application.id, confirming.status)
                      }
                    >
                      {confirming.status === "accepted"
                        ? "Yes, accept"
                        : "Yes, reject"}
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="min-h-11"
                      onClick={() => setConfirming(null)}
                    >
                      Cancel
                    </Button>
                  </>
                ) : (
                  <>
                    <Button
                      size="sm"
                      className="min-h-11 gap-1.5"
                      onClick={() =>
                        setConfirming({
                          applicationId: application.id,
                          status: "accepted",
                        })
                      }
                    >
                      <CheckCircle className="size-4" aria-hidden="true" />
                      Accept
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      className="min-h-11 gap-1.5"
                      onClick={() =>
                        setConfirming({
                          applicationId: application.id,
                          status: "rejected",
                        })
                      }
                    >
                      <XCircle className="size-4" aria-hidden="true" />
                      Reject
                    </Button>
                  </>
                )}
              </div>
            )}

            <div className="text-muted-foreground text-xs">
              Applied on {new Date(application.appliedAt).toLocaleDateString()}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

"use client";

import { useState } from "react";
import { User, Mail, FileText, ExternalLink, Loader2, CheckCircle, XCircle } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Badge } from "~/components/ui/badge";
import { Button } from "~/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "~/components/ui/avatar";
import { api } from "~/trpc/react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";

interface ApplicationsTabProps {
  opportunityId: string;
  isOwner: boolean;
}

const statusVariants = {
  pending: "secondary" as const,
  reviewing: "default" as const,
  accepted: "default" as const,
  rejected: "destructive" as const,
};

const statusColors: Record<string, string> = {
  pending: "bg-yellow-500/20 text-yellow-500 border-yellow-500/30",
  reviewing: "bg-blue-500/20 text-blue-500 border-blue-500/30",
  accepted: "bg-green-500/20 text-green-500 border-green-500/30",
  rejected: "bg-red-500/20 text-red-500 border-red-500/30",
};

const statusLabels: Record<string, string> = {
  pending: "Pending",
  reviewing: "Reviewing",
  accepted: "Accepted",
  rejected: "Rejected",
};

export default function ApplicationsTab({
  opportunityId,
  isOwner,
}: ApplicationsTabProps) {
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<{ type: "success" | "error"; message: string } | null>(null);

  const { data: applications, isLoading } =
    api.opportunity.getApplications.useQuery(
      { opportunityId },
      { enabled: isOwner }
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
      const previous = utils.opportunity.getApplications.getData({ opportunityId });

      // Optimistically update
      utils.opportunity.getApplications.setData({ opportunityId }, (old) => {
        if (!old) return old;
        return old.map((app) =>
          app.id === newData.applicationId
            ? { ...app, status: newData.status }
            : app
        );
      });

      return { previous };
    },
    onError: (err, newData, context) => {
      // Roll back on error
      if (context?.previous) {
        utils.opportunity.getApplications.setData({ opportunityId }, context.previous);
      }
      setFeedback({ type: "error", message: err.message || "Failed to update status" });
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

  const handleStatusChange = (applicationId: string, newStatus: string) => {
    updateStatus.mutate({
      applicationId,
      opportunityId,
      status: newStatus as "pending" | "reviewing" | "accepted" | "rejected",
    });
  };

  if (!isOwner) {
    return (
      <Card className="bg-card border-2 border-border">
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
      <Card className="bg-card border-2 border-border">
        <CardContent className="py-12 text-center">
          <p className="text-muted-foreground">Loading applications...</p>
        </CardContent>
      </Card>
    );
  }

  if (!applications || applications.length === 0) {
    return (
      <Card className="bg-card border-2 border-border">
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
          className={`flex items-center gap-2 p-3 rounded-lg text-sm ${
            feedback.type === "success"
              ? "bg-green-500/10 text-green-500 border border-green-500/30"
              : "bg-red-500/10 text-red-500 border border-red-500/30"
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
        <Card key={application.id} className="bg-card border-2 border-border">
          <CardHeader>
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <Avatar>
                  <AvatarImage src={application.applicant.image ?? undefined} />
                  <AvatarFallback>
                    {application.applicant.name?.[0]?.toUpperCase() ?? "U"}
                  </AvatarFallback>
                </Avatar>
                <div>
                  <CardTitle className="text-base flex items-center gap-2">
                    <User className="size-4 text-muted-foreground" />
                    {application.applicant.name}
                  </CardTitle>
                  <p className="text-sm text-muted-foreground flex items-center gap-1 mt-1">
                    <Mail className="size-3" />
                    {application.applicant.email}
                  </p>
                </div>
              </div>
              <Badge
                className={statusColors[application.status?.toLowerCase()] || statusColors.pending}
                variant="outline"
              >
                {statusLabels[application.status?.toLowerCase()] || application.status}
              </Badge>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <h4 className="text-sm font-semibold flex items-center gap-2 mb-2">
                <FileText className="size-4 text-muted-foreground" />
                Cover Letter
              </h4>
              <p className="text-sm text-foreground whitespace-pre-wrap leading-relaxed">
                {application.coverLetter}
              </p>
            </div>

            {application.portfolio && (
              <div>
                <Button
                  variant="outline"
                  size="sm"
                  asChild
                  className="gap-2"
                >
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

            <div className="flex items-center gap-3 pt-2 border-t">
              <span className="text-sm text-muted-foreground">
                Update Status:
              </span>
              <Select
                value={application.status?.toLowerCase() || "pending"}
                onValueChange={(value) =>
                  handleStatusChange(application.id, value)
                }
                disabled={updatingId === application.id}
              >
                <SelectTrigger className="w-[180px]">
                  {updatingId === application.id ? (
                    <div className="flex items-center gap-2">
                      <Loader2 className="size-4 animate-spin" />
                      <span>Updating...</span>
                    </div>
                  ) : (
                    <SelectValue />
                  )}
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="pending">Pending</SelectItem>
                  <SelectItem value="reviewing">Reviewing</SelectItem>
                  <SelectItem value="accepted">Accepted</SelectItem>
                  <SelectItem value="rejected">Rejected</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="text-xs text-muted-foreground">
              Applied on {new Date(application.appliedAt).toLocaleDateString()}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

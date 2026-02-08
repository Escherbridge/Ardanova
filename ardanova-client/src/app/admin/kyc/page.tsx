"use client";

import { useState } from "react";
import { api } from "~/trpc/react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
  CardFooter,
} from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Badge } from "~/components/ui/badge";
import { Textarea } from "~/components/ui/textarea";
import {
  Shield,
  ShieldCheck,
  ShieldAlert,
  CheckCircle,
  XCircle,
  Clock,
  Loader2,
  FileText,
  ExternalLink,
} from "lucide-react";

export default function KYCReviewDashboard() {
  const [reviewingId, setReviewingId] = useState<string | null>(null);
  const [reviewNotes, setReviewNotes] = useState("");
  const [rejectionReason, setRejectionReason] = useState("");

  const utils = api.useUtils();
  const { data: pendingSubmissions, isLoading } =
    api.kyc.getPending.useQuery();

  const approveMutation = api.kyc.approve.useMutation({
    onSuccess: () => {
      void utils.kyc.getPending.invalidate();
      setReviewNotes("");
    },
  });

  const rejectMutation = api.kyc.reject.useMutation({
    onSuccess: () => {
      void utils.kyc.getPending.invalidate();
      setReviewingId(null);
      setRejectionReason("");
      setReviewNotes("");
    },
  });

  const handleApprove = (id: string) => {
    approveMutation.mutate({ id, reviewNotes: reviewNotes || undefined });
  };

  const handleReject = (id: string) => {
    if (!rejectionReason.trim()) {
      return;
    }
    rejectMutation.mutate({
      id,
      rejectionReason,
      reviewNotes: reviewNotes || undefined,
    });
  };

  const handleCancelReject = () => {
    setReviewingId(null);
    setRejectionReason("");
    setReviewNotes("");
  };

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-neon-green" />
      </div>
    );
  }

  return (
    <div className="container mx-auto max-w-7xl space-y-8 py-8">
      <div className="space-y-2 border-b border-white/10 pb-6">
        <div className="flex items-center gap-3">
          <Shield className="h-8 w-8 text-neon-green" />
          <h1 className="text-3xl font-bold tracking-tight">
            KYC Review Dashboard
          </h1>
        </div>
        <p className="text-sm text-muted-foreground">
          Review and approve or reject pending KYC submissions from users.
        </p>
      </div>

      {!pendingSubmissions || pendingSubmissions.length === 0 ? (
        <Card className="border-white/10">
          <CardContent className="flex flex-col items-center justify-center py-16">
            <ShieldCheck className="mb-4 h-16 w-16 text-muted-foreground/50" />
            <p className="text-lg font-medium text-muted-foreground">
              No pending submissions
            </p>
            <p className="text-sm text-muted-foreground/70">
              All KYC submissions have been reviewed.
            </p>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-6">
          {pendingSubmissions.map((submission) => (
            <Card key={submission.id} className="border-white/10">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-4">
                    {submission.userImage ? (
                      <img
                        src={submission.userImage}
                        alt={submission.userName ?? "User"}
                        className="h-12 w-12 rounded-full border-2 border-white/10 object-cover"
                      />
                    ) : (
                      <div className="flex h-12 w-12 items-center justify-center rounded-full border-2 border-white/10 bg-white/5 text-lg font-bold text-muted-foreground">
                        {(submission.userName ?? submission.userEmail ?? "?").charAt(0).toUpperCase()}
                      </div>
                    )}
                    <div className="space-y-1">
                      <CardTitle className="text-xl">
                        {submission.userName ?? "Unknown User"}
                      </CardTitle>
                      <CardDescription className="space-y-0.5">
                        <span className="block text-sm">
                          {submission.userEmail ?? submission.userId}
                        </span>
                        <span className="flex items-center gap-1.5 text-xs text-muted-foreground/70">
                          <Clock className="h-3 w-3" />
                          Submitted {new Date(submission.submittedAt).toLocaleDateString()}
                        </span>
                      </CardDescription>
                    </div>
                  </div>
                  <Badge variant="warning" className="flex items-center gap-1">
                    <ShieldAlert className="h-3 w-3" />
                    {submission.status === "IN_REVIEW" ? "IN REVIEW" : "PENDING"}
                  </Badge>
                </div>
              </CardHeader>

              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <h3 className="flex items-center gap-2 text-sm font-medium">
                    <FileText className="h-4 w-4" />
                    Documents
                  </h3>
                  <div className="space-y-2">
                    {submission.documents.map((doc) => (
                      <div
                        key={doc.id}
                        className="flex items-center justify-between rounded-md border border-white/10 bg-white/5 p-3"
                      >
                        <div className="flex items-center gap-3">
                          <Badge variant="outline">{doc.type}</Badge>
                          <span className="text-sm">{doc.fileName}</span>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          asChild
                          className="h-8"
                        >
                          <a
                            href={doc.fileUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="flex items-center gap-2"
                          >
                            View
                            <ExternalLink className="h-3 w-3" />
                          </a>
                        </Button>
                      </div>
                    ))}
                  </div>
                </div>

                {reviewingId === submission.id && (
                  <div className="space-y-4 rounded-md border border-destructive/50 bg-destructive/5 p-4">
                    <div className="space-y-2">
                      <label className="text-sm font-medium text-destructive">
                        Rejection Reason *
                      </label>
                      <Textarea
                        placeholder="Enter the reason for rejection..."
                        value={rejectionReason}
                        onChange={(e) => setRejectionReason(e.target.value)}
                        className="min-h-[100px]"
                      />
                    </div>
                    <div className="space-y-2">
                      <label className="text-sm font-medium">
                        Review Notes (Optional)
                      </label>
                      <Textarea
                        placeholder="Additional notes for internal review..."
                        value={reviewNotes}
                        onChange={(e) => setReviewNotes(e.target.value)}
                        className="min-h-[80px]"
                      />
                    </div>
                  </div>
                )}
              </CardContent>

              <CardFooter className="flex gap-3 border-t border-white/10 pt-6">
                {reviewingId === submission.id ? (
                  <>
                    <Button
                      variant="destructive"
                      onClick={() => handleReject(submission.id)}
                      disabled={
                        !rejectionReason.trim() || rejectMutation.isPending
                      }
                      className="flex items-center gap-2"
                    >
                      {rejectMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <XCircle className="h-4 w-4" />
                      )}
                      Confirm Reject
                    </Button>
                    <Button
                      variant="outline"
                      onClick={handleCancelReject}
                      disabled={rejectMutation.isPending}
                    >
                      Cancel
                    </Button>
                  </>
                ) : (
                  <>
                    <Button
                      variant="neon-green"
                      onClick={() => handleApprove(submission.id)}
                      disabled={approveMutation.isPending}
                      className="flex items-center gap-2"
                    >
                      {approveMutation.isPending ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <CheckCircle className="h-4 w-4" />
                      )}
                      Approve
                    </Button>
                    <Button
                      variant="destructive"
                      onClick={() => setReviewingId(submission.id)}
                      disabled={approveMutation.isPending}
                      className="flex items-center gap-2"
                    >
                      <XCircle className="h-4 w-4" />
                      Reject
                    </Button>
                  </>
                )}
              </CardFooter>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

"use client";

import { useState } from "react";
import Link from "next/link";
import { api } from "~/trpc/react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "~/components/ui/card";
import { Button } from "~/components/ui/button";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "~/components/ui/select";
import { Badge } from "~/components/ui/badge";
import { KycStatusBanner } from "~/components/kyc/kyc-status-banner";
import { cn } from "~/lib/utils";
import { Shield, ShieldCheck, Upload, FileText, Loader2, X, Plus } from "lucide-react";
import type { KycDocumentType } from "~/lib/api/ardanova/endpoints/kyc";

const DOCUMENT_TYPES: Array<{ value: KycDocumentType; label: string }> = [
  { value: "GOVERNMENT_ID", label: "Government ID" },
  { value: "PASSPORT", label: "Passport" },
  { value: "DRIVERS_LICENSE", label: "Driver's License" },
];

interface DocumentEntry {
  type: KycDocumentType;
  fileUrl: string;
  fileName: string;
}

export default function VerificationPage() {
  const { data: status, isLoading: statusLoading, refetch } = api.kyc.getMyStatus.useQuery();
  const [documents, setDocuments] = useState<DocumentEntry[]>([
    { type: "GOVERNMENT_ID", fileUrl: "", fileName: "" },
  ]);

  const submitMutation = api.kyc.submit.useMutation({
    onSuccess: () => {
      void refetch();
      setDocuments([{ type: "GOVERNMENT_ID", fileUrl: "", fileName: "" }]);
    },
  });

  const handleAddDocument = () => {
    setDocuments([...documents, { type: "GOVERNMENT_ID" as KycDocumentType, fileUrl: "", fileName: "" }]);
  };

  const handleRemoveDocument = (index: number) => {
    setDocuments(documents.filter((_, i) => i !== index));
  };

  const handleDocumentChange = (index: number, field: keyof DocumentEntry, value: string) => {
    const newDocuments = [...documents];
    newDocuments[index] = { ...newDocuments[index]!, [field]: value };
    setDocuments(newDocuments);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const validDocuments = documents.filter(doc => doc.fileUrl && doc.fileName);
    if (validDocuments.length === 0) return;

    submitMutation.mutate({ documents: validDocuments });
  };

  const canSubmit = documents.some(doc => doc.fileUrl && doc.fileName);

  if (statusLoading) {
    return (
      <div className="container mx-auto max-w-4xl px-4 py-8">
        <div className="flex items-center justify-center min-h-[400px]">
          <Loader2 className="h-8 w-8 animate-spin text-neon-cyan" />
        </div>
      </div>
    );
  }

  const isApproved = status?.status === "APPROVED";
  const isPending = status?.status === "PENDING" || status?.status === "IN_REVIEW";
  const isRejected = status?.status === "REJECTED";
  const notSubmitted = !status;

  return (
    <div className="container mx-auto max-w-4xl px-4 py-8">
      <div className="space-y-6">
        {/* Header */}
        <div className="space-y-2">
          <div className="flex items-center gap-3">
            <Shield className="h-8 w-8 text-neon-cyan" />
            <h1 className="text-3xl font-black tracking-tight text-foreground">
              Identity Verification
            </h1>
          </div>
          <p className="text-sm text-muted-foreground">
            Verify your identity to unlock advanced features and increase your trust level.
          </p>
        </div>

        {/* Status Banner */}
        <KycStatusBanner />

        {/* Approved State */}
        {isApproved && (
          <Card className="border-neon-green/20 bg-neon-green/5">
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-none bg-neon-green/10">
                  <ShieldCheck className="h-6 w-6 text-neon-green" />
                </div>
                <div className="flex-1">
                  <h3 className="font-bold text-foreground">Verification Complete</h3>
                  <p className="text-sm text-muted-foreground">
                    Your identity has been verified. You now have full access to all features.
                  </p>
                </div>
                <Badge variant="success" className="rounded-none">
                  Verified
                </Badge>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Pending/In Review State */}
        {isPending && (
          <Card className="border-neon-yellow/20 bg-neon-yellow/5">
            <CardContent className="pt-6">
              <div className="flex items-center gap-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-none bg-neon-yellow/10">
                  <Loader2 className="h-6 w-6 text-neon-yellow animate-spin" />
                </div>
                <div className="flex-1">
                  <h3 className="font-bold text-foreground">Verification In Progress</h3>
                  <p className="text-sm text-muted-foreground">
                    Your documents are being reviewed. This usually takes 1-2 business days.
                  </p>
                </div>
                <Badge variant="warning" className="rounded-none">
                  {status?.status === "PENDING" ? "Pending" : "In Review"}
                </Badge>
              </div>
            </CardContent>
          </Card>
        )}

        {/* Submission Form */}
        {(notSubmitted || isRejected) && (
          <Card>
            <CardHeader>
              <CardTitle className="font-black">Submit Verification Documents</CardTitle>
              <CardDescription>
                {isRejected
                  ? "Your previous submission was rejected. Please review the feedback and submit again."
                  : "Upload valid government-issued identification documents to verify your identity."
                }
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit} className="space-y-6">
                {/* Rejection Reason */}
                {isRejected && status?.rejectionReason && (
                  <div className="rounded-none border border-destructive/20 bg-destructive/5 p-4">
                    <p className="text-sm font-bold text-destructive mb-1">Rejection Reason:</p>
                    <p className="text-sm text-foreground">{status.rejectionReason}</p>
                  </div>
                )}

                {/* Document Inputs */}
                <div className="space-y-4">
                  {documents.map((doc, index) => (
                    <div key={index} className="rounded-none border border-border p-4 space-y-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <FileText className="h-4 w-4 text-muted-foreground" />
                          <span className="text-sm font-bold">
                            Document {index + 1}
                          </span>
                        </div>
                        {documents.length > 1 && (
                          <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => handleRemoveDocument(index)}
                            className="h-8 w-8 p-0"
                          >
                            <X className="h-4 w-4" />
                          </Button>
                        )}
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor={`type-${index}`}>Document Type</Label>
                        <Select
                          value={doc.type}
                          onValueChange={(value) => handleDocumentChange(index, "type", value)}
                        >
                          <SelectTrigger id={`type-${index}`} className="rounded-none">
                            <SelectValue placeholder="Select document type" />
                          </SelectTrigger>
                          <SelectContent>
                            {DOCUMENT_TYPES.map((type) => (
                              <SelectItem key={type.value} value={type.value}>
                                {type.label}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor={`url-${index}`}>File URL</Label>
                        <Input
                          id={`url-${index}`}
                          type="url"
                          placeholder="https://example.com/document.pdf"
                          value={doc.fileUrl}
                          onChange={(e) => handleDocumentChange(index, "fileUrl", e.target.value)}
                          variant="neon"
                          className="rounded-none"
                        />
                      </div>

                      <div className="space-y-2">
                        <Label htmlFor={`name-${index}`}>File Name</Label>
                        <Input
                          id={`name-${index}`}
                          type="text"
                          placeholder="passport.pdf"
                          value={doc.fileName}
                          onChange={(e) => handleDocumentChange(index, "fileName", e.target.value)}
                          variant="neon"
                          className="rounded-none"
                        />
                      </div>
                    </div>
                  ))}
                </div>

                {/* Add Document Button */}
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleAddDocument}
                  className="w-full rounded-none"
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Add Another Document
                </Button>

                {/* Submit Button */}
                <div className="flex items-center gap-4 pt-4">
                  <Button
                    type="submit"
                    variant="neon"
                    disabled={!canSubmit || submitMutation.isPending}
                    className="flex-1 rounded-none"
                  >
                    {submitMutation.isPending ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Submitting...
                      </>
                    ) : (
                      <>
                        <Upload className="mr-2 h-4 w-4" />
                        Submit for Verification
                      </>
                    )}
                  </Button>
                </div>

                {submitMutation.isError && (
                  <div className="rounded-none border border-neon-red/20 bg-neon-red/5 p-3">
                    <p className="text-sm text-neon-red">
                      {submitMutation.error?.message || "Failed to submit documents. Please try again."}
                    </p>
                  </div>
                )}

                {submitMutation.isSuccess && (
                  <div className="rounded-none border border-neon-green/20 bg-neon-green/5 p-3">
                    <p className="text-sm text-neon-green">
                      Documents submitted successfully! Your verification is now pending review.
                    </p>
                  </div>
                )}
              </form>
            </CardContent>
          </Card>
        )}

        {/* Help Text */}
        <Card className="border-border/50">
          <CardContent className="pt-6">
            <div className="space-y-3">
              <h4 className="text-sm font-bold text-foreground">Verification Requirements</h4>
              <ul className="space-y-2 text-sm text-muted-foreground">
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>Documents must be clear and readable</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>All information must be visible and not obscured</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>Documents must be current and not expired</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-neon-cyan mt-0.5">•</span>
                  <span>Verification typically takes 1-2 business days</span>
                </li>
              </ul>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

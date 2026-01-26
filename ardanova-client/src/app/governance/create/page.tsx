"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Loader2, Info, Vote, Calendar, AlertTriangle } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "~/components/ui/select";

const proposalTypes = [
  { id: "Treasury", label: "Treasury", description: "Allocate or manage treasury funds" },
  { id: "Governance", label: "Governance", description: "Change governance rules or processes" },
  { id: "Strategic", label: "Strategic", description: "Major strategic decisions" },
  { id: "Operational", label: "Operational", description: "Day-to-day operational changes" },
  { id: "Emergency", label: "Emergency", description: "Urgent matters requiring quick action" },
  { id: "Constitutional", label: "Constitutional", description: "Fundamental protocol changes" },
  { id: "Token", label: "Token", description: "Token-related decisions" },
];

const votingDurations = [
  { id: "3", label: "3 days" },
  { id: "5", label: "5 days" },
  { id: "7", label: "7 days" },
  { id: "14", label: "14 days" },
];

const quorumOptions = [
  { id: "10", label: "10%" },
  { id: "25", label: "25%" },
  { id: "33", label: "33%" },
  { id: "50", label: "50%" },
];

export default function CreateProposalPage() {
  const router = useRouter();
  const [formData, setFormData] = useState({
    title: "",
    type: "",
    summary: "",
    description: "",
    rationale: "",
    votingDuration: "7",
    quorum: "25",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (field: string, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.title.trim()) newErrors.title = "Title is required";
    if (!formData.type) newErrors.type = "Proposal type is required";
    if (!formData.summary.trim()) newErrors.summary = "Summary is required";
    if (formData.summary.length < 20)
      newErrors.summary = "Summary must be at least 20 characters";
    if (!formData.description.trim())
      newErrors.description = "Description is required";
    if (formData.description.length < 50)
      newErrors.description = "Description must be at least 50 characters";
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    // TODO: Implement proposal creation API
    setTimeout(() => {
      router.push("/governance");
    }, 1000);
  };

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <Button variant="ghost" asChild className="mb-4 -ml-2">
            <Link href="/governance">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Governance
            </Link>
          </Button>
          <h1 className="text-3xl font-bold flex items-center gap-3">
            <div className="w-10 h-10 bg-neon-purple/20 rounded-lg flex items-center justify-center">
              <Vote className="h-5 w-5 text-neon-purple" />
            </div>
            New Proposal
          </h1>
          <p className="text-muted-foreground mt-2">
            Submit a proposal for the community to vote on
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Proposal Type */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Proposal Type</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Type <span className="text-neon">*</span>
                </label>
                <Select
                  value={formData.type}
                  onValueChange={(value) => handleChange("type", value)}
                >
                  <SelectTrigger
                    className={
                      errors.type ? "border-destructive" : "border-border"
                    }
                  >
                    <SelectValue placeholder="Select proposal type" />
                  </SelectTrigger>
                  <SelectContent>
                    {proposalTypes.map((type) => (
                      <SelectItem key={type.id} value={type.id}>
                        <div className="flex flex-col">
                          <span>{type.label}</span>
                          <span className="text-xs text-muted-foreground">
                            {type.description}
                          </span>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {errors.type && (
                  <p className="text-sm text-destructive mt-1">{errors.type}</p>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Proposal Content */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Proposal Content</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Title <span className="text-neon">*</span>
                </label>
                <input
                  type="text"
                  value={formData.title}
                  onChange={(e) => handleChange("title", e.target.value)}
                  placeholder="e.g., Allocate 15% Treasury for European Expansion"
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 ${
                    errors.title ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.title && (
                  <p className="text-sm text-destructive mt-1">{errors.title}</p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Summary <span className="text-neon">*</span>
                </label>
                <textarea
                  value={formData.summary}
                  onChange={(e) => handleChange("summary", e.target.value)}
                  placeholder="A brief summary of the proposal (1-2 sentences)"
                  rows={2}
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.summary ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.summary && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.summary}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Full Description <span className="text-neon">*</span>
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => handleChange("description", e.target.value)}
                  placeholder="Detailed description of the proposal, including all relevant information..."
                  rows={6}
                  className={`w-full px-4 py-3 bg-muted/50 border-2 rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none ${
                    errors.description ? "border-destructive" : "border-border"
                  }`}
                />
                {errors.description && (
                  <p className="text-sm text-destructive mt-1">
                    {errors.description}
                  </p>
                )}
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">
                  Rationale
                </label>
                <textarea
                  value={formData.rationale}
                  onChange={(e) => handleChange("rationale", e.target.value)}
                  placeholder="Why should voters support this proposal? What are the benefits?"
                  rows={4}
                  className="w-full px-4 py-3 bg-muted/50 border-2 border-border rounded-lg focus:outline-none focus:ring-2 focus:ring-neon/50 resize-none"
                />
              </div>
            </CardContent>
          </Card>

          {/* Voting Parameters */}
          <Card className="bg-card border-border">
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <Calendar className="h-5 w-5 text-neon" />
                Voting Parameters
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Voting Duration
                  </label>
                  <Select
                    value={formData.votingDuration}
                    onValueChange={(value) =>
                      handleChange("votingDuration", value)
                    }
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select duration" />
                    </SelectTrigger>
                    <SelectContent>
                      {votingDurations.map((dur) => (
                        <SelectItem key={dur.id} value={dur.id}>
                          {dur.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div>
                  <label className="text-sm font-medium mb-2 block">
                    Quorum Required
                  </label>
                  <Select
                    value={formData.quorum}
                    onValueChange={(value) => handleChange("quorum", value)}
                  >
                    <SelectTrigger>
                      <SelectValue placeholder="Select quorum" />
                    </SelectTrigger>
                    <SelectContent>
                      {quorumOptions.map((opt) => (
                        <SelectItem key={opt.id} value={opt.id}>
                          {opt.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Warning */}
          <div className="flex items-start gap-3 p-4 bg-warning/10 rounded-lg border border-warning/30">
            <AlertTriangle className="h-5 w-5 text-warning mt-0.5" />
            <div className="text-sm">
              <p className="font-medium text-warning">Important</p>
              <p className="text-muted-foreground mt-1">
                Once submitted, proposals cannot be edited. Make sure all
                information is accurate before submitting.
              </p>
            </div>
          </div>

          {/* Submit Error */}
          {errors.submit && (
            <Card className="bg-destructive/10 border-destructive">
              <CardContent className="py-4">
                <p className="text-sm text-destructive">{errors.submit}</p>
              </CardContent>
            </Card>
          )}

          {/* Submit Button */}
          <div className="flex gap-4">
            <Button
              type="submit"
              className="flex-1 bg-neon hover:bg-neon/90 text-black font-semibold py-6"
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                  Submitting Proposal...
                </>
              ) : (
                "Submit Proposal"
              )}
            </Button>
            <Button type="button" variant="outline" asChild className="py-6">
              <Link href="/governance">Cancel</Link>
            </Button>
          </div>

          {/* Info Note */}
          <div className="flex items-start gap-3 p-4 bg-muted/30 rounded-lg border border-border">
            <Info className="h-5 w-5 text-neon mt-0.5" />
            <div className="text-sm text-muted-foreground">
              <p>
                Proposals require community voting to pass. The voting period will
                begin immediately after submission.
              </p>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}

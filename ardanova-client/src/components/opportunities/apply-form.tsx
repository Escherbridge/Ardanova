"use client";

import { type FormEvent, useState } from "react";
import { CheckCircle, Loader2, Send } from "lucide-react";

import { Button } from "~/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "~/components/ui/card";
import { Input } from "~/components/ui/input";
import { Label } from "~/components/ui/label";
import { Textarea } from "~/components/ui/textarea";
import { api } from "~/trpc/react";

interface ApplyFormProps {
  opportunityId: string;
  opportunityStatus: string;
}

const MIN_COVER_LETTER_LENGTH = 20;

/** Mirrors the router's `z.string().url()` so a bad link is caught before the round trip. */
function isAbsoluteUrl(value: string): boolean {
  try {
    new URL(value);
    return true;
  } catch {
    return false;
  }
}

/** Applies to a role; an accepted application seats the applicant on the project team (see components/opportunities/AGENTS.md). */
export function ApplyForm({ opportunityId, opportunityStatus }: ApplyFormProps) {
  const [coverLetter, setCoverLetter] = useState("");
  const [portfolio, setPortfolio] = useState("");
  const [validationError, setValidationError] = useState<string | null>(null);
  const submitApplication = api.opportunity.submitApplication.useMutation();

  if (opportunityStatus !== "OPEN") {
    return (
      <Card className="bg-card border-border">
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Apply for this role</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground text-sm">
            This role is not accepting applications right now.
          </p>
        </CardContent>
      </Card>
    );
  }

  if (submitApplication.isSuccess) {
    return (
      <Card className="bg-card border-border">
        <CardContent className="flex items-center gap-2 py-6 text-sm">
          <CheckCircle className="text-neon-green size-4" />
          Application sent. The team will accept or decline it here.
        </CardContent>
      </Card>
    );
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setValidationError(null);
    const trimmed = coverLetter.trim();
    if (trimmed.length < MIN_COVER_LETTER_LENGTH) {
      setValidationError(
        `Tell the team a little more (at least ${MIN_COVER_LETTER_LENGTH} characters).`,
      );
      return;
    }
    const portfolioUrl = portfolio.trim();
    if (portfolioUrl && !isAbsoluteUrl(portfolioUrl)) {
      setValidationError("Portfolio must be a full link, e.g. https://…");
      return;
    }
    submitApplication.mutate({
      opportunityId,
      coverLetter: trimmed,
      portfolio: portfolioUrl || undefined,
    });
  }

  return (
    <Card className="bg-card border-border">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg">Apply for this role</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="application-cover-letter">Why you</Label>
            <Textarea
              id="application-cover-letter"
              value={coverLetter}
              onChange={(event) => setCoverLetter(event.target.value)}
              placeholder="What you'd bring to the team and how you'd like to help."
              rows={4}
              required
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="application-portfolio">Portfolio link</Label>
            <Input
              id="application-portfolio"
              type="url"
              value={portfolio}
              onChange={(event) => setPortfolio(event.target.value)}
              placeholder="Optional"
            />
          </div>
          {(validationError ?? submitApplication.error?.message) && (
            <p className="text-destructive text-sm" role="alert">
              {validationError ?? submitApplication.error?.message}
            </p>
          )}
          <Button type="submit" disabled={submitApplication.isPending}>
            {submitApplication.isPending ? (
              <Loader2 className="animate-spin" />
            ) : (
              <Send />
            )}
            Send application
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

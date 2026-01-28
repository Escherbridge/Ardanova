"use client";

import { useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import {
  ArrowLeft,
  Loader2,
} from "lucide-react";

import { Button } from "~/components/ui/button";
import { api } from "~/trpc/react";

export default function ProposalDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;

  const { data: proposal, isLoading } = api.governance.getByIdWithProjectSlug.useQuery({ id });

  useEffect(() => {
    if (proposal?.projectSlug) {
      router.push(`/projects/${proposal.projectSlug}?tab=proposals&proposalId=${id}`);
    }
  }, [proposal, router, id]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="size-8 animate-spin text-primary" />
        <p className="ml-3 text-muted-foreground">Redirecting to project page...</p>
      </div>
    );
  }

  if (!proposal) {
    return (
      <div className="min-h-screen bg-background flex flex-col items-center justify-center">
        <h1 className="text-2xl font-bold text-foreground">Proposal not found</h1>
        <p className="text-muted-foreground mt-2">
          This proposal may have been deleted or does not exist.
        </p>
        <Button asChild className="mt-4">
          <Link href="/governance">
            <ArrowLeft className="size-4 mr-2" />
            Back to Governance
          </Link>
        </Button>
      </div>
    );
  }

  return null;
}

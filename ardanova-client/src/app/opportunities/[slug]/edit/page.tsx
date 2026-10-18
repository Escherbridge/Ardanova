import { redirect, notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { apiClient } from "~/lib/api";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { buildSignInHref } from "~/lib/auth-navigation";
import { OpportunityForm } from "~/components/opportunity-form";
import { toOpportunityPageData } from "../opportunity-page-contract";

interface EditOpportunityPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function EditOpportunityPage({
  params,
}: EditOpportunityPageProps) {
  const { slug } = await params;
  const session = await auth();

  if (!session) {
    redirect(buildSignInHref(`/opportunities/${slug}/edit`));
  }

  // Signed read so the poster can still reach a DRAFT role (anonymous reads hide drafts).
  const response = await runWithActorAssertion(
    { subject: session.user.id, role: session.user.role },
    () => apiClient.opportunities.getBySlug(slug),
  );
  if (response.error || !response.data) {
    notFound();
  }
  const opportunity = toOpportunityPageData(response.data);

  // Check ownership
  if (opportunity.posterId !== session.user.id) {
    redirect(`/opportunities/${slug}`);
  }

  return <OpportunityForm mode="edit" opportunity={opportunity} />;
}

import { redirect, notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { apiClient } from "~/lib/api";
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

  const response = await apiClient.opportunities.getBySlug(slug);
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

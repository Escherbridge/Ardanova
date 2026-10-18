import { notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { apiClient } from "~/lib/api";
import { runWithActorAssertion } from "~/server/actor-assertion";
import { OpportunityDetailView } from "~/components/opportunity-detail-view";
import { toOpportunityPageData } from "./opportunity-page-contract";

interface OpportunityDetailPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function OpportunityDetailPage({
  params,
}: OpportunityDetailPageProps) {
  const session = await auth();
  const { slug } = await params;
  const viewer = session?.user;

  // Read as the signed-in viewer: a DRAFT role is visible only to its team, so an anonymous read 404s it.
  const response = viewer?.id
    ? await runWithActorAssertion(
        { subject: viewer.id, role: viewer.role },
        () => apiClient.opportunities.getBySlug(slug),
      )
    : await apiClient.opportunities.getBySlug(slug);
  if (response.error || !response.data) {
    notFound();
  }
  const opportunity = toOpportunityPageData(response.data);

  // Co-managers (any project manager) get the owner's hiring panel too; the API answers who manages.
  const management =
    viewer?.id && viewer.id !== opportunity.posterId
      ? await runWithActorAssertion(
          { subject: viewer.id, role: viewer.role },
          () => apiClient.opportunities.getViewerManagement(opportunity.id),
        )
      : null;
  const isOwner =
    viewer?.id === opportunity.posterId || management?.data?.canManage === true;

  return <OpportunityDetailView opportunity={opportunity} isOwner={isOwner} />;
}

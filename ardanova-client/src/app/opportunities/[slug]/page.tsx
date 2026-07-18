import { notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { apiClient } from "~/lib/api";
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

  const response = await apiClient.opportunities.getBySlug(slug);
  if (response.error || !response.data) {
    notFound();
  }
  const opportunity = toOpportunityPageData(response.data);

  const isOwner = session?.user?.id === opportunity.posterId;

  return <OpportunityDetailView opportunity={opportunity} isOwner={isOwner} />;
}

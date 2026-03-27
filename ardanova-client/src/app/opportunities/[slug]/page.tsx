import { notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";
import { OpportunityDetailView } from "~/components/opportunity-detail-view";

interface OpportunityDetailPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function OpportunityDetailPage({ params }: OpportunityDetailPageProps) {
  const session = await auth();
  const { slug } = await params;

  let opportunity;
  try {
    opportunity = await api.opportunity.getById({ id: slug });
  } catch {
    notFound();
  }

  if (!opportunity) {
    notFound();
  }

  const isOwner = session?.user?.id === opportunity.posterId;

  return <OpportunityDetailView opportunity={opportunity} isOwner={isOwner} />;
}

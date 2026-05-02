import { redirect, notFound } from "next/navigation";
import { auth } from "~/server/auth";
import { api } from "~/trpc/server";
import { OpportunityForm } from "~/components/opportunity-form";

interface EditOpportunityPageProps {
  params: Promise<{
    slug: string;
  }>;
}

export default async function EditOpportunityPage({ params }: EditOpportunityPageProps) {
  const session = await auth();

  if (!session) {
    redirect("/api/auth/signin");
  }

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

  // Check ownership
  if (opportunity.posterId !== session.user.id) {
    redirect(`/opportunities/${slug}`);
  }

  return <OpportunityForm mode="edit" opportunity={opportunity} />;
}
